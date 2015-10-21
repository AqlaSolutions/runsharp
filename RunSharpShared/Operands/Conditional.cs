/*
 * Copyright (c) 2015, Stefan Simek, Vladyslav Taranov
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
	    readonly Operand _cond;
	    readonly Operand _ifTrue;
	    readonly Operand _ifFalse;

	    public Conditional(Operand cond, Operand ifTrue, Operand ifFalse)
		{
			// TODO: proper checking as in specification
			if (Operand.GetType(ifTrue) != Operand.GetType(ifFalse))
				throw new ArgumentException(Properties.Messages.ErrInvalidConditionalVariants);

			this._cond = cond;
			this._ifTrue = ifTrue;
			this._ifFalse = ifFalse;
		}

		internal override void EmitGet(CodeGen g)
		{
			Label lbTrue = g.IL.DefineLabel();
			Label lbFalse = g.IL.DefineLabel();

			_cond.EmitBranch(g, BranchSet.Normal, lbTrue);
			_ifFalse.EmitGet(g);
			g.IL.Emit(OpCodes.Br, lbFalse);
			g.IL.MarkLabel(lbTrue);
			_ifTrue.EmitGet(g);
			g.IL.MarkLabel(lbFalse);
		}

		public override Type Type => Operand.GetType(_ifTrue);
	}
}
