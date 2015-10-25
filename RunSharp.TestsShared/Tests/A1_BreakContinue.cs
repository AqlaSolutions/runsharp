/*
Copyright(c) 2009, Stefan Simek
Copyright(c) 2015, Vladyslav Taranov

MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
	[TestFixture]
    public class A1_BreakContinue
    {
        [Test]
        public void TestGenBreakContinue()
        {
            TestingFacade.GetTestsForGenerator(GenBreakContinue, @">>> GEN TriAxis.RunSharp.Tests.A1_BreakContinue.GenBreakContinue
=== RUN TriAxis.RunSharp.Tests.A1_BreakContinue.GenBreakContinue
Break test:
1
2
3
4
Continue test:
1
2
3
8
9
10
<<< END TriAxis.RunSharp.Tests.A1_BreakContinue.GenBreakContinue

").RunAll();
        }

        public static void GenBreakContinue(AssemblyGen ag)
		{
			CodeGen g = ag.Class("Test").Public.Static.Method(typeof(void), "Main");

			g.WriteLine("Break test:");
            var i = g.Local();
			g.For(i.Assign(1), i <= 100, i.Increment());
			{
				g.If(i == 5);
				{
					g.Break();
				}
				g.End();
				g.WriteLine(i);
			}
			g.End();

			g.WriteLine("Continue test:");
			g.For(i.Assign(1), i <= 10, i.Increment());
			{
				g.If(i > 3 && i < 8);
				{
					g.Continue();
				}
				g.End();
				g.WriteLine(i);
			}
			g.End();
		}
	}
}
