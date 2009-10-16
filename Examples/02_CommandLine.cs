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
	static class _02_CommandLine
	{
		// example based on the MSDN Command Line Parameters Sample (CmdLine2.cs)
		[TestArguments("arg1", "arg2", "arg3", "arg4")]
		public static void GenCmdLine2(AssemblyGen ag)
		{
			TypeGen CommandLine2 = ag.Public.Class("CommandLine2");
			{
				CodeGen g = CommandLine2.Public.Static.Method(typeof(void), "Main", typeof(string[]));
				{
					Operand args = g.Arg(0);
					g.WriteLine("Number of command line parameters = {0}",
						args.Property("Length"));
					Operand s = g.ForEach(typeof(string), args);
					{
						g.WriteLine(s);
					}
					g.End();
				}
			}
		}
	}
}
