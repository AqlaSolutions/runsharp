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
using System.Reflection.Emit;

namespace TriAxis.RunSharp.Operands
{
	class ArrayAccess : Operand
	{
		Operand array;
		Operand[] indexes;

		public ArrayAccess(Operand array, Operand[] indexes)
		{
			if (array.Type.GetArrayRank() != indexes.Length)
				throw new ArgumentException(Properties.Messages.ErrIndexCountMismatch);

			this.array = array;
			this.indexes = indexes;
		}

		void LoadArrayAndIndexes(CodeGen g)
		{
			array.EmitGet(g);

			foreach (Operand op in indexes)
				g.EmitGetHelper(op, Operand.GetType(op) == typeof(int) ? typeof(int) : typeof(long), false);
		}

		internal override void EmitGet(CodeGen g)
		{
			LoadArrayAndIndexes(g);

			if (indexes.Length == 1)
			{
				g.EmitLdelemHelper(Type);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			LoadArrayAndIndexes(g);

			if (indexes.Length == 1)
			{
				g.EmitStelemHelper(Type, value, allowExplicitConversion);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		internal override void EmitAddressOf(CodeGen g)
		{
			LoadArrayAndIndexes(g);

			if (indexes.Length == 1)
			{
				g.IL.Emit(OpCodes.Ldelema, Type);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public override Type Type
		{
			get
			{
				return array.Type.GetElementType();
			}
		}

		internal override bool TrivialAccess
		{
			get
			{
				return true;
			}
		}
	}
}
