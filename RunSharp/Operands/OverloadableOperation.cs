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
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace TriAxis.RunSharp.Operands
{
	class OverloadableOperation : Operand
	{
		Operator op;
		Operand[] operands;
		ApplicableFunction af;

		public OverloadableOperation(Operator op, params Operand[] operands)
		{
			this.op = op;
			this.operands = operands;

			List<ApplicableFunction> candidates = null;

			foreach (Operand operand in operands)
			{
				if ((object)operand != null && !operand.Type.IsPrimitive)
				{
					// try overloads
					candidates = op.FindUserCandidates(operands);
					break;
				}
			}

			if (candidates == null)
				candidates = OverloadResolver.FindApplicable(op.GetStandardCandidates(operands), operands);

			if (candidates == null)
				throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrInvalidOperation, op.methodName,
					string.Join(", ", Array.ConvertAll<Operand, string>(operands, Operand.GetTypeName))));

			af = OverloadResolver.FindBest(candidates);

			if (af == null)
				throw new AmbiguousMatchException(Properties.Messages.ErrAmbiguousBinding);
		}

		internal void SetOperand(Operand newOp)
		{
			operands[0] = newOp;
		}

		internal override void EmitGet(CodeGen g)
		{
			af.EmitArgs(g, operands);

			IStandardOperation sop = af.Method as IStandardOperation;
			if (sop != null)
				sop.Emit(g, op);
			else
				g.IL.Emit(OpCodes.Call, (MethodInfo)af.Method.Member);
		}

		internal override void EmitBranch(CodeGen g, BranchSet branchSet, Label label)
		{
			IStandardOperation stdOp = af.Method as IStandardOperation;
			if (op.branchOp == 0 || stdOp == null)
			{
				base.EmitBranch(g, branchSet, label);
				return;
			}

			af.EmitArgs(g, operands);
			g.IL.Emit(branchSet.Get(op.branchOp, stdOp.IsUnsigned), label);
		}

		public override Type Type
		{
			get
			{
				return af.Method.ReturnType;
			}
		}
	}
}
