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
	static class _01_HelloWorld
	{
		// example based on the MSDN Hello World Sample (Hello1.cs)
		public static void GenHello1(AssemblyGen ag)
		{
			TypeGen Hello1 = ag.Public.Class("Hello1");
			{
				CodeGen g = Hello1.Public.Static.Method(typeof(void), "Main");
				{
					g.WriteLine("Hello, World!");
				}
			}
		}

		// example based on the MSDN Hello World Sample (Hello3.cs)
		[TestArguments("arg1", "arg2", "arg3", "arg4")]
		public static void GenHello3(AssemblyGen ag)
		{
			TypeGen Hello3 = ag.Public.Class("Hello3");
			{
				CodeGen g = Hello3.Public.Static.Method(typeof(void), "Main").Parameter(typeof(string[]), "args");
				{
					Operand args = g.Arg("args");
					g.WriteLine("Hello, World!");
					g.WriteLine("You entered the following {0} command line arguments:",
						args.ArrayLength());
					Operand i = g.Local();
					g.For(i.Assign(0), i < args.ArrayLength(), i.Increment());
					{
						g.WriteLine("{0}", args[i]);
					}
					g.End();
				}
			}
		}
	}
}
