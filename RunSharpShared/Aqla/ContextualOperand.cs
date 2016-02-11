/*
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

namespace TriAxis.RunSharp
{
    public class ContextualOperand : Operand
    {
        internal System.Type GetInternalOperandType()
        {
            return (_operand as ContextualOperand)?.GetInternalOperandType() ?? _operand.GetType();
        }

        protected override bool DetectsLeaking => false;

        protected override void ResetLeakedStateRecursively()
        {
            base.ResetLeakedStateRecursively();
            OperandExtensions.SetLeakedState(_operand, false);
        }

        protected override void SetLeakedStateRecursively()
        {
            _operand.LeakedState = true;
            base.SetLeakedStateRecursively();
        }

        readonly Operand _operand;
        public ITypeMapper TypeMapper { get; }

        [DebuggerStepThrough]
        public ContextualOperand(Operand operand, ITypeMapper typeMapper)
        {
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            _operand = operand;
            TypeMapper = typeMapper;
        }

        [DebuggerStepThrough]
        public override Type GetReturnType(ITypeMapper typeMapper)
        {
            OperandExtensions.SetLeakedState(this, false);
            return _operand.GetReturnType(typeMapper);
        }

        public Type GetReturnType()
        {
            return GetReturnType(TypeMapper);
        }

        [DebuggerStepThrough]
        protected internal override void EmitGet(CodeGen g)  
        {
		    OperandExtensions.SetLeakedState(this, false); 
            _operand.EmitGet(g);
        }

        [DebuggerStepThrough]
        protected internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
		    OperandExtensions.SetLeakedState(this, false);  
            OperandExtensions.SetLeakedState(this, false);
            _operand.EmitSet(g, value, allowExplicitConversion);
        }

        [DebuggerStepThrough]
        protected internal override void EmitAddressOf(CodeGen g)
		{
		    OperandExtensions.SetLeakedState(this, false);  
            _operand.EmitAddressOf(g);
        }

        [DebuggerStepThrough]
        protected internal override void EmitBranch(CodeGen g, OptionalLabel labelTrue, OptionalLabel labelFalse)
		{
		    OperandExtensions.SetLeakedState(this, false); 
            _operand.EmitBranch(g, labelTrue, labelFalse);
        }

        [DebuggerStepThrough]
        protected internal override BindingFlags GetBindingFlags()
        {
            OperandExtensions.SetLeakedState(this, false);
            return _operand.GetBindingFlags();
        }

        protected internal override bool TrivialAccess
        { [DebuggerStepThrough] get { OperandExtensions.SetLeakedState(this, false); return _operand.TrivialAccess; } }

        protected internal override bool IsStaticTarget
        { [DebuggerStepThrough] get { OperandExtensions.SetLeakedState(this, false); return _operand.IsStaticTarget; } }

        protected internal override bool SuppressVirtual
        { [DebuggerStepThrough] get { OperandExtensions.SetLeakedState(this, false); return _operand.SuppressVirtual; } }

        protected internal override object ConstantValue
        { [DebuggerStepThrough] get { OperandExtensions.SetLeakedState(this, false); return _operand.ConstantValue; } }

        [DebuggerStepThrough]
        protected internal override void AssignmentHint(Operand op)
        {
            OperandExtensions.SetLeakedState(this, false);
            _operand.AssignmentHint(op);
        }

        public new ContextualAssignment Assign(Operand value)
        {
            return Assign(value, false);
        }

        public new ContextualAssignment Assign(Operand value, bool allowExplicitConversion)
        {
            return OperandExtensions.SetLeakedState(new ContextualAssignment(new Assignment(this, value, allowExplicitConversion), TypeMapper), true);
        }

        public ContextualOperand InvokeReferenceEquals(Operand right)
        {
            return base.InvokeReferenceEquals(right, TypeMapper);
        }

        [Obsolete("Don't pass typeMapper", true)]
        public new ContextualOperand InvokeReferenceEquals(Operand right, ITypeMapper typeMapper)
        {
            return base.InvokeReferenceEquals(right, TypeMapper);
        }

        public ContextualOperand InvokeEquals(Operand right)
        {
            return base.InvokeEquals(right, TypeMapper);
        }

        [Obsolete("Don't pass typeMapper", true)]
        public new ContextualOperand InvokeEquals(Operand right, ITypeMapper typeMapper)
        {
            return base.InvokeEquals(right, TypeMapper);
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
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Add(value), TypeMapper), true);
        }

        public new ContextualOperand Subtract(Operand value)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Subtract(value), TypeMapper), true);
        }

        public new ContextualOperand Multiply(Operand value)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Multiply(value), TypeMapper), true);
        }

        public new ContextualOperand Divide(Operand value)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Divide(value), TypeMapper), true);
        }

        public new ContextualOperand Modulus(Operand value)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Modulus(value), TypeMapper), true);
        }

        public new ContextualOperand BitwiseAnd(Operand value)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.BitwiseAnd(value), TypeMapper), true);
        }

        public new ContextualOperand BitwiseOr(Operand value)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.BitwiseOr(value), TypeMapper), true);
        }

        public new ContextualOperand Xor(Operand value)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Xor(value), TypeMapper), true);
        }

        public new ContextualOperand LeftShift(Operand value)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.LeftShift(value), TypeMapper), true);
        }

        public new ContextualOperand RightShift(Operand value)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.RightShift(value), TypeMapper), true);
        }

        public new ContextualOperand Plus()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Plus(), TypeMapper), true);
        }

        public new ContextualOperand Negate()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Negate(), TypeMapper), true);
        }

        public new ContextualOperand LogicalNot()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.LogicalNot(), TypeMapper), true);
        }

        public new ContextualOperand OnesComplement()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.OnesComplement(), TypeMapper), true);
        }

        public new ContextualOperand Pow2()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Pow2(), TypeMapper), true);
        }

        public new ContextualOperand LogicalAnd(Operand other)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.LogicalAnd(other), TypeMapper), true);
        }

        public new ContextualOperand LogicalOr(Operand other)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.LogicalOr(other), TypeMapper), true);
        }

        public new ContextualOperand PostIncrement()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.PostIncrement(), TypeMapper), true);
        }

        public new ContextualOperand PostDecrement()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.PostDecrement(), TypeMapper), true);
        }

        public new ContextualOperand PreIncrement()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.PreIncrement(), TypeMapper), true);
        }

        public new ContextualOperand PreDecrement()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.PreDecrement(), TypeMapper), true);
        }

        public new ContextualOperand IsTrue()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.IsTrue(), TypeMapper), true);
        }

        public new ContextualOperand IsFalse()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.IsFalse(), TypeMapper), true);
        }

        public new ContextualOperand Conditional(Operand ifTrue, Operand ifFalse)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Conditional(ifTrue, ifFalse), TypeMapper), true);
        }

#if FEAT_IKVM

        public Operand Cast(System.Type type)
        {
            return OperandExtensions.SetLeakedState(Cast(TypeMapper.MapType(type)), true);
        }
        
#endif

        public new ContextualOperand Cast(Type type)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Cast(type), TypeMapper), true);
        }

#if FEAT_IKVM
        public new ContextualOperand As(System.Type type)
        {
            return As(TypeMapper.MapType(type));
        }
#endif
        public new ContextualOperand As(Type type)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.As(type), TypeMapper), true);
        }
#if FEAT_IKVM
        public new ContextualOperand Is(System.Type type)
        {
            return Is(TypeMapper.MapType(type));
        }
#endif
        public new ContextualOperand Is(Type type)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.Is(type), TypeMapper), true);
        }

        public ContextualOperand InvokeGetType()
        {
            return base.InvokeGetType(TypeMapper);
        }
        
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't pass typeMapper", true)]
        public new Operand InvokeGetType(ITypeMapper typeMapper)
        {
            return InvokeGetType();
        }

        public new ContextualOperand ArrayLength()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.ArrayLength(), TypeMapper), true);
        }

        public new ContextualOperand LongArrayLength()
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(base.LongArrayLength(), TypeMapper), true);
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

        public static ContextualOperand operator !=(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left != right, left.TypeMapper);
        }

        public static ContextualOperand operator !=(Operand left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left != (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator !=(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left != (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator <(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left < right, left.TypeMapper);
        }

        public static ContextualOperand operator <(Operand left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left < (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator <(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left < (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator >(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left > right, left.TypeMapper);
        }

        public static ContextualOperand operator >(Operand left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left > (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator >(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left > (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator >=(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left >= right, left.TypeMapper);
        }

        public static ContextualOperand operator >=(Operand left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left >= (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator >=(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left >= (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator <=(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return new ContextualOperand((Operand)left <= right, left.TypeMapper);
        }

        public static ContextualOperand operator <=(Operand left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return new ContextualOperand(left <= (Operand)right, right.TypeMapper);
        }

        public static ContextualOperand operator <=(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return new ContextualOperand((Operand)left <= (Operand)right, left?.TypeMapper ?? right.TypeMapper);
        }

        public static ContextualOperand operator +(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left + right, left.TypeMapper), true);
        }

        public static ContextualOperand operator +(Operand left, ContextualOperand right)
        {
            ThrowIfOperandNull(right);
            return OperandExtensions.SetLeakedState(new ContextualOperand(left + (Operand)right, right.TypeMapper), true);
        }

        public static ContextualOperand operator +(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left + (Operand)right, left?.TypeMapper ?? right.TypeMapper), true);
        }

        public static ContextualOperand operator -(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left - right, left.TypeMapper), true);
        }

        public static ContextualOperand operator -(Operand right, ContextualOperand left)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand(right - (Operand)left, left.TypeMapper), true);
        }

        public static ContextualOperand operator -(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left - (Operand)right, left?.TypeMapper ?? right.TypeMapper), true);
        }

        public static ContextualOperand operator *(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left * right, left.TypeMapper), true);
        }

        public static ContextualOperand operator *(Operand right, ContextualOperand left)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand(right * (Operand)left, left.TypeMapper), true);
        }

        public static ContextualOperand operator *(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left * (Operand)right, left?.TypeMapper ?? right.TypeMapper), true);
        }

        public static ContextualOperand operator /(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left / right, left.TypeMapper), true);
        }

        public static ContextualOperand operator /(Operand right, ContextualOperand left)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand(right / (Operand)left, left.TypeMapper), true);
        }

        public static ContextualOperand operator /(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left / (Operand)right, left?.TypeMapper ?? right.TypeMapper), true);
        }

        public static ContextualOperand operator %(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left % right, left.TypeMapper), true);
        }

        public static ContextualOperand operator %(Operand right, ContextualOperand left)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand(right % (Operand)left, left.TypeMapper), true);
        }

        public static ContextualOperand operator %(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left % (Operand)right, left?.TypeMapper ?? right.TypeMapper), true);
        }

        public static ContextualOperand operator &(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left & right, left.TypeMapper), true);
        }

        public static ContextualOperand operator &(Operand right, ContextualOperand left)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand(right & (Operand)left, left.TypeMapper), true);
        }

        public static ContextualOperand operator &(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left & (Operand)right, left?.TypeMapper ?? right.TypeMapper), true);
        }

        public static ContextualOperand operator |(ContextualOperand left, Operand right)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left | right, left.TypeMapper), true);
        }

        public static ContextualOperand operator |(Operand right, ContextualOperand left)
        {
            ThrowIfOperandNull(left);
            return OperandExtensions.SetLeakedState(new ContextualOperand(right | (Operand)left, left.TypeMapper), true);
        }

        public static ContextualOperand operator |(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left | (Operand)right, left?.TypeMapper ?? right.TypeMapper), true);
        }

        public static ContextualOperand operator ^(ContextualOperand left, Operand right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left ^ right, left.TypeMapper), true);
        }

        public static ContextualOperand operator ^(Operand right, ContextualOperand left)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            return OperandExtensions.SetLeakedState(new ContextualOperand(right ^ (Operand)left, left.TypeMapper), true);
        }

        public static ContextualOperand operator ^(ContextualOperand left, ContextualOperand right)
        {
            ThrowIfOperandNull(left, right);
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left ^ (Operand)right, left?.TypeMapper ?? right.TypeMapper), true);
        }

        public static ContextualOperand operator <<(ContextualOperand left, int right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left << right, left.TypeMapper), true);
        }

        public static ContextualOperand operator >>(ContextualOperand left, int right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            return OperandExtensions.SetLeakedState(new ContextualOperand((Operand)left >> right, left.TypeMapper), true);
        }

        public static ContextualOperand operator +(ContextualOperand op)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            return OperandExtensions.SetLeakedState(new ContextualOperand(+(Operand)op, op.TypeMapper), true);
        }

        public static ContextualOperand operator -(ContextualOperand op)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            return OperandExtensions.SetLeakedState(new ContextualOperand(-(Operand)op, op.TypeMapper), true);
        }

        public static ContextualOperand operator !(ContextualOperand op)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            return OperandExtensions.SetLeakedState(new ContextualOperand(!(Operand)op, op.TypeMapper), true);
        }

        public static ContextualOperand operator ~(ContextualOperand op)
        {
            if (op == null) throw new ArgumentNullException(nameof(op));
            return OperandExtensions.SetLeakedState(new ContextualOperand(~(Operand)op, op.TypeMapper), true);
        }

        static void ThrowIfOperandNull(ContextualOperand left)
        {
            if ((object)left == null) throw new ArgumentNullException("operand", "You should cast null to Operand base type because null can't be ContextualOperand");
        }

        static void ThrowIfOperandNull(ContextualOperand left, ContextualOperand right)
        {
            if ((object)left == null && (object)right == null) throw new ArgumentNullException("operand", "You should cast null to Operand base type because null can't be ContextualOperand");
        }

        public override string ToString()
        {
            return "C+" + _operand.ToString();
        }
    }
}