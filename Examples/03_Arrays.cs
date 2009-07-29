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

namespace TriAxis.RunSharp.Examples
{
	static class _03_Arrays
	{
		// example based on the MSDN Arrays Sample (arrays.cs)
		public static void GenArrays(AssemblyGen ag)
		{
			TypeGen DeclareArraysSample = ag.Class("DecalreArraysSample");
			{
				CodeGen g = DeclareArraysSample.Public.Static.Method(typeof(void), "Main");
				{
					// Single-dimensional array
					Operand numbers = g.Local(Exp.NewArray(typeof(int), 5));
					
					// Multidimensional array
					Operand names = g.Local(Exp.NewArray(typeof(string), 5, 4));
					
					// Array-of-arrays (jagged array)
					Operand scores = g.Local(Exp.NewArray(typeof(byte[]), 5));

					// Create the jagged array
					Operand i = g.Local();
					g.For(i.Assign(0), i < scores.ArrayLength(), i.Increment());
					{
						g.Assign(scores[i], Exp.NewArray(typeof(byte), i + 3));
					}
					g.End();

					// Print length of each row
					g.For(i.Assign(0), i < scores.ArrayLength(), i.Increment());
					{
						g.WriteLine("Length of row {0} is {1}", i, scores[i].ArrayLength());
					}
					g.End();
				}
			}
		}
	}
}
