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

namespace TriAxis.RunSharp.Operands
{
    class Conditional : Operand
    {
        Operand _cond;
        readonly Operand _ifTrue;
        readonly Operand _ifFalse;

        protected override void ResetLeakedStateRecursively()
        {
            base.ResetLeakedStateRecursively();
            _cond.SetLeakedState(false);
            _ifFalse.SetLeakedState(false);
            _ifTrue.SetLeakedState(false);
        }

        public Conditional(Operand cond, Operand ifTrue, Operand ifFalse)
        {
            _cond = cond;
            _ifTrue = ifTrue;
            _ifFalse = ifFalse;
        }

        bool _initialized;

        void Initialize(ITypeMapper typeMapper)
        {
            if (_initialized) return;
            _initialized = true;

            // TODO: proper checking as in specification
            if (_ifTrue.GetReturnType(typeMapper) != _ifFalse.GetReturnType(typeMapper))
                throw new ArgumentException(Properties.Messages.ErrInvalidConditionalVariants);

            if (_cond.GetReturnType(typeMapper) != typeMapper.MapType(typeof(bool))) _cond = _cond.IsTrue();
        }

        protected internal override void EmitGet(CodeGen g)  
        {
		    this.SetLeakedState(false); 
            Initialize(g.TypeMapper);
            Label lbTrue = g.IL.DefineLabel();
            Label lbEnd = g.IL.DefineLabel();

            var lbFalse = new OptionalLabel(g.IL);

            _cond.EmitBranch(g, lbTrue, lbFalse);
            if (lbFalse.IsLabelExist)
                g.IL.MarkLabel(lbFalse.Value);
            _ifFalse.EmitGet(g);
            g.IL.Emit(OpCodes.Br, lbEnd);
            g.IL.MarkLabel(lbTrue);
            _ifTrue.EmitGet(g);
            g.IL.MarkLabel(lbEnd);
        }

        protected internal override void EmitBranch(CodeGen g, OptionalLabel labelTrue, OptionalLabel labelFalse)
        {
            this.SetNotLeaked();
            
            // try handle And/Or
            bool handled = false;
            var lit = _ifTrue as IntLiteral;
            if (!ReferenceEquals(lit, null) && lit.Value == 1 && lit.GetReturnType(g.TypeMapper) == g.TypeMapper.MapType(typeof(bool)))
            {
                EmitOr(g, labelTrue, labelFalse, _cond, _ifFalse);
                handled = true;
            }
            else if (ReferenceEquals(lit, null))
            {
                lit = _ifFalse as IntLiteral;
                if (!ReferenceEquals(lit, null) && lit.Value == 0 && lit.GetReturnType(g.TypeMapper) == g.TypeMapper.MapType(typeof(bool)))
                {
                    EmitAnd(g, labelTrue, labelFalse, _cond, _ifTrue);
                    handled = true;
                }
            }

            if (!handled) base.EmitBranch(g, labelTrue, labelFalse);
        }

        void EmitOr(CodeGen g, OptionalLabel labelTrue, OptionalLabel labelFalse, Operand first, Operand second)
        {
            var falseOptional = new OptionalLabel(g.IL);
            labelTrue.EnsureExists();
            first.EmitBranch(g, labelTrue, falseOptional);
            if (falseOptional.IsLabelExist) // it can jump out of internal And on first false but we still may hope on second
                g.IL.MarkLabel(falseOptional.Value);
            second.EmitBranch(g, labelTrue, labelFalse);
        }

        void EmitAnd(CodeGen g, OptionalLabel labelTrue,OptionalLabel labelFalse, Operand first, Operand second)
        {
            var trueOptional = new OptionalLabel(g.IL);
            labelFalse.EnsureExists();
            first.EmitBranch(g, trueOptional, labelFalse);
            if (trueOptional.IsLabelExist) // it can jump out of internal Or on first true but we still need to check second
                g.IL.MarkLabel(trueOptional.Value);
            second.EmitBranch(g, labelTrue, labelFalse);
        }

        public override Type GetReturnType(ITypeMapper typeMapper) => GetType(_ifTrue, typeMapper);
    }
}