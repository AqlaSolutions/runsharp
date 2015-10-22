using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using TryAxis.RunSharp;
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

namespace TriAxis.RunSharp.Operands
{
    public class ContextualAssignment : Assignment
    {
        public ITypeMapper TypeMapper { get; }
        
        public ContextualAssignment(Operand lvalue, Operand rvalue, bool allowExplicitConversion, ITypeMapper typeMapper)
            : base(lvalue, rvalue, allowExplicitConversion)
        {
            TypeMapper = typeMapper;
        }

        public new ContextualAssignment Assign(Operand value)
        {
            return Assign(value, false);
        }

        public new ContextualAssignment Assign(Operand value, bool allowExplicitConversion)
        {
            return new ContextualAssignment(this, value, allowExplicitConversion, TypeMapper);
        }

        public new ContextualOperand Eq(Operand value)
        {
            return new ContextualOperand(base.Eq(value), TypeMapper);
        }

        public new ContextualOperand Ne(Operand value)
        {
            return new ContextualOperand(base.Ne(value), TypeMapper);
        }

        public new ContextualOperand Lt(Operand value)
        {
            return new ContextualOperand(base.Lt(value), TypeMapper);
        }

        public new ContextualOperand Gt(Operand value)
        {
            return new ContextualOperand(base.Gt(value), TypeMapper);
        }

        public new ContextualOperand Ge(Operand value)
        {
            return new ContextualOperand(base.Ge(value), TypeMapper);
        }

        public new ContextualOperand Le(Operand value)
        {
            return new ContextualOperand(base.Le(value), TypeMapper);
        }

        public new ContextualOperand Add(Operand value)
        {
            return new ContextualOperand(base.Add(value), TypeMapper);
        }

        public new ContextualOperand Subtract(Operand value)
        {
            return new ContextualOperand(base.Subtract(value), TypeMapper);
        }

        public new ContextualOperand Multiply(Operand value)
        {
            return new ContextualOperand(base.Multiply(value), TypeMapper);
        }

        public new ContextualOperand Divide(Operand value)
        {
            return new ContextualOperand(base.Divide(value), TypeMapper);
        }

        public new ContextualOperand Modulus(Operand value)
        {
            return new ContextualOperand(base.Modulus(value), TypeMapper);
        }

        public new ContextualOperand BitwiseAnd(Operand value)
        {
            return new ContextualOperand(base.BitwiseAnd(value), TypeMapper);
        }

        public new ContextualOperand BitwiseOr(Operand value)
        {
            return new ContextualOperand(base.BitwiseOr(value), TypeMapper);
        }

        public new ContextualOperand Xor(Operand value)
        {
            return new ContextualOperand(base.Xor(value), TypeMapper);
        }

        public new ContextualOperand LeftShift(Operand value)
        {
            return new ContextualOperand(base.LeftShift(value), TypeMapper);
        }

        public new ContextualOperand RightShift(Operand value)
        {
            return new ContextualOperand(base.RightShift(value), TypeMapper);
        }

        public new ContextualOperand Plus()
        {
            return new ContextualOperand(base.Plus(), TypeMapper);
        }

        public new ContextualOperand Negate()
        {
            return new ContextualOperand(base.Negate(), TypeMapper);
        }

        public new ContextualOperand LogicalNot()
        {
            return new ContextualOperand(base.LogicalNot(), TypeMapper);
        }

        public new ContextualOperand OnesComplement()
        {
            return new ContextualOperand(base.OnesComplement(), TypeMapper);
        }

        public new ContextualOperand Pow2()
        {
            return new ContextualOperand(base.Pow2(), TypeMapper);
        }

        public new ContextualOperand LogicalAnd(Operand other)
        {
            return new ContextualOperand(base.LogicalAnd(other), TypeMapper);
        }

        public new ContextualOperand LogicalOr(Operand other)
        {
            return new ContextualOperand(base.LogicalOr(other), TypeMapper);
        }

        public new ContextualOperand PostIncrement()
        {
            return new ContextualOperand(base.PostIncrement(), TypeMapper);
        }

        public new ContextualOperand PostDecrement()
        {
            return new ContextualOperand(base.PostDecrement(), TypeMapper);
        }

        public new ContextualOperand PreIncrement()
        {
            return new ContextualOperand(base.PreIncrement(), TypeMapper);
        }

        public new ContextualOperand PreDecrement()
        {
            return new ContextualOperand(base.PreDecrement(), TypeMapper);
        }

        public new ContextualOperand IsTrue()
        {
            return new ContextualOperand(base.IsTrue(), TypeMapper);
        }

        public new ContextualOperand IsFalse()
        {
            return new ContextualOperand(base.IsFalse(), TypeMapper);
        }

        public new ContextualOperand Conditional(Operand ifTrue, Operand ifFalse)
        {
            return new ContextualOperand(base.Conditional(ifTrue, ifFalse), TypeMapper);
        }

        public new ContextualOperand Cast(Type type)
        {
            return new ContextualOperand(base.Cast(type), TypeMapper);
        }

        public new ContextualOperand ArrayLength()
        {
            return new ContextualOperand(base.ArrayLength(), TypeMapper);
        }

        public new ContextualOperand LongArrayLength()
        {
            return new ContextualOperand(base.LongArrayLength(), TypeMapper);
        }

        public new ContextualOperand Ref()
        {
            return new ContextualOperand(base.Ref(), TypeMapper);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't pass typeMapper", true)]
        public new ContextualOperand Field(string name, ITypeMapper typeMapper)
        {
            return Field(name);
        }

        public ContextualOperand Field(string name)
        {
            return base.Field(name, TypeMapper);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't pass typeMapper", true)]
        public new ContextualOperand Property(string name, ITypeMapper typeMapper)
        {
            return Property(name);
        }

        public ContextualOperand Property(string name)
        {
            return base.Property(name, TypeMapper);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't pass typeMapper", true)]
        public new ContextualOperand Property(string name, ITypeMapper typeMapper, params Operand[] indexes)
        {
            return Property(name, indexes);
        }

        public ContextualOperand Property(string name, params Operand[] indexes)
        {
            return base.Property(name, TypeMapper);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't pass typeMapper", true)]
        public new ContextualOperand Invoke(string name, ITypeMapper typeMapper)
        {
            return Invoke(name);
        }

        public ContextualOperand Invoke(string name)
        {
            return base.Invoke(name, TypeMapper);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't pass typeMapper", true)]
        public new ContextualOperand Invoke(string name, ITypeMapper typeMapper, params Operand[] args)
        {
            return Invoke(name, args);
        }

        public ContextualOperand Invoke(string name, params Operand[] args)
        {
            return base.Invoke(name, TypeMapper, args);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't pass typeMapper", true)]
        public new ContextualOperand InvokeDelegate(ITypeMapper typeMapper)
        {
            return InvokeDelegate();
        }

        public ContextualOperand InvokeDelegate()
        {
            return base.InvokeDelegate(TypeMapper);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't pass typeMapper", true)]
        public new ContextualOperand InvokeDelegate(ITypeMapper typeMapper, params Operand[] args)
        {
            return InvokeDelegate(args);
        }

        public ContextualOperand InvokeDelegate(params Operand[] args)
        {
            return base.InvokeDelegate(TypeMapper, args);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't pass typeMapper", true)]
        public new Operand this[ITypeMapper typeMapper, params Operand[] indexes] => this[indexes];
        public ContextualOperand this[params Operand[] indexes] => base[TypeMapper, indexes];
    }
}