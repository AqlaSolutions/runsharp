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
	static class _07_Structs
	{
		// example based on the MSDN Structs Sample (struct1.cs)
		public static void GenStruct1(AssemblyGen ag)
		{
			CodeGen g;

			TypeGen SimpleStruct = ag.Struct("SimpleStruct");
			{
				FieldGen xval = SimpleStruct.Field(typeof(int), "xval");

				PropertyGen X = SimpleStruct.Public.Property(typeof(int), "X");
				{
					X.Getter().GetCode().Return(xval);
					g = X.Setter();
					{
						g.If(g.PropertyValue() < 100);
						{
							g.Assign(xval, g.PropertyValue());
						}
						g.End();
					}
				}

				g = SimpleStruct.Public.Method(typeof(void), "DisplayX");
				{
					g.WriteLine("The stored value is: {0}", xval);
				}
			}

			TypeGen TestClass = ag.Class("TestClass");
			{
				g = TestClass.Public.Static.Method(typeof(void), "Main");
				{
					Operand ss = g.Local(SimpleStruct);
					g.InitObj(ss);
					g.Assign(ss.Property("X"), 5);
					g.Invoke(ss, "DisplayX");
				}
			}
		}

		// example based on the MSDN Structs Sample (struct2.cs)
		public static void GenStruct2(AssemblyGen ag)
		{
			TypeGen TheClass = ag.Class("TheClass");
			{
				TheClass.Public.Field(typeof(int), "x");
			}

			TypeGen TheStruct = ag.Struct("TheStruct");
			{
				TheStruct.Public.Field(typeof(int), "x");
			}

			TypeGen TestClass = ag.Class("TestClass");
			{
				CodeGen g = TestClass.Public.Static.Method(typeof(void), "structtaker").Parameter(TheStruct, "s");
				{
					g.Assign(g.Arg("s").Field("x"), 5);
				}

				g = TestClass.Public.Static.Method(typeof(void), "classtaker").Parameter(TheClass, "c");
				{
					g.Assign(g.Arg("c").Field("x"), 5);
				}

				g = TestClass.Public.Static.Method(typeof(void), "Main");
				{
					Operand a = g.Local(TheStruct);
					g.InitObj(a);
					Operand b = g.Local(Exp.New(TheClass));

					g.Assign(a.Field("x"), 1);
					g.Assign(b.Field("x"), 1);
					g.Invoke(TestClass, "structtaker", a);
					g.Invoke(TestClass, "classtaker", b);
					g.WriteLine("a.x = {0}", a.Field("x"));
					g.WriteLine("b.x = {0}", b.Field("x"));
				}
			}
		}
	}
}
