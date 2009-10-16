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
	class ArrayLength : Operand
	{
		Operand array;
		bool asLong;

		static MethodInfo arrGetLen = typeof(Array).GetProperty("Length").GetGetMethod();
		static MethodInfo arrGetLongLen = typeof(Array).GetProperty("LongLength").GetGetMethod();

		public ArrayLength(Operand array, bool asLong)
		{
			if (!array.Type.IsArray)
				throw new InvalidOperationException(Properties.Messages.ErrArrayOnly);

			this.array = array;
			this.asLong = asLong;
		}

		internal override void EmitGet(CodeGen g)
		{
			array.EmitGet(g);

			if (array.Type.GetArrayRank() == 1 && (!asLong || IntPtr.Size == 8))
			{
				g.IL.Emit(OpCodes.Ldlen);
				g.IL.Emit(asLong ? OpCodes.Conv_I8 : OpCodes.Conv_I4);
				return;
			}

			g.IL.Emit(OpCodes.Call, asLong ? arrGetLongLen : arrGetLen);
		}

		public override Type Type
		{
			get
			{
				return asLong ? typeof(long) : typeof(int);
			}
		}
	}
}
