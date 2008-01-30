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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace TriAxis.RunSharp
{
	using Operands;

	public interface IStatement
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "0#g", Justification = "The 'g' is used throughout the library for 'CodeGen'")]
		void Emit(CodeGen g);
	}

	public abstract class Operand
	{
		internal static readonly Operand[] EmptyArray = { };

		#region Virtual methods
		internal virtual void EmitGet(CodeGen g)
		{
			throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrOperandNotReadable, GetType()));
		}

		internal virtual void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrOperandNotWritable, GetType()));
		}

		internal virtual void EmitAddressOf(CodeGen g)
		{
			throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrOperandNotReferencible, GetType()));
		}

		internal virtual void EmitBranch(CodeGen g, BranchSet branchSet, Label label)
		{
			if (g == null)
				throw new ArgumentNullException("g");
			if (branchSet == null)
				throw new ArgumentNullException("branchSet");

			EmitGet(g);
			g.IL.Emit(branchSet.brTrue, label);
		}
		public abstract Type Type { get; }

		internal virtual bool TrivialAccess { get { return false; } }
		internal virtual bool IsStaticTarget { get { return false; } }
		internal virtual bool SuppressVirtual { get { return false; } }
		internal virtual object ConstantValue { get { return null; } }
		internal virtual void AssignmentHint(Operand op) { }
		#endregion

		// emits the refrence to the operand (address-of for value types)
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "0#g", Justification = "The 'g' is used throughout the library for 'CodeGen'")]
		public void EmitRef(CodeGen g)
		{
			if (Type.IsValueType)
				EmitAddressOf(g);
			else
				EmitGet(g);
		}

		#region Implicit conversions
		[DebuggerHidden]
		public static implicit operator Operand(Type type)
		{
			return new TypeLiteral(type);
		}

		[DebuggerHidden]
		public static implicit operator Operand(string value)
		{
			return new StringLiteral(value);
		}

		[DebuggerHidden]
		public static implicit operator Operand(bool value)
		{
			return new IntLiteral(typeof(bool), value ? 1 : 0);
		}

		[DebuggerHidden]
		public static implicit operator Operand(byte value)
		{
			return new IntLiteral(typeof(byte), value);
		}

		[DebuggerHidden]
		[CLSCompliant(false)]
		public static implicit operator Operand(sbyte value)
		{
			return new IntLiteral(typeof(sbyte), value);
		}

		[DebuggerHidden]
		public static implicit operator Operand(short value)
		{
			return new IntLiteral(typeof(short), value);
		}

		[DebuggerHidden]
		[CLSCompliant(false)]
		public static implicit operator Operand(ushort value)
		{
			return new IntLiteral(typeof(ushort), value);
		}

		[DebuggerHidden]
		public static implicit operator Operand(char value)
		{
			return new IntLiteral(typeof(char), value);
		}

		[DebuggerHidden]
		public static implicit operator Operand(int value)
		{
			return new IntLiteral(typeof(int), value);
		}

		[DebuggerHidden]
		[CLSCompliant(false)]
		public static implicit operator Operand(uint value)
		{
			return new IntLiteral(typeof(uint), unchecked((int)value));
		}

		[DebuggerHidden]
		public static implicit operator Operand(long value)
		{
			return new LongLiteral(typeof(long), value);
		}

		[DebuggerHidden]
		[CLSCompliant(false)]
		public static implicit operator Operand(ulong value)
		{
			return new LongLiteral(typeof(ulong), unchecked((long)value));
		}

		[DebuggerHidden]
		public static implicit operator Operand(float value)
		{
			return new FloatLiteral(value);
		}

		[DebuggerHidden]
		public static implicit operator Operand(double value)
		{
			return new DoubleLiteral(value);
		}

		[DebuggerHidden]
		public static implicit operator Operand(decimal value)
		{
			return new DecimalLiteral(value);
		}

		public static implicit operator Operand(Enum value)
		{
			return new EnumLiteral(value);
		}
		#endregion

		public static Operand FromObject(object operandOrLiteral)
		{
			if (operandOrLiteral == null)
				return null;

			Operand op = operandOrLiteral as Operand;
			if ((object)op != null)
				return op;

			Type type = operandOrLiteral as Type;
			if (type != null)
				return type;

			string str = operandOrLiteral as string;
			if (str != null)
				return str;

			Type t = operandOrLiteral.GetType();

			if (t.IsPrimitive)
			{
				if (t == typeof(int))
					return (int)operandOrLiteral;
				if (t == typeof(uint))
					return (uint)operandOrLiteral;
				if (t == typeof(long))
					return (long)operandOrLiteral;
				if (t == typeof(ulong))
					return (ulong)operandOrLiteral;
				if (t == typeof(float))
					return (float)operandOrLiteral;
				if (t == typeof(double))
					return (double)operandOrLiteral;

				// all other types are converted to I4
				return new IntLiteral(t, ((IConvertible)operandOrLiteral).ToInt32(null));
			}

			if (t == typeof(decimal))
				return (decimal)operandOrLiteral;

			throw new InvalidOperationException(Properties.Messages.ErrInvalidOperand);
		}

		public Assignment Assign(Operand value)
		{
			return Assign(value, false);
		}

		public Assignment Assign(Operand value, bool allowExplicitConversion)
		{
			return new Assignment(this, value, allowExplicitConversion);
		}

		public IStatement AssignAdd(Operand value)
		{
			return Assign(Add(value));
		}

		public IStatement AssignSubtract(Operand value)
		{
			return Assign(Subtract(value));
		}

		public IStatement AssignMultiply(Operand value)
		{
			return Assign(Multiply(value));
		}

		public IStatement AssignDivide(Operand value)
		{
			return Assign(Divide(value));
		}

		public IStatement AssignModulus(Operand value)
		{
			return Assign(Modulus(value));
		}

		public IStatement AssignAnd(Operand value)
		{
			return Assign(BitwiseAnd(value));
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Justification = "Checked, OK")]
		public IStatement AssignOr(Operand value)
		{
			return Assign(BitwiseOr(value));
		}

		public IStatement AssignXor(Operand value)
		{
			return Assign(Xor(value));
		}

		public IStatement AssignLeftShift(Operand value)
		{
			return Assign(LeftShift(value));
		}

		public IStatement AssignRightShift(Operand value)
		{
			return Assign(RightShift(value));
		}

		public IStatement Increment()
		{
			return new PrefixOperation(Operator.Increment, this);
		}

		public IStatement Decrement()
		{
			return new PrefixOperation(Operator.Decrement, this);
		}

		#region Comparisons
		public override bool Equals(object obj)
		{
			throw new InvalidOperationException();
		}

		public override int GetHashCode()
		{
			throw new InvalidOperationException();
		}

		public static Operand operator ==(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.Equality, left, right);
		}

		public Operand EQ(Operand value)
		{
			return new OverloadableOperation(Operator.Equality, this, value);
		}

		public static Operand operator !=(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.Inequality, left, right);
		}

		public Operand NE(Operand value)
		{
			return new OverloadableOperation(Operator.Inequality, this, value);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Method *has* an alternative (LT)")]
		public static Operand operator <(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.LessThan, left, right);
		}

		public Operand LT(Operand value)
		{
			return new OverloadableOperation(Operator.LessThan, this, value);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Method *has* an alternative (GT)")]
		public static Operand operator >(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.GreaterThan, left, right);
		}

		public Operand GT(Operand value)
		{
			return new OverloadableOperation(Operator.GreaterThan, this, value);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Method *has* an alternative (GE)")]
		public static Operand operator >=(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.GreaterThanOrEqual, left, right);
		}

		public Operand GE(Operand value)
		{
			return new OverloadableOperation(Operator.GreaterThanOrEqual, this, value);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Method *has* an alternative (LE)")]
		public static Operand operator <=(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.LessThanOrEqual, left, right);
		}

		public Operand LE(Operand value)
		{
			return new OverloadableOperation(Operator.LessThanOrEqual, this, value);
		}
		#endregion

		#region Arithmetic Operations
		public static Operand operator +(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.Add, left, right);
		}

		public Operand Add(Operand value)
		{
			return new OverloadableOperation(Operator.Add, this, value);
		}

		public static Operand operator -(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.Subtract, left, right);
		}

		public Operand Subtract(Operand value)
		{
			return new OverloadableOperation(Operator.Subtract, this, value);
		}

		public static Operand operator *(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.Multiply, left, right);
		}

		public Operand Multiply(Operand value)
		{
			return new OverloadableOperation(Operator.Multiply, this, value);
		}

		public static Operand operator /(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.Divide, left, right);
		}

		public Operand Divide(Operand value)
		{
			return new OverloadableOperation(Operator.Divide, this, value);
		}

		public static Operand operator %(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.Modulus, left, right);
		}

		public Operand Modulus(Operand value)
		{
			return new OverloadableOperation(Operator.Modulus, this, value);
		}

		public static Operand operator &(Operand left, Operand right)
		{
			if ((object)left != null && left.logical)
			{
				left.logical = false;
				return left.LogicalAnd(right);
			}

			return new OverloadableOperation(Operator.And, left, right);
		}

		public Operand BitwiseAnd(Operand value)
		{
			return new OverloadableOperation(Operator.And, this, value);
		}

		public static Operand operator |(Operand left, Operand right)
		{
			if ((object)left != null && left.logical)
			{
				left.logical = false;
				return left.LogicalOr(right);
			}

			return new OverloadableOperation(Operator.Or, left, right);
		}

		public Operand BitwiseOr(Operand value)
		{
			return new OverloadableOperation(Operator.Or, this, value);
		}

		public static Operand operator ^(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.Xor, left, right);
		}

		public Operand Xor(Operand value)
		{
			return new OverloadableOperation(Operator.Xor, this, value);
		}

		public static Operand operator <<(Operand left, int right)
		{
			return new OverloadableOperation(Operator.LeftShift, left, right);
		}

		public Operand LeftShift(Operand value)
		{
			return new OverloadableOperation(Operator.LeftShift, this, value);
		}

		public static Operand operator >>(Operand left, int right)
		{
			return new OverloadableOperation(Operator.RightShift, left, right);
		}

		public Operand RightShift(Operand value)
		{
			return new OverloadableOperation(Operator.RightShift, this, value);
		}

		public static Operand operator +(Operand op)
		{
			return new OverloadableOperation(Operator.Plus, op);
		}

		public Operand Plus()
		{
			return new OverloadableOperation(Operator.Plus, this);
		}

		public static Operand operator -(Operand op)
		{
			return new OverloadableOperation(Operator.Minus, op);
		}

		public Operand Negate()
		{
			return new OverloadableOperation(Operator.Minus, this);
		}

		public static Operand operator !(Operand op)
		{
			return new OverloadableOperation(Operator.LogicalNot, op);
		}

		public Operand LogicalNot()
		{
			return new OverloadableOperation(Operator.LogicalNot, this);
		}

		public static Operand operator ~(Operand op)
		{
			return new OverloadableOperation(Operator.Not, op);
		}

		public Operand OnesComplement()
		{
			return new OverloadableOperation(Operator.Not, this);
		}

		public Operand Pow2()
		{
			return new SimpleOperation(this, OpCodes.Dup, OpCodes.Mul);
		}

		public Operand LogicalAnd(Operand other)
		{
			return Conditional(other, false);
		}

		public Operand LogicalOr(Operand other)
		{
			return Conditional(true, other);
		}

		public Operand PostIncrement()
		{
			return new PostfixOperation(Operator.Increment, this);
		}

		public Operand PostDecrement()
		{
			return new PostfixOperation(Operator.Decrement, this);
		}

		public Operand PreIncrement()
		{
			return new PrefixOperation(Operator.Increment, this);
		}

		public Operand PreDecrement()
		{
			return new PrefixOperation(Operator.Decrement, this);
		}

		public Operand IsTrue()
		{
			return new OverloadableOperation(Operator.True, this);
		}

		public Operand IsFalse()
		{
			return new OverloadableOperation(Operator.False, this);
		}
		#endregion

		#region Logical operations
		bool logical = false;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "The operator is provided for convenience, so that the && and || operators work correctly. It should not be invoked under any other circumstances.")]
		public static bool operator true(Operand op)
		{
			if ((object)op != null)
				op.logical = true;
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "The operator is provided for convenience, so that the && and || operators work correctly. It should not be invoked under any other circumstances.")]
		public static bool operator false(Operand op)
		{
			if ((object)op != null)
				op.logical = true;
			return false;
		}
		#endregion
		#region Special operations
		public Operand Conditional(Operand ifTrue, Operand ifFalse)
		{
			return new Conditional(Type == typeof(bool) ? this : IsTrue(), ifTrue, ifFalse);
		}

		public Operand Cast(Type type)
		{
			return new Cast(this, type);
		}
		#endregion

		#region Member access
		internal virtual BindingFlags GetBindingFlags()
		{
			return BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		}

		internal static string GetTypeName(Operand op)
		{
			if ((object)op == null)
				return "<null>";

			return op.Type.FullName;
		}

		internal static Type GetType(Operand op)
		{
			if ((object)op == null)
				return null;

			return op.Type;
		}

		internal static Type[] GetTypes(Operand[] ops)
		{
			if (ops == null)
				return null;

			Type[] types = new Type[ops.Length];
			for (int i = 0; i < ops.Length; i++)
				types[i] = (object)ops[i] == null ? null : ops[i].Type;

			return types;
		}

		/*FieldInfo FindField(string name, BindingFlags flags)
		{
			FieldInfo fi = Type.GetField(name, flags);

			if (fi == null)
				throw new MissingFieldException(Properties.Messages.ErrMissingField);

			return fi;
		}

		PropertyInfo FindProperty(string name, BindingFlags flags, Operand[] indexes)
		{
			Type[] types = Operand.GetTypes(indexes);
			ArrayUtils.ReduceIncompleteTypesToBase(types);

			if (name == null)
			{
				foreach (DefaultMemberAttribute dma in Attribute.GetCustomAttributes(Type, typeof(DefaultMemberAttribute)))
				{
					name = dma.MemberName;
					break;
				}
			}

			if (name == null)
				throw new InvalidOperationException(Properties.Messages.ErrMissingDefaultProperty);

			PropertyInfo pi = Type.UnderlyingSystemType.GetProperty(name, flags, null, null, types, null);

			if (pi == null)
				throw new MissingMemberException(Properties.Messages.ErrMissingProperty);

			return pi;
		}

		MethodInfo FindMethod(string name, BindingFlags flags, Operand[] args)
		{
			Type[] types = Operand.GetTypes(args);
			ArrayUtils.ReduceIncompleteTypesToBase(types);

			MethodInfo mi = Type.GetMethod(name, flags, null, types, null);

			if (mi == null)
				throw new MissingMethodException(Properties.Messages.ErrMissingMethod);

			return mi;
		}
		*/

		public Operand Field(string name)
		{
			return new Field((FieldInfo)TypeInfo.FindField(Type, name, IsStaticTarget).Member, this);
		}

		public Operand Property(string name)
		{
			return Property(name, Operand.EmptyArray);
		}

		public Operand Property(string name, params Operand[] indexes)
		{
			return new Property(TypeInfo.FindProperty(Type, name, indexes, IsStaticTarget), this, indexes);
		}

		public Operand Invoke(string name)
		{
			return Invoke(name, Operand.EmptyArray);
		}

		public Operand Invoke(string name, params Operand[] args)
		{
			return new Invocation(TypeInfo.FindMethod(Type, name, args, IsStaticTarget), this, args);
		}

		public Operand InvokeDelegate()
		{
			return InvokeDelegate(Operand.EmptyArray);
		}

		public Operand InvokeDelegate(params Operand[] args)
		{
			return Invoke("Invoke", args);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers", Justification = "Intentional, to simulate standard indexer 'feel'")]
		public Operand this[params Operand[] indexes]
		{
			get
			{
				if (Type.IsArray)
					return new ArrayAccess(this, indexes);

				return Property(null, indexes);
			}
		}

		public Operand ArrayLength()
		{
			return new ArrayLength(this, false);
		}

		public Operand LongArrayLength()
		{
			return new ArrayLength(this, true);
		}
		#endregion

		public Operand Ref()
		{
			return new Reference(this);
		}

		class Reference : Operand
		{
			Operand op;

			public Reference(Operand op) { this.op = op; }

			internal override void EmitAddressOf(CodeGen g)
			{
				op.EmitAddressOf(g);
			}

			public override Type Type { get { return op.Type.MakeByRefType(); } }
		}
	}
}
