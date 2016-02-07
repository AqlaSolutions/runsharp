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
	    Operand _afInterceptor;
	    Type _returnType;

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

        readonly List<LocalCache>_caches=new List<LocalCache>();

	    void PrepareAf(ITypeMapper typeMapper)
	    {
	        if (_af != null || !ReferenceEquals(_afInterceptor, null)) return;
	        Operand[] operandsValueOrDefault = new Operand[2];
	        Operand[] operandsHasValue = new Operand[2];
	        Operand[] operandsValue = new Operand[2];
	        int nullables = 0;
	        int nulls = 0;
	        int lastNullableIndex = -1;
	        for (int i = 0; i < _operands.Length; i++)
	        {
	            var op = _operands[i];
	            if (ReferenceEquals(op, null))
	            {
	                nulls++;
	                continue;
	            }
	            
	            Type vt = op.GetReturnType(typeMapper);
	            if (vt.IsValueType && (Helpers.GetNullableUnderlyingType(vt) != null))
	            {
                    if (!op.TrivialAccess)
                    {
                        var cache = new LocalCache(op);
                        _caches.Add(cache);
                        op = cache;
                    }
                    operandsValueOrDefault[i] = op.Invoke("GetValueOrDefault", typeMapper).SetNotLeaked();
	                operandsValue[i] = op.Property("Value", typeMapper).SetNotLeaked();
                    operandsHasValue[i] = op.Property("HasValue", typeMapper).SetNotLeaked();
	                nullables++;
	                lastNullableIndex = i;
	            }
	            else
	            {
	                operandsValueOrDefault[i] = op;
	                operandsValue[i] = op;
	            }
	        }

	        if (nullables > 0 && nulls == 0)
	        {
	            if (_operands.Length == 2)
	            {
	                var nonNullableOperation = new OverloadableOperation(_op, operandsValueOrDefault[0], operandsValueOrDefault[1]);

	                Type returnType = nonNullableOperation.GetReturnType(typeMapper);

	                // when no value
	                // for comparsion we return false, 
	                // for +-, etc we return nullable null 
	                Type nullableReturnType = typeMapper.MapType(typeof(Nullable<>)).MakeGenericType(returnType);


	                // bool? || bool? - not allowed
	                // bool? || bool - not allowed
	                // but bool? | bool  - allowed but not logical!
	                // the difference between logical == != vs normal: for we should always return true or false
	                // expression "(int?) == 5" is not nullable but "int? + 5" is!
	                if (_op.IsLogical)
	                {
	                    // true or false required
	                    _returnType = typeMapper.MapType(typeof(bool));

	                    if (_op.BranchOp == BranchInstruction.Ne)
	                    {
	                        if (nullables == 1)
	                        {
	                            Operand notHasValue = !operandsHasValue[lastNullableIndex];
	                            _afInterceptor = new Conditional(nonNullableOperation, true, notHasValue);
	                        }
	                        else
	                        {
	                            //  ((nullable.GetValueOrDefault() != nullable2.GetValueOrDefault()) ? true : (nullable.HasValue != nullable2.HasValue))
	                            Operand hasValueNotEqual = operandsHasValue[0] != operandsHasValue[1];
	                            _afInterceptor = new Conditional(nonNullableOperation, true, hasValueNotEqual);
	                        }
	                    }
	                    else
	                    {
	                        //  ((nullable.GetValueOrDefault() == nullable2.GetValueOrDefault()) ? (nullable.HasValue == nullable2.HasValue) : false)
	                        Operand hasValueEqualCheck =
	                            nullables == 2
	                                ? operandsHasValue[0] == operandsHasValue[1]
	                                : operandsHasValue[lastNullableIndex];

	                        _afInterceptor = new Conditional(nonNullableOperation, hasValueEqualCheck, false);
	                    }
	                }
	                else
	                {
	                    // nullable return:
	                    // long? = int? + long?
	                    // long? = (a.HasValue && b.HasValue) ? new long?(a.Value + b.Value) : (long?)null
	                    _returnType = nullableReturnType;
	                    nonNullableOperation = new OverloadableOperation(_op, operandsValue[0], operandsValue[1]);
	                    var ctorArgs = new Operand[] { nonNullableOperation };
	                    var ctor = typeMapper.TypeInfo.FindConstructor(nullableReturnType, ctorArgs);
	                    Operand bothWithValueCondition =
	                        nullables == 2
	                            ? operandsHasValue[0] && operandsHasValue[1]
	                            : operandsHasValue[lastNullableIndex];

	                    _afInterceptor = new Conditional(bothWithValueCondition, new NewObject(ctor, ctorArgs), new DefaultValue(nullableReturnType));
	                }
	                return;
	            }
	            else if (_operands.Length == 1)
	            {
	                // convert increment/decrement to binary operators
	                if (_op.OpCode == OpCodes.Add)
	                {
	                    _afInterceptor = _operands[0] + 1;
	                    _returnType = _afInterceptor.GetReturnType(typeMapper);
	                    return;
	                }
	                if (_op.OpCode == OpCodes.Sub)
	                {
	                    _afInterceptor = _operands[0] - 1;
	                    _returnType = _afInterceptor.GetReturnType(typeMapper);
	                    return;
	                }
	            }
	        }

	        PrepareAfNormally(typeMapper, _operands);

	        if (_af == null)
	            throw new AmbiguousMatchException(Properties.Messages.ErrAmbiguousBinding);

	        _returnType = _af.Method.ReturnType;
	    }

	    void PrepareAfNormally(ITypeMapper typeMapper, Operand[] afOperands)
	    {

	        List<ApplicableFunction> candidates = null;

	        foreach (Operand operand in afOperands)
	        {
	            if ((object)operand != null && !operand.GetReturnType(typeMapper).IsPrimitive)
	            {
	                // try overloads
	                candidates = _op.FindUserCandidates(typeMapper, afOperands);
	                break;
	            }
	        }

	        if (candidates == null)
	            candidates = OverloadResolver.FindApplicable(_op.GetStandardCandidates(typeMapper, afOperands), typeMapper, afOperands);

	        if (candidates == null)
	            throw new InvalidOperationException(
	                string.Format(
	                    null,
	                    Properties.Messages.ErrInvalidOperation,
	                    _op.MethodName,
	                    string.Join(", ", ConvertAll<Operand, string>(afOperands, op => op.GetReturnType(typeMapper).FullName))));

	        _af = OverloadResolver.FindBest(candidates, typeMapper);
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

            foreach (LocalCache lc in _caches)
            {
                lc.Clear();
            }

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
                    else if (_op.IsLogical) // 5? > null, 5 <= null, ...
                    {
                        g.EmitI4Helper(0);
                        return;
                    }
                }
            }


            PrepareAf(g.TypeMapper);

            if (!ReferenceEquals(_afInterceptor, null))
            {
                _afInterceptor.EmitGet(g);
                return;
            }

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

            foreach (LocalCache lc in _caches)
            {
                lc.Clear();
            }

            bool argsEmitted = false;

		    var branchSet = BranchSet.Normal;

            // value type handling
		    if (_operands.Length == 2)
		    {
		        var valueOperand = _operands[0] ?? _operands[1];
                Type returnType = valueOperand?.GetReturnType(g.TypeMapper);
                if (returnType?.IsValueType ?? false)
                {
                    if (ReferenceEquals(_operands[0], null) || ReferenceEquals(_operands[1], null))
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
                        else if (_op.IsLogical) // 5? > null, 5 <= null, ...
                        {
                            if (labelFalse != null && labelFalse.IsLabelExist) // otherwise default path
                                g.IL.Emit(OpCodes.Br, labelFalse.Value);
                        }
                    }
                }
            }

            PrepareAf(g.TypeMapper);

		    if (!ReferenceEquals(_afInterceptor, null))
		    {
		        _afInterceptor.EmitBranch(g, labelTrue, labelFalse);
		        return;
		    }

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
	        return _returnType;
	    }
	}
}
