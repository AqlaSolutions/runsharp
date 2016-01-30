/*
Copyright(c) 2009, Stefan Simek
Copyright(c) 2016, Vladyslav Taranov

MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TriAxis.RunSharp;
#if FEAT_IKVM
using IKVM.Reflection;
using IKVM.Reflection.Emit;
using Type = IKVM.Reflection.Type;
using MissingMethodException = System.MissingMethodException;
using MissingMemberException = System.MissingMemberException;
using DefaultMemberAttribute = System.Reflection.DefaultMemberAttribute;
using Attribute = IKVM.Reflection.CustomAttributeData;
using BindingFlags = IKVM.Reflection.BindingFlags;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace TriAxis.RunSharp
{
	using Operands;

    public interface ICodeGenBasicContext
    {
        ITypeMapper TypeMapper { get; }
        StaticFactory StaticFactory { get; }
        ExpressionFactory ExpressionFactory { get; }
    }

    public interface ICodeGenContext : IMemberInfo, ISignatureGen, ICodeGenBasicContext, IDelayedDefinition, IDelayedCompletion
    {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Typical implementation invokes XxxBuilder.GetILGenerator() which is a method as well.")]
		ILGenerator GetILGenerator();

		Type OwnerType { get; }
		bool SupportsScopes { get; }
    }

	public partial class CodeGen : ICodeGenContext
	{
	    readonly bool _isOwner;
#if !PHONE8
        readonly ConstructorGen _cg;
#endif

        bool _chainCalled;
		bool _reachable = true;
		bool _hasRetVar, _hasRetLabel;
		LocalBuilder _retVar;
		Label _retLabel;
	    readonly Stack<Block> _blocks = new Stack<Block>();
	    readonly Dictionary<string, Label> _labels = new Dictionary<string, Label>();
	    readonly Dictionary<string, Operand> _namedLocals = new Dictionary<string, Operand>();

		protected internal ILGenerator IL { get; }
        protected internal ICodeGenContext Context { get; }

        public StaticFactory StaticFactory => Context.StaticFactory;
        public ExpressionFactory ExpressionFactory => Context.ExpressionFactory;

        public CodeGen(ICodeGenContext context, bool isOwner = true)
		{
	        _isOwner = isOwner;
	        Context = context;
#if !PHONE8

            _cg = context as ConstructorGen;

			if (_cg != null && _cg.IsStatic)
				// #14 - cg is relevant for instance constructors - it wreaks havoc in a static constructor
				_cg = null;
            
#endif
            IL = context.GetILGenerator();
		}
        
#region Arguments
		public ContextualOperand This()
		{
			if (Context.IsStatic)
				throw new InvalidOperationException(Properties.Messages.ErrCodeStaticThis);

		    Type ownerType = Context.OwnerType;
            
		    if (Context.OwnerType.IsValueType)
		    {
		        var m = Context.Member as MethodInfo;
		        if (m != null && m.IsVirtual)
		            ownerType = ownerType.MakeByRefType();
		    }

		    Operand arg = new _Arg(0, ownerType);
            return new ContextualOperand(arg, TypeMapper);
		}

		public ContextualOperand Base()
		{
			if (Context.IsStatic)
				return new ContextualOperand(new StaticTarget(Context.OwnerType.BaseType), TypeMapper);
			else
				return new ContextualOperand(new _Base(Context.OwnerType.BaseType), TypeMapper);
		}

		int ThisOffset => Context.IsStatic ? 0 : 1;

	    public ContextualOperand PropertyValue()
		{
			Type[] parameterTypes = Context.ParameterTypes;
			return new ContextualOperand(new _Arg(ThisOffset + parameterTypes.Length - 1, parameterTypes[parameterTypes.Length - 1]), TypeMapper);
		}

		public ContextualOperand Arg(string name)
		{
			var param = Context.GetParameterByName(name);
			return new ContextualOperand(new _Arg(ThisOffset + param.Position - 1, param.Type), TypeMapper);
		}
        
        /// <summary>
        /// ThisOffset is applied inside
        /// </summary>
        /// <param name="parameterIndex"></param>
        /// <returns></returns>
		public ContextualOperand Arg(int parameterIndex)
		{
			return new ContextualOperand(new _Arg(ThisOffset + parameterIndex, Context.ParameterTypes[parameterIndex]), TypeMapper);
		}
#endregion

#region Locals
		public ContextualOperand Local()
		{
			return new ContextualOperand(new _Local(this), TypeMapper);
		}

		public ContextualOperand Local(Operand init)
		{
			Operand var = Local();
			Assign(var, init);
			return new ContextualOperand(var, TypeMapper);
		}

#if FEAT_IKVM

        public ContextualOperand Local(System.Type type)
	    {
	        return Local(TypeMapper.MapType(type));
	    }
        
#endif

	    public ContextualOperand Local(Type type)
		{
			return new ContextualOperand(new _Local(this, type), TypeMapper);
		}

#if FEAT_IKVM

        public ContextualOperand Local(System.Type type, Operand init)
	    {
	        return Local(TypeMapper.MapType(type), init);
	    }
        
#endif

	    public ContextualOperand Local(Type type, Operand init)
		{
			Operand var = Local(type);
			Assign(var, init);
			return new ContextualOperand(var, TypeMapper);
		}
#endregion

		bool HasReturnValue
		{
			get
			{
				Type returnType = Context.ReturnType;
			    return returnType != null && !Helpers.AreTypesEqual(returnType, typeof(void), TypeMapper);
			}
		}

		void EnsureReturnVariable()
        {
            if (!_isOwner)
                throw new InvalidOperationException("CodeGen is not an owner of the context");
            if (_hasRetVar)
				return;

			_retLabel = IL.DefineLabel();
			if (HasReturnValue)
				_retVar = IL.DeclareLocal(Context.ReturnType);
			_hasRetVar = true;
		}

		public bool IsCompleted => _blocks.Count == 0 && (!_isOwner || !_reachable) && _hasRetVar == _hasRetLabel;

	    internal void Complete()
		{
			if (_blocks.Count > 0)
				throw new InvalidOperationException(Properties.Messages.ErrOpenBlocksRemaining);

			if (_reachable)
			{
				if (HasReturnValue)
					throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrMethodMustReturnValue, Context));
				else if (_isOwner)
					Return();
			}

			if (_hasRetVar && !_hasRetLabel)
			{
				IL.MarkLabel(_retLabel);
				if (_retVar != null)
					IL.Emit(OpCodes.Ldloc, _retVar);
				IL.Emit(OpCodes.Ret);
				_hasRetLabel = true;
			}
		}

		class _Base : _Arg
		{
			public _Base(Type type) : base(0, type) { }

			protected internal override bool SuppressVirtual => true;
		}

		class _Arg : Operand
		{
            protected override bool DetectsLeaking => false;

            readonly ushort _index;
		    readonly Type _type;

			public _Arg(int index, Type type)
			{
				_index = checked((ushort)index);
				_type = type;
			}

		    protected internal override void EmitGet(CodeGen g)
		    {
		        this.SetLeakedState(false); 
				g.EmitLdargHelper(_index);

				if (IsReference)
					g.EmitLdindHelper(GetReturnType(g.TypeMapper));
			}

		    protected internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		    {
		        this.SetLeakedState(false); 
				if (IsReference)
				{
					g.EmitLdargHelper(_index);
					g.EmitStindHelper(GetReturnType(g.TypeMapper), value, allowExplicitConversion);
				}
				else
				{
					g.EmitGetHelper(value, GetReturnType(g.TypeMapper), allowExplicitConversion);
					g.EmitStargHelper(_index);
				}
			}

		    protected internal override void EmitAddressOf(CodeGen g)
		    {
		        this.SetLeakedState(false);  
				if (IsReference)
				{
					g.EmitLdargHelper(_index);
				}
				else
				{
					if (_index <= byte.MaxValue)
						g.IL.Emit(OpCodes.Ldarga_S, (byte)_index);
					else
						g.IL.Emit(OpCodes.Ldarga, _index);
				}
			}

			bool IsReference => _type.IsByRef;

		    public override Type GetReturnType(ITypeMapper typeMapper) => IsReference ? _type.GetElementType() : _type;

		    protected internal override bool TrivialAccess => true;
		}

		internal class _Local : Operand
		{
            protected override bool DetectsLeaking => false;

            readonly CodeGen _owner;
			LocalBuilder _var;
		    readonly Block _scope;
			Type _t, _tHint;

			public _Local(CodeGen owner)
			{
				_owner = owner;
				_scope = owner.GetBlockForVariable();
			}
			public _Local(CodeGen owner, Type t)
			{
				_owner = owner; _t = t;
				_scope = owner.GetBlockForVariable();
			}

			public _Local(CodeGen owner, LocalBuilder var)
			{
				_owner = owner;
				_var = var;
				_t = var.LocalType;
			}

			void CheckScope(CodeGen g)
			{
				if (g != _owner)
					throw new InvalidOperationException(Properties.Messages.ErrInvalidVariableContext);
				if (_scope != null && !_owner._blocks.Contains(_scope))
					throw new InvalidOperationException(Properties.Messages.ErrInvalidVariableScope);
			}

		    protected internal override void EmitGet(CodeGen g) 
		    {
		        this.SetLeakedState(false); 
				CheckScope(g);

				if (_var == null)
					throw new InvalidOperationException(Properties.Messages.ErrUninitializedVarAccess);

                switch (_var.LocalIndex)
                {
                    case 0:
                        g.IL.Emit(OpCodes.Ldloc_0);
                        break;
                    case 1:
                        g.IL.Emit(OpCodes.Ldloc_1);
                        break;
                    case 2:
                        g.IL.Emit(OpCodes.Ldloc_2);
                        break;
                    case 3:
                        g.IL.Emit(OpCodes.Ldloc_3);
                        break;
                    default:
                        g.IL.Emit(OpCodes.Ldloc, _var);
                        break;
                }
            }

			protected internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
			{
		        this.SetLeakedState(false); 
				CheckScope(g);

				if (_t == null)
					_t = value.GetReturnType(g.TypeMapper);

				if (_var == null)
					_var = g.IL.DeclareLocal(_t);

			    if (ReferenceEquals(value, null) && Helpers.GetNullableUnderlyingType(_t) != null)
			    {
			        g.InitObj(this);
			        return;
			    }

				g.EmitGetHelper(value, _t, allowExplicitConversion);

			    switch (_var.LocalIndex)
			    {
			        case 0:
			            g.IL.Emit(OpCodes.Stloc_0);
			            break;
			        case 1:
			            g.IL.Emit(OpCodes.Stloc_1);
			            break;
			        case 2:
			            g.IL.Emit(OpCodes.Stloc_2);
			            break;
			        case 3:
			            g.IL.Emit(OpCodes.Stloc_3);
			            break;
			        default:
			            g.IL.Emit(OpCodes.Stloc, _var);
                        break;
                }
			}

			protected internal override void EmitAddressOf(CodeGen g)
			{
		        this.SetLeakedState(false); 
				CheckScope(g);

				if (_var == null)
				{
					RequireType();
					_var = g.IL.DeclareLocal(_t);
				}

				g.IL.Emit(OpCodes.Ldloca, _var);
			}

		    public override Type GetReturnType(ITypeMapper typeMapper)
		    {
		        RequireType();
		        return _t;
		    }

		    void RequireType()
			{
				if (_t == null)
				{
					if (_tHint != null)
						_t = _tHint;
					else
						throw new InvalidOperationException(Properties.Messages.ErrUntypedVarAccess);
				}
			}

			protected internal override bool TrivialAccess => true;

		    protected internal override void AssignmentHint(Operand op)
			{
				if (_tHint == null)
					_tHint = GetType(op, _owner.TypeMapper);
			}
		}

		class StaticTarget : Operand
		{
		    public StaticTarget(Type t) { _type = t; }

		    readonly Type _type;

		    public override Type GetReturnType(ITypeMapper typeMapper)
		    {
		        return _type;
		    }

		    protected internal override bool IsStaticTarget => true;
		}

		public Operand this[string localName] // Named locals support. 
		{
			get
			{
				Operand target;
				if (!_namedLocals.TryGetValue(localName, out target))
					throw new InvalidOperationException(Properties.Messages.ErrUninitializedVarAccess);
				return target;
			}
			set
			{
				Operand target;
				if (_namedLocals.TryGetValue(localName, out target))
					// run in statement form; C# left-to-right evaluation semantics "just work"
					Assign(target, value);
				else
					_namedLocals.Add(localName, Local(value));
			}
		}

        
		public Label Label(string labelName)
		{
			Label label;
			if (!_labels.TryGetValue(labelName, out label))
				_labels.Add(labelName, label = IL.DefineLabel());
			IL.MarkLabel(label);
		    return label;
		}

	    public Label DefineLabel()
	    {
	        return IL.DefineLabel();
	    }

	    public void MarkLabel(Label label)
	    {
	        IL.MarkLabel(label);
	    }

		public void Goto(string labelName)
		{
			Label label;
			if (!_labels.TryGetValue(labelName, out label))
				_labels.Add(labelName, label = IL.DefineLabel());
			Goto(label);
		}

	    public void Goto(Label label)
	    {
	        IL.Emit(OpCodes.Br, label);
	    }

	    #region Context explicit delegation

	    MemberInfo IMemberInfo.Member { get { return Context.Member; } }

	    string IMemberInfo.Name { get { return Context.Name; } }

	    Type IMemberInfo.ReturnType { get { return Context.ReturnType; } }

	    Type[] IMemberInfo.ParameterTypes { get { return Context.ParameterTypes; } }

	    bool IMemberInfo.IsParameterArray { get { return Context.IsParameterArray; } }

	    bool IMemberInfo.IsStatic { get { return Context.IsStatic; } }

	    bool IMemberInfo.IsOverride { get { return Context.IsOverride; } }

	    ParameterBuilder ISignatureGen.DefineParameter(int position, ParameterAttributes attributes, string parameterName)
	    {
	        return Context.DefineParameter(position, attributes, parameterName);
	    }

	    IParameterBasicInfo ISignatureGen.GetParameterByName(string parameterName)
	    {
	        return Context.GetParameterByName(parameterName);
	    }

	    StaticFactory ICodeGenBasicContext.StaticFactory { get { return Context.StaticFactory; } }

	    ExpressionFactory ICodeGenBasicContext.ExpressionFactory { get { return Context.ExpressionFactory; } }

	    void IDelayedDefinition.EndDefinition()
	    {
	        Context.EndDefinition();
	    }

	    void IDelayedCompletion.Complete()
	    {
	        Context.Complete();
	    }

	    ILGenerator ICodeGenContext.GetILGenerator()
	    {
	        return Context.GetILGenerator();
	    }

	    Type ICodeGenContext.OwnerType { get { return Context.OwnerType; } }

	    bool ICodeGenContext.SupportsScopes { get { return Context.SupportsScopes; } }

	    #endregion

	}
}
