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

            // value type handling
            if (_operands.Length == 2)
            {
                var valueOperand = _operands[0] ?? _operands[1];
                Type returnType = valueOperand?.GetReturnType(g.TypeMapper);
                if ((ReferenceEquals(_operands[0], null) || ReferenceEquals(_operands[1], null)) && (returnType?.IsValueType ?? false))
                {
                    bool isNullable = Helpers.GetNullableUnderlyingType(returnType) != null;
                    if (_op.BranchOp == BranchInstruction.Ne)
                    {
                        if (isNullable)
                        {
                            valueOperand.Property("HasValue", g.TypeMapper).EmitGet(g);
                        }
                        else
                        {
                            g.EmitI4Helper(1);
                        }
                        return;
                    }
                    else if (_op.BranchOp == BranchInstruction.Eq)
                    {
                        if (isNullable)
                        {
                            valueOperand.Property("HasValue", g.TypeMapper).LogicalNot().EmitGet(g);
                        }
                        else
                        {
                            g.EmitI4Helper(0);
                        }
                        return;
                    }
                }
            }


            PrepareAf(g.TypeMapper);
            _af.EmitArgs(g, _operands);

			IStandardOperation sop = _af.Method as IStandardOperation;
			if (sop != null)
				sop.Emit(g, _op);
			else
				g.IL.Emit(OpCodes.Call, (MethodInfo)_af.Method.Member);
		}

		protected internal override void EmitBranch(CodeGen g, OptionalLabel labelTrue, OptionalLabel labelFalse)
		{
		    this.SetLeakedState(false);
            
            bool argsEmitted = false;

		    var branchSet = BranchSet.Normal;

            // value type handling
		    if (_operands.Length == 2)
		    {
		        var valueOperand = _operands[0] ?? _operands[1];
                Type returnType = valueOperand?.GetReturnType(g.TypeMapper);
                if ((ReferenceEquals(_operands[0], null) || ReferenceEquals(_operands[1], null)) && (returnType?.IsValueType ?? false))
                {
                    bool isNullable = Helpers.GetNullableUnderlyingType(returnType) != null;
                    
                    var op = branchSet.Get(_op.BranchOp, true);
                    if (op == OpCodes.Bne_Un || op == OpCodes.Bne_Un_S)
                    {
                        if (isNullable)
                        {
                            valueOperand.Property("HasValue", g.TypeMapper).EmitBranch(g, labelTrue, labelFalse);
                        }
                        else
                        {
                            // ValueType != null, should return true
                            if (labelTrue != null && labelTrue.IsLabelExist) // otherwise default path
                                g.IL.Emit(OpCodes.Br, labelTrue.Value);
                        }
                        return;
                    }
                    else if (op == OpCodes.Beq || op == OpCodes.Beq_S)
                    {
                        if (isNullable)
                        {
                            valueOperand.Property("HasValue", g.TypeMapper).EmitBranch(g, labelFalse, labelTrue);
                        }
                        else
                        {
                            // ValueType == null, should return false
                            if (labelFalse != null && labelFalse.IsLabelExist) // otherwise default path
                                g.IL.Emit(OpCodes.Br, labelFalse.Value); 
                        }
                        return;
                    }
                }
            }

            PrepareAf(g.TypeMapper);
            IStandardOperation stdOp = _af.Method as IStandardOperation;
			if (_op.BranchOp == 0 || stdOp == null)
			{
				base.EmitBranch(g, labelTrue, labelFalse);
				return;
			}

		    if (!argsEmitted)
		        _af.EmitArgs(g, _operands);

		    bool inverted = false;
		    if (labelTrue == null || !labelTrue.IsLabelExist)
		    {
		        if (labelFalse == null) throw new InvalidOperationException("No labels passed");
                if (!labelFalse.IsLabelExist) throw new InvalidOperationException("No existing labels were passed");
                labelTrue = labelFalse;
		        branchSet = branchSet.GetInverted();
		        inverted = true;
		    }
		    g.IL.Emit(branchSet.Get(_op.BranchOp, stdOp.IsUnsigned), labelTrue.Value);
		    if (!inverted && labelFalse != null && labelFalse.IsLabelExist)
		        g.IL.Emit(OpCodes.Br, labelFalse.Value);
		}

	    public override Type GetReturnType(ITypeMapper typeMapper)
	    {
            PrepareAf(typeMapper);
	        return _af.Method.ReturnType;
	    }
	}
}
