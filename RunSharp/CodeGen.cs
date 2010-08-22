/*
 * Copyright (c) 2009, Stefan Simek
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
using System.Reflection;
using System.Reflection.Emit;

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
		ILGenerator il;
		ICodeGenContext context;
		ConstructorGen cg;
		bool chainCalled = false;
		bool reachable = true;
		bool hasRetVar = false, hasRetLabel = false;
		LocalBuilder retVar = null;
		Label retLabel;
		Stack<Block> blocks = new Stack<Block>();
		Dictionary<string, Label> Labels = new Dictionary<string, Label>();
		Dictionary<string, Operand> NamedLocals = new Dictionary<string, Operand>();

		internal ILGenerator IL { get { return il; } }
		internal ICodeGenContext Context { get { return context; } }

		internal CodeGen(ICodeGenContext context)
		{
			this.context = context;
			this.cg = context as ConstructorGen;

			if (cg != null && cg.IsStatic)
				// #14 - cg is relevant for instance constructors - it wreaks havoc in a static constructor
				cg = null;

			il = context.GetILGenerator();
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
			if (context.IsStatic)
				throw new InvalidOperationException(Properties.Messages.ErrCodeStaticThis);

			return new _Arg(0, context.OwnerType);
		}

		public Operand Base()
		{
			if (context.IsStatic)
				return new _StaticTarget(context.OwnerType.BaseType);
			else
				return new _Base(context.OwnerType.BaseType);
		}

		int _ThisOffset { get { return context.IsStatic ? 0 : 1; } }

		public Operand PropertyValue()
		{
			Type[] parameterTypes = context.ParameterTypes;
			return new _Arg(_ThisOffset + parameterTypes.Length - 1, parameterTypes[parameterTypes.Length - 1]);
		}

		public Operand Arg(string name)
		{
			ParameterGen param = context.GetParameterByName(name);
			return new _Arg(_ThisOffset + param.Position - 1, param.Type);
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
				Type returnType = context.ReturnType;
				return returnType != null && returnType != typeof(void);
			}
		}

		void EnsureReturnVariable()
		{
			if (hasRetVar)
				return;

			retLabel = il.DefineLabel();
			if (HasReturnValue)
				retVar = il.DeclareLocal(context.ReturnType);
			hasRetVar = true;
		}

		public bool IsCompleted
		{
			get
			{
				return blocks.Count == 0 && !reachable && hasRetVar == hasRetLabel;
			}
		}

		internal void Complete()
		{
			if (blocks.Count > 0)
				throw new InvalidOperationException(Properties.Messages.ErrOpenBlocksRemaining);

			if (reachable)
			{
				if (HasReturnValue)
					throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrMethodMustReturnValue, context));
				else
					Return();
			}

			if (hasRetVar && !hasRetLabel)
			{
				il.MarkLabel(retLabel);
				if (retVar != null)
					il.Emit(OpCodes.Ldloc, retVar);
				il.Emit(OpCodes.Ret);
				hasRetLabel = true;
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
			ushort index;
			Type type;

			public _Arg(int index, Type type)
			{
				this.index = checked((ushort)index);
				this.type = type;
			}

			internal override void EmitGet(CodeGen g)
			{
				g.EmitLdargHelper(index);

				if (IsReference)
					g.EmitLdindHelper(Type);
			}

			internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
			{
				if (IsReference)
				{
					g.EmitLdargHelper(index);
					g.EmitStindHelper(Type, value, allowExplicitConversion);
				}
				else
				{
					g.EmitGetHelper(value, Type, allowExplicitConversion);
					g.EmitStargHelper(index);
				}
			}

			internal override void EmitAddressOf(CodeGen g)
			{
				if (IsReference)
				{
					g.EmitLdargHelper(index);
				}
				else
				{
					if (index <= byte.MaxValue)
						g.il.Emit(OpCodes.Ldarga_S, (byte)index);
					else
						g.il.Emit(OpCodes.Ldarga, index);
				}
			}

			bool IsReference { get { return type.IsByRef; } }

			public override Type Type
			{
				get
				{
					return IsReference ? type.GetElementType() : type;
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
			CodeGen owner;
			LocalBuilder var;
			Block scope;
			Type t, tHint;

			public _Local(CodeGen owner)
			{
				this.owner = owner;
				scope = owner.GetBlockForVariable();
			}
			public _Local(CodeGen owner, Type t)
			{
				this.owner = owner; this.t = t;
				scope = owner.GetBlockForVariable();
			}

			public _Local(CodeGen owner, LocalBuilder var)
			{
				this.owner = owner;
				this.var = var;
				this.t = var.LocalType;
			}

			void CheckScope(CodeGen g)
			{
				if (g != owner)
					throw new InvalidOperationException(Properties.Messages.ErrInvalidVariableContext);
				if (scope != null && !owner.blocks.Contains(scope))
					throw new InvalidOperationException(Properties.Messages.ErrInvalidVariableScope);
			}

			internal override void EmitGet(CodeGen g)
			{
				CheckScope(g);

				if (var == null)
					throw new InvalidOperationException(Properties.Messages.ErrUninitializedVarAccess);

				g.il.Emit(OpCodes.Ldloc, var);
			}

			internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
			{
				CheckScope(g);

				if (t == null)
					t = value.Type;

				if (var == null)
					var = g.il.DeclareLocal(t);

				g.EmitGetHelper(value, t, allowExplicitConversion);
				g.il.Emit(OpCodes.Stloc, var);
			}

			internal override void EmitAddressOf(CodeGen g)
			{
				CheckScope(g);

				if (var == null)
				{
					RequireType();
					var = g.il.DeclareLocal(t);
				}

				g.il.Emit(OpCodes.Ldloca, var);
			}

			public override Type Type
			{
				get
				{
					RequireType();
					return t;
				}
			}

			void RequireType()
			{
				if (t == null)
				{
					if (tHint != null)
						t = tHint;
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
				if (tHint == null)
					tHint = Operand.GetType(op);
			}
		}

		class _StaticTarget : Operand
		{
			Type t;

			public _StaticTarget(Type t) { this.t = t; }

			public override Type Type
			{
				get
				{
					return t;
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
				if (!NamedLocals.TryGetValue(localName, out target))
					throw new InvalidOperationException(Properties.Messages.ErrUninitializedVarAccess);
				return target;
			}
			set
			{
				Operand target;
				if (NamedLocals.TryGetValue(localName, out target))
					// run in statement form; C# left-to-right evaluation semantics "just work"
					Assign(target, value);
				else
					NamedLocals.Add(localName, Local(value));
			}
		}

		public void Label(string labelName)
		{
			Label label;
			if (!Labels.TryGetValue(labelName, out label))
				Labels.Add(labelName, label = IL.DefineLabel());
			IL.MarkLabel(label);
		}

		public void Goto(string labelName)
		{
			Label label;
			if (!Labels.TryGetValue(labelName, out label))
				Labels.Add(labelName, label = IL.DefineLabel());
			IL.Emit(OpCodes.Br, label);
		}
	}
}
