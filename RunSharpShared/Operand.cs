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

	public interface IStatement
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "0#g", Justification = "The 'g' is used throughout the library for 'CodeGen'")]
		void Emit(CodeGen g);
	}

	public abstract class Operand
	{
	    protected Operand()
	    {
	        _detectsLeaks = DetectsLeaking;
	    }

	    protected void EmitGetHelper(CodeGen g, Operand op, Type desiredType, bool allowExplicitConversion)
	    {
            g.EmitGetHelper(op, desiredType,allowExplicitConversion);
	    }
        
	    protected internal static readonly Operand[] EmptyArray = { };

        public void _ManualEmitGet(CodeGen g) => EmitGet(g);

		#region Virtual methods
		protected internal virtual void EmitGet(CodeGen g)
		{
			throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrOperandNotReadable, GetType()));
		}

        protected internal virtual void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrOperandNotWritable, GetType()));
		}

        protected internal virtual void EmitAddressOf(CodeGen g)
		{
			throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrOperandNotReferencible, GetType()));
		}

	    protected internal virtual void EmitBranch(CodeGen g, OptionalLabel labelTrue, OptionalLabel labelFalse)
	    {
	        if (g == null)
	            throw new ArgumentNullException(nameof(g));

	        OperandExtensions.SetLeakedState(this, false);
	        EmitGet(g);
	        if (labelTrue != null && labelTrue.IsLabelExist)
	        {
	            g.IL.Emit(BranchSet.Normal.BrTrue, labelTrue.Value);
                if (labelFalse != null && labelFalse.IsLabelExist)
                {
                    g.IL.Emit(OpCodes.Br, labelFalse.Value);
                }
            }
	        else if (labelFalse != null && labelFalse.IsLabelExist)
	        {
	            g.IL.Emit(BranchSet.Normal.BrFalse, labelFalse.Value);
	        }
	        else
	        {
                throw new InvalidOperationException("No labels passed");
            }
	            
	    }

	    public abstract Type GetReturnType(ITypeMapper typeMapper);

        protected internal virtual bool TrivialAccess
	    { get { OperandExtensions.SetLeakedState(this, false); return false; } }

        protected internal virtual bool IsStaticTarget
	    { get { OperandExtensions.SetLeakedState(this, false); return false; } }

        protected internal virtual bool SuppressVirtual
	    { get { OperandExtensions.SetLeakedState(this, false); return false; } }

        protected internal virtual object ConstantValue
	    { get { OperandExtensions.SetLeakedState(this, false); return null; } }

        protected internal virtual void AssignmentHint(Operand op) { OperandExtensions.SetLeakedState(this, false); }
		#endregion

		// emits the refrence to the operand (address-of for value types)
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "0#g", Justification = "The 'g' is used throughout the library for 'CodeGen'")]
		protected internal void EmitRef(CodeGen g)
		{
            OperandExtensions.SetLeakedState(this, false);
            if (GetReturnType(g.TypeMapper).IsValueType)
				EmitAddressOf(g);
			else
				EmitGet(g);
		}

		#region Implicit conversions
		[DebuggerHidden]
		public static implicit operator Operand(Type type)
		{
			return OperandExtensions.SetLeakedState(new TypeLiteral(type), true);
		}

		[DebuggerHidden]
		public static implicit operator Operand(string value)
		{
			return OperandExtensions.SetLeakedState(new StringLiteral(value), true);
		}

		[DebuggerHidden]
		public static implicit operator Operand(bool value)
		{
			return OperandExtensions.SetLeakedState(new IntLiteral(typeof(bool), value ? 1 : 0), true);
		}

		[DebuggerHidden]
		public static implicit operator Operand(byte value)
		{
			return OperandExtensions.SetLeakedState(new IntLiteral(typeof(byte), value), true);
		}

		[DebuggerHidden]
		//[CLSCompliant(false)]
		public static implicit operator Operand(sbyte value)
		{
			return OperandExtensions.SetLeakedState(new IntLiteral(typeof(sbyte), value), true);
		}

		[DebuggerHidden]
		public static implicit operator Operand(short value)
		{
			return OperandExtensions.SetLeakedState(new IntLiteral(typeof(short), value), true);
		}

		[DebuggerHidden]
		//[CLSCompliant(false)]
		public static implicit operator Operand(ushort value)
		{
			return OperandExtensions.SetLeakedState(new IntLiteral(typeof(ushort), value), true);
		}

		[DebuggerHidden]
		public static implicit operator Operand(char value)
		{
			return OperandExtensions.SetLeakedState(new IntLiteral(typeof(char), value), true);
		}

		[DebuggerHidden]
		public static implicit operator Operand(int value)
		{
			return OperandExtensions.SetLeakedState(new IntLiteral(typeof(int), value), true);
		}

		[DebuggerHidden]
		//[CLSCompliant(false)]
		public static implicit operator Operand(uint value)
		{
			return OperandExtensions.SetLeakedState(new IntLiteral(typeof(uint), unchecked((int)value)), true);
		}

		[DebuggerHidden]
		public static implicit operator Operand(long value)
		{
			return OperandExtensions.SetLeakedState(new LongLiteral(typeof(long), value), true);
		}

		[DebuggerHidden]
		//[CLSCompliant(false)]
		public static implicit operator Operand(ulong value)
		{
			return OperandExtensions.SetLeakedState(new LongLiteral(typeof(ulong), unchecked((long)value)), true);
		}

		[DebuggerHidden]
		public static implicit operator Operand(float value)
		{
			return OperandExtensions.SetLeakedState(new FloatLiteral(value), true);
		}

		[DebuggerHidden]
		public static implicit operator Operand(double value)
		{
			return OperandExtensions.SetLeakedState(new DoubleLiteral(value), true);
		}

		[DebuggerHidden]
		public static implicit operator Operand(decimal value)
		{
			return OperandExtensions.SetLeakedState(new DecimalLiteral(value), true);
		}

		public static implicit operator Operand(Enum value)
		{
			return OperandExtensions.SetLeakedState(new EnumLiteral(value), true);
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

			System.Type t = operandOrLiteral.GetType();

			if (t.IsEnum)
				return new EnumLiteral((Enum)operandOrLiteral);

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
			return OperandExtensions.SetLeakedState(new Assignment(this, value, allowExplicitConversion), true);
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

		public Operand Eq(Operand value)
		{
			return new OverloadableOperation(Operator.Equality, this, value);
		}

	    public ContextualOperand InvokeReferenceEquals(Operand right, ITypeMapper typeMapper)
	    {
            Operand left = this;
            var args = new Operand[] { left, right };
            return OperandExtensions.SetLeakedState(new ContextualOperand(new Invocation(typeMapper.TypeInfo.FindMethod(typeMapper.MapType(typeof(object)), "ReferenceEquals", args, true), null, args), typeMapper), true);
        }
        
	    public ContextualOperand InvokeEquals(Operand right, ITypeMapper typeMapper)
	    {
            Operand left = this;
            var args = new Operand[] { left, right };
            return OperandExtensions.SetLeakedState(new ContextualOperand(new Invocation(typeMapper.TypeInfo.FindMethod(typeMapper.MapType(typeof(object)), "Equals", args, true), null, args), typeMapper), true);
        }
        
	    public static Operand operator !=(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.Inequality, left, right);
		}

		public Operand Ne(Operand value)
		{
			return new OverloadableOperation(Operator.Inequality, this, value);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Method *has* an alternative (LT)")]
		public static Operand operator <(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.LessThan, left, right);
		}

		public Operand Lt(Operand value)
		{
			return new OverloadableOperation(Operator.LessThan, this, value);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Method *has* an alternative (GT)")]
		public static Operand operator >(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.GreaterThan, left, right);
		}

		public Operand Gt(Operand value)
		{
			return new OverloadableOperation(Operator.GreaterThan, this, value);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Method *has* an alternative (GE)")]
		public static Operand operator >=(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.GreaterThanOrEqual, left, right);
		}

		public Operand Ge(Operand value)
		{
			return new OverloadableOperation(Operator.GreaterThanOrEqual, this, value);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Method *has* an alternative (LE)")]
		public static Operand operator <=(Operand left, Operand right)
		{
			return new OverloadableOperation(Operator.LessThanOrEqual, left, right);
		}

		public Operand Le(Operand value)
		{
			return new OverloadableOperation(Operator.LessThanOrEqual, this, value);
		}
		#endregion

		#region Arithmetic Operations
		public static Operand operator +(Operand left, Operand right)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Add, left, right), true);
		}

		public Operand Add(Operand value)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Add, this, value), true);
		}

		public static Operand operator -(Operand left, Operand right)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Subtract, left, right), true);
		}

		public Operand Subtract(Operand value)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Subtract, this, value), true);
		}

		public static Operand operator *(Operand left, Operand right)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Multiply, left, right), true);
		}

		public Operand Multiply(Operand value)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Multiply, this, value), true);
		}

		public static Operand operator /(Operand left, Operand right)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Divide, left, right), true);
		}

		public Operand Divide(Operand value)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Divide, this, value), true);
		}

		public static Operand operator %(Operand left, Operand right)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Modulus, left, right), true);
		}

		public Operand Modulus(Operand value)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Modulus, this, value), true);
		}

		public static Operand operator &(Operand left, Operand right)
		{
			if ((object)left != null && left._logical)
			{
				left._logical = false;
				return left.LogicalAnd(right);
			}

			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.And, left, right), true);
		}

		public Operand BitwiseAnd(Operand value)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.And, this, value), true);
		}

		public static Operand operator |(Operand left, Operand right)
		{
			if ((object)left != null && left._logical)
			{
				left._logical = false;
				return OperandExtensions.SetLeakedState(left.LogicalOr(right), true);
			}

			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Or, left, right), true);
		}

		public Operand BitwiseOr(Operand value)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Or, this, value), true);
		}

		public static Operand operator ^(Operand left, Operand right)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Xor, left, right), true);
		}

		public Operand Xor(Operand value)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Xor, this, value), true);
		}

		public static Operand operator <<(Operand left, int right)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.LeftShift, left, right), true);
		}

		public Operand LeftShift(Operand value)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.LeftShift, this, value), true);
		}

		public static Operand operator >>(Operand left, int right)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.RightShift, left, right), true);
		}

		public Operand RightShift(Operand value)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.RightShift, this, value), true);
		}

		public static Operand operator +(Operand op)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Plus, op), true);
		}

		public Operand Plus()
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Plus, this), true);
		}

		public static Operand operator -(Operand op)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Minus, op), true);
		}

		public Operand Negate()
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Minus, this), true);
		}

		public static Operand operator !(Operand op)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.LogicalNot, op), true);
		}

		public Operand LogicalNot()
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.LogicalNot, this), true);
		}

		public static Operand operator ~(Operand op)
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Not, op), true);
		}

		public Operand OnesComplement()
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.Not, this), true);
		}

		public Operand Pow2()
		{
			return OperandExtensions.SetLeakedState(new SimpleOperation(this, OpCodes.Dup, OpCodes.Mul), true);
		}

		public Operand LogicalAnd(Operand other)
		{
            return OperandExtensions.SetLeakedState(Conditional(other, false), true);
		}

		public Operand LogicalOr(Operand other)
		{
			return OperandExtensions.SetLeakedState(Conditional(true, other), true);
		}

		public Operand PostIncrement()
		{
			return OperandExtensions.SetLeakedState(new PostfixOperation(Operator.Increment, this), true);
		}

		public Operand PostDecrement()
		{
			return OperandExtensions.SetLeakedState(new PostfixOperation(Operator.Decrement, this), true);
		}

		public Operand PreIncrement()
		{
			return OperandExtensions.SetLeakedState(new PrefixOperation(Operator.Increment, this), true);
		}

		public Operand PreDecrement()
		{
			return OperandExtensions.SetLeakedState(new PrefixOperation(Operator.Decrement, this), true);
		}

		public Operand IsTrue()
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.True, this), true);
		}

		public Operand IsFalse()
		{
			return OperandExtensions.SetLeakedState(new OverloadableOperation(Operator.False, this), true);
		}
		#endregion

		#region Logical operations
		bool _logical;
        
	    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "The operator is provided for convenience, so that the && and || operators work correctly. It should not be invoked under any other circumstances.")]
		public static bool operator true(Operand op)
		{
			if ((object)op != null)
				op._logical = true;
			return false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "The operator is provided for convenience, so that the && and || operators work correctly. It should not be invoked under any other circumstances.")]
		public static bool operator false(Operand op)
		{
			if ((object)op != null)
				op._logical = true;
			return false;
		}
		#endregion
		#region Special operations
		public Operand Conditional(Operand ifTrue, Operand ifFalse)
		{
			return OperandExtensions.SetLeakedState(new Conditional(this, ifTrue, ifFalse), true);
		}

	    public Operand Cast(Type type)
		{
			return OperandExtensions.SetLeakedState(new Cast(this, type), true);
		}

	    public Operand As(Type type)
	    {
	        return OperandExtensions.SetLeakedState(new SafeCast(this, type), true);
	    }

	    public Operand Is(Type type)
	    {
            return OperandExtensions.SetLeakedState(new IsInstanceOf(this, type), true);
        }

        public ContextualOperand InvokeGetType(ITypeMapper typeMapper)
        {
            return Invoke("GetType", typeMapper);
        }

        public ContextualOperand InvokeToString(ITypeMapper typeMapper)
        {
            return Invoke("ToString", typeMapper);
        }

        public ContextualOperand InvokeGetHashCode(ITypeMapper typeMapper)
        {
            return Invoke("GetHashCode", typeMapper);
        }

        #endregion

        #region Member access
        protected internal virtual BindingFlags GetBindingFlags()
		{
			return BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		}

        protected internal static Type GetType(Operand op, ITypeMapper typeMapper)
		{
			if ((object)op == null)
				return null;

			return op.GetReturnType(typeMapper);
		}

        protected internal static Type[] GetTypes(Operand[] ops, ITypeMapper typeMapper)
		{
			if (ops == null)
				return null;

			Type[] types = new Type[ops.Length];
			for (int i = 0; i < ops.Length; i++)
				types[i] = (object)ops[i] == null ? null : ops[i].GetReturnType(typeMapper);

			return types;
		}
        
        public ContextualOperand Field(string name, ITypeMapper typeMapper)
		{
			return new ContextualOperand(new Field((FieldInfo)typeMapper.TypeInfo.FindField(GetReturnType(typeMapper), name, IsStaticTarget).Member, this), typeMapper);
        }

        public ContextualOperand Property(string name, ITypeMapper typeMapper)
		{
			return Property(name, typeMapper, EmptyArray);
		}
        
		public ContextualOperand Property(string name, ITypeMapper typeMapper, params Operand[] indexes)
		{
			return new ContextualOperand(new Property(typeMapper.TypeInfo.FindProperty(GetReturnType(typeMapper), name, indexes, IsStaticTarget), this, indexes), typeMapper);
        }
        
		public ContextualOperand Invoke(string name, ITypeMapper typeMapper)
		{
			return Invoke(name, typeMapper, EmptyArray);
		}

		public ContextualOperand Invoke(string name, ITypeMapper typeMapper, params Operand[] args)
		{
			return OperandExtensions.SetLeakedState(new ContextualOperand(new Invocation(typeMapper.TypeInfo.FindMethod(GetReturnType(typeMapper), name, args, IsStaticTarget), this, args), typeMapper), true);
        }

		public ContextualOperand Invoke(MethodInfo method, ITypeMapper typeMapper, params Operand[] args)
		{
			return OperandExtensions.SetLeakedState(new ContextualOperand(new Invocation(typeMapper.TypeInfo.FindMethod(method), this, args), typeMapper), true);
        }

		public ContextualOperand InvokeDelegate(ITypeMapper typeMapper)
		{
			return InvokeDelegate(typeMapper, EmptyArray);
		}

		public ContextualOperand InvokeDelegate(ITypeMapper typeMapper, params Operand[] args)
		{
			return Invoke("Invoke", typeMapper, args);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers", Justification = "Intentional, to simulate standard indexer 'feel'")]
		public ContextualOperand this[ITypeMapper typeMapper, params Operand[] indexes]
		{
			get
			{
				if (GetReturnType(typeMapper).IsArray)
					return new ContextualOperand(new ArrayAccess(this, indexes), typeMapper);

				return Property(null, typeMapper, indexes);
			}
		}

		public Operand ArrayLength()
		{
			return OperandExtensions.SetLeakedState(new ArrayLength(this, false), true);
		}

		public Operand LongArrayLength()
		{
			return OperandExtensions.SetLeakedState(new ArrayLength(this, true), true);
		}

        public Operand UnwrapNullableValue(ITypeMapper typeMapper)
        {
            Type t = Helpers.GetNullableUnderlyingType(GetReturnType(typeMapper));
            if (t == null) return this;
            return Cast(t);
        }

		#endregion

		public Operand Ref()
		{
			return new Reference(this);
		}

		class Reference : Operand
		{
            protected override bool DetectsLeaking => false;

            readonly Operand _op;
            protected override void ResetLeakedStateRecursively()
            {
                base.ResetLeakedStateRecursively();
                OperandExtensions.SetLeakedState(_op, false);
            }


            public Reference(Operand op)
		    {
		        _op = op;
		    }

			protected internal override void EmitAddressOf(CodeGen g)
		    {
		        OperandExtensions.SetLeakedState(this, false);  
				_op.EmitAddressOf(g);
			}

		    public override Type GetReturnType(ITypeMapper typeMapper) => _op.GetReturnType(typeMapper).MakeByRefType();
		}

        ~Operand()
        {
            // yes, we throw exception in finalizer because we need to alert
            if (LeakedState && _detectsLeaks)
            {
				string message = "RunSharp: a possible leak of operand " + ((this as ContextualOperand)?.GetInternalOperandType() ?? this.GetType()).Name
                                 + " detected, see Operand.SetNotLeaked() if it's not the case. " +
                                 "Operand creation stack trace "
                                 + (_leakedStateStack != null ? "\r\n" + _leakedStateStack : " may be enabled with RunSharpDebug.StoreLeakingStackTrace = LeakingDetectionMode.DetectAndCaptureStack");
                RunSharpDebug.StoreLeak(message);
                if (RunSharpDebug.LeakingDetection != LeakingDetectionMode.StoreAndContinue)
                    throw new InvalidOperationException(message);
            }
        }

        StackTrace _leakedStateStack;

        /// <summary>
        /// This is set from internal things
        /// </summary>
	    bool _leakedState;
        
        /// <summary>
        /// Set by ineritors, affects only final check
        /// </summary>
	    bool _detectsLeaks;

        /// <summary>
        /// If false at construction, exception won't be throw in leak detection case
        /// </summary>
        protected virtual bool DetectsLeaking => true;
        
        protected internal bool LeakedState
	    {
            [DebuggerStepThrough] get { return _leakedState; }
            [DebuggerStepThrough]
            set
            {
                if (_leakedState == value) return;
                if (value)
                {
                    _leakedState = true;
                    if (_detectsLeaks && (RunSharpDebug.LeakingDetection == LeakingDetectionMode.DetectAndCaptureStack || RunSharpDebug.LeakingDetection == LeakingDetectionMode.DetectAndCaptureStackWithFiles))
#if SILVERLIGHT
                        _leakedStateStack = new StackTrace();
#else
                        _leakedStateStack = new StackTrace(3, RunSharpDebug.LeakingDetection == LeakingDetectionMode.DetectAndCaptureStackWithFiles);
#endif
                    SetLeakedStateRecursively();
                }
                else
                {
                    _leakedState = false;
                    ResetLeakedStateRecursively();
                }
            }
	    }

		/// <summary>
		/// Set not leaked for this and *all used operands recursively*. Returns itself.
		/// </summary>
		/// <remarks>Usage: <br/>
		/// <code>
		/// var asStream = ag.ExpressionFactory.New(typeof(MemoryStream)).Cast(typeof(Stream)).SetNotLeaked(false)();
		/// </code>
		/// </remarks>
		public Operand SetNotLeaked()
        {
            LeakedState = false;
            return this;
        }

        /// <summary>
        /// Called even when leaking status ignored
        /// </summary>
        [DebuggerStepThrough]
        protected virtual void SetLeakedStateRecursively()
	    {
	        
	    }

        [DebuggerStepThrough]
        protected virtual void ResetLeakedStateRecursively()
	    {
	        
	    }

	    protected ILGenerator GetILGenerator(CodeGen g)
	    {
	        return g.IL;
	    }
	}
}