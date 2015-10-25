/*
Copyright(c) 2015, Vladyslav Taranov

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
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using TriAxis.RunSharp;
using TriAxis.RunSharp.Operands;
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

namespace TryAxis.RunSharp
{
    public class ContextualOperand : Operand
    {
        readonly Operand _operand;
        public ITypeMapper TypeMapper { get; }

        internal ContextualOperand(Operand operand, ITypeMapper typeMapper)
        {
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            _operand = operand;
            TypeMapper = typeMapper;
        }

        public override Type GetReturnType(ITypeMapper typeMapper)
        {
            return _operand.GetReturnType(typeMapper);
        }

        internal override void EmitGet(CodeGen g)
        {
            _operand.EmitGet(g);
        }

        internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
        {
            _operand.EmitSet(g, value, allowExplicitConversion);
        }

        internal override void EmitAddressOf(CodeGen g)
        {
            _operand.EmitAddressOf(g);
        }

        internal override void EmitBranch(CodeGen g, BranchSet branchSet, Label label)
        {
            _operand.EmitBranch(g, branchSet, label);
        }

        internal override BindingFlags GetBindingFlags()
        {
            return _operand.GetBindingFlags();
        }

        internal override bool TrivialAccess => _operand.TrivialAccess;

        internal override bool IsStaticTarget => _operand.IsStaticTarget;

        internal override bool SuppressVirtual => _operand.SuppressVirtual;

        internal override object ConstantValue => _operand.ConstantValue;

        internal override void AssignmentHint(Operand op)
        {
            _operand.AssignmentHint(op);
        }

        public new ContextualAssignment Assign(Operand value)
        {
            return Assign(value, false);
        }

        public new ContextualAssignment Assign(Operand value, bool allowExplicitConversion)
        {
            return new ContextualAssignment(new Assignment(this, value, allowExplicitConversion), TypeMapper);
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

#if FEAT_IKVM

        public Operand Cast(System.Type type)
        {
            return Cast(TypeMapper.MapType(type));
        }
        
#endif

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
        
        public static ContextualOperand operator ==(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left == right, left.TypeMapper);
        }

        public static ContextualOperand operator ==(Operand left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left == (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator ==(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left == (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator !=(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left != right, left.TypeMapper);
        }

        public static ContextualOperand operator !=(Operand  left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left != (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator !=(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left != (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator <(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left < right, left.TypeMapper);
        }
        
        public static ContextualOperand operator <(Operand  left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left < (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator <(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left < (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator >(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left > right, left.TypeMapper);
        }
        
        public static ContextualOperand operator >(Operand  left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left > (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator >(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left > (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator >=(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left >= right, left.TypeMapper);
        }
        
        public static ContextualOperand operator >=(Operand  left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left >= (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator >=(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left >= (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator <=(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left <= right, left.TypeMapper);
        }
        
        public static ContextualOperand operator <=(Operand  left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left <= (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator <=(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left <= (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator +(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left + right, left.TypeMapper);
        }
        
        public static ContextualOperand operator +(Operand  left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left + (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator +(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left + (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator -(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left - right, left.TypeMapper);
        }

        public static ContextualOperand operator -(Operand right, ContextualOperand  left)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand(right - (Operand)left, left.TypeMapper);
        }

        public static ContextualOperand operator -(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left - (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator *(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left * right, left.TypeMapper);
        }
        
        public static ContextualOperand operator *(Operand right, ContextualOperand  left)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand(right * (Operand)left, left.TypeMapper);
        }

        public static ContextualOperand operator *(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left * (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator /(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left / right, left.TypeMapper);
        }
        
        public static ContextualOperand operator /(Operand right, ContextualOperand  left)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand(right / (Operand)left, left.TypeMapper);
        }

        public static ContextualOperand operator /(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left / (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator %(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left % right, left.TypeMapper);
        }
        
        public static ContextualOperand operator %(Operand right, ContextualOperand  left)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand(right % (Operand)left, left.TypeMapper);
        }

        public static ContextualOperand operator %(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left % (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator &(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left & right, left.TypeMapper);
        }
        
        public static ContextualOperand operator &(Operand right, ContextualOperand  left)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand(right & (Operand)left, left.TypeMapper);
        }

        public static ContextualOperand operator &(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left & (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator |(ContextualOperand  left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left | right, left.TypeMapper);
        }

        public static ContextualOperand operator |(Operand right, ContextualOperand  left)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand(right | (Operand)left, left.TypeMapper);
        }

        public static ContextualOperand operator |(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left | (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator ^(ContextualOperand  left, Operand right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            return new ContextualOperand((Operand)left ^ right, left.TypeMapper);
        }

        public static ContextualOperand operator ^(Operand right, ContextualOperand  left)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            return new ContextualOperand(right ^ (Operand)left, left.TypeMapper);
        }

        public static ContextualOperand operator ^(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left ^ (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator <<(ContextualOperand  left, int right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            return new ContextualOperand((Operand)left << right, left.TypeMapper);
        }

        public static ContextualOperand operator >>(ContextualOperand  left, int right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            return new ContextualOperand((Operand)left >> right, left.TypeMapper);
        }

        public static ContextualOperand operator +(ContextualOperand op)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            return new ContextualOperand(+(Operand)op, op.TypeMapper);
        }

        public static ContextualOperand operator -(ContextualOperand op)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            return new ContextualOperand(-(Operand)op, op.TypeMapper);
        }

        public static ContextualOperand operator !(ContextualOperand op)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            return new ContextualOperand(!(Operand)op, op.TypeMapper);
        }

        public static ContextualOperand operator ~(ContextualOperand op)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            return new ContextualOperand(~(Operand)op, op.TypeMapper);
        }

        static void ThrowIfOperandNull(ContextualOperand left)
        {
            if ((object)left == null) throw new ArgumentNullException("operand", "You should cast null to Operand base type because null can't be ContextualOperand");
        }

        static void ThrowIfOperandNull(ContextualOperand left, ContextualOperand right)
        {
            if ((object)left == null && (object)right == null) throw new ArgumentNullException("operand", "You should cast null to Operand base type because null can't be ContextualOperand");
        }
    }
}