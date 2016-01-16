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
	class OverloadableOperation : Operand
	{
	    readonly Operator _op;
	    readonly Operand[] _operands;
	    ApplicableFunction _af;

        protected override void ResetLeakedStateRecursively()
        {
            base.ResetLeakedStateRecursively();
            _operands.SetLeakedState(false);
        }

        public OverloadableOperation(Operator op, params Operand[] operands)
		{
			_op = op;
			_operands = operands;
		}

		internal void SetOperand(Operand newOp)
		{
			_operands[0] = newOp;
		}

	    void PrepareAf(ITypeMapper typeMapper)
	    {
	        if (_af != null) return;
	        List<ApplicableFunction> candidates = null;
            
	        foreach (Operand operand in _operands)
            {
                if ((object)operand != null && !operand.GetReturnType(typeMapper).IsPrimitive)
                {
                    // try overloads
                    candidates = _op.FindUserCandidates(typeMapper, _operands);
                    break;
                }
            }

            if (candidates == null)
                candidates = OverloadResolver.FindApplicable(_op.GetStandardCandidates(typeMapper, _operands), typeMapper, _operands);

            if (candidates == null)
                throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrInvalidOperation, _op.MethodName,
                    string.Join(", ", ConvertAll<Operand, string>(_operands, op=>op.GetReturnType(typeMapper).FullName))));

            _af = OverloadResolver.FindBest(candidates, typeMapper);

            if (_af == null)
                throw new AmbiguousMatchException(Properties.Messages.ErrAmbiguousBinding);
        }

        static TOut[] ConvertAll<TIn, TOut>(TIn[] array, Converter<TIn, TOut> converter)
        {
            TOut[] newArray = new TOut[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                newArray[i] = converter(array[i]);
            }
            return newArray;
        }

        protected internal override void EmitGet(CodeGen g)  
        {
		    this.SetLeakedState(false); 
		    PrepareAf(g.TypeMapper);
            _af.EmitArgs(g, _operands);

			IStandardOperation sop = _af.Method as IStandardOperation;
			if (sop != null)
				sop.Emit(g, _op);
			else
				g.IL.Emit(OpCodes.Call, (MethodInfo)_af.Method.Member);
		}

		protected internal override void EmitBranch(CodeGen g, BranchSet branchSet, Label label)
		{
		    this.SetLeakedState(false);  
            PrepareAf(g.TypeMapper);
            IStandardOperation stdOp = _af.Method as IStandardOperation;
			if (_op.BranchOp == 0 || stdOp == null)
			{
				base.EmitBranch(g, branchSet, label);
				return;
			}

			_af.EmitArgs(g, _operands);
			g.IL.Emit(branchSet.Get(_op.BranchOp, stdOp.IsUnsigned), label);
		}

	    public override Type GetReturnType(ITypeMapper typeMapper)
	    {
            PrepareAf(typeMapper);
	        return _af.Method.ReturnType;
	    }
	}
}
