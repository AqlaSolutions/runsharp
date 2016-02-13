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
    class SafeCast : Operand
    {
        readonly Operand _op;
        readonly Type _t;

        protected override void ResetLeakedStateRecursively()
        {
            OperandExtensions.SetLeakedState(_op, false);
            base.ResetLeakedStateRecursively();
        }

        readonly Operand _conditional;

        public SafeCast(Operand op, Type t)
        {
            _op = op;
            _t = t;
            if (t.IsValueType)
                _conditional = _op.Is(_t).Conditional(_op.Cast(_t), new DefaultValue(_t));
        }

        protected internal override void EmitGet(CodeGen g)
        {
            OperandExtensions.SetLeakedState(this, false);
            if (_t.IsValueType)
            {
                _conditional.EmitGet(g);
                return;
            }
            _op.EmitGet(g);
            g.IL.Emit(OpCodes.Isinst, _t);
        }

        public override Type GetReturnType(ITypeMapper typeMapper) => _t;
    }
}