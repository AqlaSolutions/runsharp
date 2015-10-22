/*
 * Copyright (c) 2010, Stefan Simek
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

// Google Code Issue 14:	static ctor generated badly
// Reported by royosherove, May 12, 2010

using System;
using System.Collections.Generic;
using System.Text;

namespace TriAxis.RunSharp.Examples.Bugs
{
	static class X14_StaticConstructor
	{
		public static void GenStaticCtor(AssemblyGen ag)
		{
			TypeGen Test = ag.Class("Test");
			{
				FieldGen a = Test.Static.Field(typeof(int), "a");

				CodeGen g = Test.StaticConstructor();
				{
					g.WriteLine("Hello from .cctor!");
					g.Assign(a, 3);
				}

				g = Test.Static.Method(typeof(void), "Main");
				{
					g.Invoke(typeof(System.Diagnostics.Debug), "Assert", a == 3);
					g.WriteLine(".cctor works now...");
				}
			}
		}
	}
}
