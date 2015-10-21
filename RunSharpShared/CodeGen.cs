/*
 * Copyright (c) 2015, Stefan Simek, Vladyslav Taranov
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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

	interface ICodeGenContext : IMemberInfo, ISignatureGen, IDelayedDefinition, IDelayedCompletion
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Typical implementation invokes XxxBuilder.GetILGenerator() which is a method as well.")]
		ILGenerator GetILGenerator();

		Type OwnerType { get; }
		bool SupportsScopes { get; }
	}

	public partial class CodeGen
	{
		ILGenerator _il;
		ICodeGenContext _context;
		ConstructorGen _cg;
		bool _chainCalled = false;
		bool _reachable = true;
		bool _hasRetVar = false, _hasRetLabel = false;
		LocalBuilder _retVar = null;
		Label _retLabel;
		Stack<Block> _blocks = new Stack<Block>();
		Dictionary<string, Label> _labels = new Dictionary<string, Label>();
		Dictionary<string, Operand> _namedLocals = new Dictionary<string, Operand>();

		internal ILGenerator IL { get { return _il; } }
		internal ICodeGenContext Context { get { return _context; } }

		internal CodeGen(ICodeGenContext context)
		{
			this._context = context;
			this._cg = context as ConstructorGen;

			if (_cg != null && _cg.IsStatic)
				// #14 - cg is relevant for instance constructors - it wreaks havoc in a static constructor
				_cg = null;

			_il = context.GetILGenerator();
		}

		/*public static CodeGen CreateDynamicMethod(string name, Type returnType, params Type[] parameterTypes, Type owner, bool skipVisibility)
		{
			DynamicMethod dm = new DynamicMethod(name, returnType, parameterTypes, owner, skipVisibility);
			return new CodeGen(method.GetILGenerator(), defaultType, method.ReturnType, method.IsStatic, parameterTypes);
		}

		public static CodeGen FromMethodBuilder(MethodBuilder builder, params Type[] parameterTypes)
		{
			return new CodeGen(builder.GetILGenerator(), builder.DeclaringType, builder.ReturnType, builder.IsStatic, parameterTypes);
		}

		public static CodeGen FromConstructorBuilder(ConstructorBuilder builder, params Type[] parameterTypes)
		{
			return new CodeGen(builder.GetILGenerator(), builder.DeclaringType, builder.ReturnType, builder.IsStatic, parameterTypes);
		}*/

		#region Arguments
		public Operand This()
		{
			if (_context.IsStatic)
				throw new InvalidOperationException(Properties.Messages.ErrCodeStaticThis);

			return new _Arg(0, _context.OwnerType);
		}

		public Operand Base()
		{
			if (_context.IsStatic)
				return new StaticTarget(_context.OwnerType.BaseType);
			else
				return new _Base(_context.OwnerType.BaseType);
		}

		int ThisOffset { get { return _context.IsStatic ? 0 : 1; } }

		public Operand PropertyValue()
		{
			Type[] parameterTypes = _context.ParameterTypes;
			return new _Arg(ThisOffset + parameterTypes.Length - 1, parameterTypes[parameterTypes.Length - 1]);
		}

		public Operand Arg(string name)
		{
			ParameterGen param = _context.GetParameterByName(name);
			return new _Arg(ThisOffset + param.Position - 1, param.Type);
		}
		#endregion

		#region Locals
		public Operand Local()
		{
			return new _Local(this);
		}

		public Operand Local(Operand init)
		{
			Operand var = Local();
			Assign(var, init);
			return var;
		}

		public Operand Local(Type type)
		{
			return new _Local(this, type);
		}

		public Operand Local(Type type, Operand init)
		{
			Operand var = Local(type);
			Assign(var, init);
			return var;
		}
		#endregion

		bool HasReturnValue
		{
			get
			{
				Type returnType = _context.ReturnType;
			    return returnType != null && !Helpers.AreTypesEqual(returnType, typeof(void), _typeMapper);
			}
		}

		void EnsureReturnVariable()
		{
			if (_hasRetVar)
				return;

			_retLabel = _il.DefineLabel();
			if (HasReturnValue)
				_retVar = _il.DeclareLocal(_context.ReturnType);
			_hasRetVar = true;
		}

		public bool IsCompleted
		{
			get
			{
				return _blocks.Count == 0 && !_reachable && _hasRetVar == _hasRetLabel;
			}
		}

		internal void Complete()
		{
			if (_blocks.Count > 0)
				throw new InvalidOperationException(Properties.Messages.ErrOpenBlocksRemaining);

			if (_reachable)
			{
				if (HasReturnValue)
					throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrMethodMustReturnValue, _context));
				else
					Return();
			}

			if (_hasRetVar && !_hasRetLabel)
			{
				_il.MarkLabel(_retLabel);
				if (_retVar != null)
					_il.Emit(OpCodes.Ldloc, _retVar);
				_il.Emit(OpCodes.Ret);
				_hasRetLabel = true;
			}
		}

		class _Base : _Arg
		{
			public _Base(Type type) : base(0, type) { }

			internal override bool SuppressVirtual
			{
				get
				{
					return true;
				}
			}
		}

		class _Arg : Operand
		{
			ushort _index;
			Type _type;

			public _Arg(int index, Type type)
			{
				this._index = checked((ushort)index);
				this._type = type;
			}

			internal override void EmitGet(CodeGen g)
			{
				g.EmitLdargHelper(_index);

				if (IsReference)
					g.EmitLdindHelper(Type);
			}

			internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
			{
				if (IsReference)
				{
					g.EmitLdargHelper(_index);
					g.EmitStindHelper(Type, value, allowExplicitConversion);
				}
				else
				{
					g.EmitGetHelper(value, Type, allowExplicitConversion);
					g.EmitStargHelper(_index);
				}
			}

			internal override void EmitAddressOf(CodeGen g)
			{
				if (IsReference)
				{
					g.EmitLdargHelper(_index);
				}
				else
				{
					if (_index <= byte.MaxValue)
						g._il.Emit(OpCodes.Ldarga_S, (byte)_index);
					else
						g._il.Emit(OpCodes.Ldarga, _index);
				}
			}

			bool IsReference { get { return _type.IsByRef; } }

			public override Type Type
			{
				get
				{
					return IsReference ? _type.GetElementType() : _type;
				}
			}

			internal override bool TrivialAccess
			{
				get
				{
					return true;
				}
			}
		}

		internal class _Local : Operand
		{
			CodeGen _owner;
			LocalBuilder _var;
			Block _scope;
			Type _t, _tHint;

			public _Local(CodeGen owner)
			{
				this._owner = owner;
				_scope = owner.GetBlockForVariable();
			}
			public _Local(CodeGen owner, Type t)
			{
				this._owner = owner; this._t = t;
				_scope = owner.GetBlockForVariable();
			}

			public _Local(CodeGen owner, LocalBuilder var)
			{
				this._owner = owner;
				this._var = var;
				this._t = var.LocalType;
			}

			void CheckScope(CodeGen g)
			{
				if (g != _owner)
					throw new InvalidOperationException(Properties.Messages.ErrInvalidVariableContext);
				if (_scope != null && !_owner._blocks.Contains(_scope))
					throw new InvalidOperationException(Properties.Messages.ErrInvalidVariableScope);
			}

			internal override void EmitGet(CodeGen g)
			{
				CheckScope(g);

				if (_var == null)
					throw new InvalidOperationException(Properties.Messages.ErrUninitializedVarAccess);

				g._il.Emit(OpCodes.Ldloc, _var);
			}

			internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
			{
				CheckScope(g);

				if (_t == null)
					_t = value.Type;

				if (_var == null)
					_var = g._il.DeclareLocal(_t);

				g.EmitGetHelper(value, _t, allowExplicitConversion);
				g._il.Emit(OpCodes.Stloc, _var);
			}

			internal override void EmitAddressOf(CodeGen g)
			{
				CheckScope(g);

				if (_var == null)
				{
					RequireType();
					_var = g._il.DeclareLocal(_t);
				}

				g._il.Emit(OpCodes.Ldloca, _var);
			}

			public override Type Type
			{
				get
				{
					RequireType();
					return _t;
				}
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

			internal override bool TrivialAccess
			{
				get
				{
					return true;
				}
			}

			internal override void AssignmentHint(Operand op)
			{
				if (_tHint == null)
					_tHint = Operand.GetType(op);
			}
		}

		class StaticTarget : Operand
		{
			Type _t;

			public StaticTarget(Type t) { this._t = t; }

			public override Type Type
			{
				get
				{
					return _t;
				}
			}

			internal override bool IsStaticTarget
			{
				get
				{
					return true;
				}
			}
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

		public void Label(string labelName)
		{
			Label label;
			if (!_labels.TryGetValue(labelName, out label))
				_labels.Add(labelName, label = IL.DefineLabel());
			IL.MarkLabel(label);
		}

		public void Goto(string labelName)
		{
			Label label;
			if (!_labels.TryGetValue(labelName, out label))
				_labels.Add(labelName, label = IL.DefineLabel());
			IL.Emit(OpCodes.Br, label);
		}
	}
}
