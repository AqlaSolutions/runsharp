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
	class NewArray : Operand
	{
		Type t;
		Operand[] indexes;

		public NewArray(Type t, Operand[] indexes)
		{
			this.t = t;
			this.indexes = indexes;
		}

		internal override void EmitGet(CodeGen g)
		{
			for (int i = 0; i < indexes.Length; i++)
				g.EmitGetHelper(indexes[i], typeof(int), false);

			if (indexes.Length == 1)
				g.IL.Emit(OpCodes.Newarr, t);
			else
			{
				Type[] argTypes = new Type[indexes.Length];
				for (int i = 0; i < argTypes.Length; i++)
					argTypes[i] = typeof(int);

				ModuleBuilder mb = t.Module as ModuleBuilder;

				if (mb != null)
					g.IL.Emit(OpCodes.Newobj, mb.GetArrayMethod(Type, ".ctor", CallingConventions.HasThis, null, argTypes));
				else
					g.IL.Emit(OpCodes.Newobj, Type.GetConstructor(argTypes));
			}
		}

		public override Type Type
		{
			get
			{
				return indexes.Length == 1 ? t.MakeArrayType() : t.MakeArrayType(indexes.Length);
			}
		}
	}
}
