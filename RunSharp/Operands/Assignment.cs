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

namespace TriAxis.RunSharp.Operands
{
	public class Assignment : Operand, IStatement
	{
		Operand lvalue, rvalue;
		bool allowExplicitConversion;

		internal Assignment(Operand lvalue, Operand rvalue, bool allowExplicitConversion)
		{
			this.lvalue = lvalue;
			this.rvalue = rvalue;
			this.allowExplicitConversion = allowExplicitConversion;

			lvalue.AssignmentHint(rvalue);
		}

		public void Emit(CodeGen g)
		{
			lvalue.EmitSet(g, rvalue, allowExplicitConversion);
		}

		internal override void EmitGet(CodeGen g)
		{
			if (lvalue.TrivialAccess)
			{
				lvalue.EmitSet(g, rvalue, allowExplicitConversion);
				lvalue.EmitGet(g);
			}
			else
			{
				Operand tmp = g.Local(lvalue.Type);
				g.Assign(tmp, rvalue, allowExplicitConversion);
				lvalue.EmitSet(g, tmp, false);
				tmp.EmitGet(g);
			}
		}

		public override Type Type
		{
			get
			{
				return lvalue.Type;
			}
		}
	}
}
