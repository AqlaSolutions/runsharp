/*
 * Copyright (c) 2015, Stefan Simek, Vladyslav Taranov
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
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class _01_HelloWorld
    {
        [Test]
        public void TestGenHello1()
        {
            TestingFacade.GetTestsForGenerator(GenHello1,
                @">>> GEN TriAxis.RunSharp.Tests.01_HelloWorld.GenHello1
=== RUN TriAxis.RunSharp.Tests.01_HelloWorld.GenHello1
Hello, World!
<<< END TriAxis.RunSharp.Tests.01_HelloWorld.GenHello1

").RunAll();
        }

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

        [Test]
        public void TestGenHello3()
        {
            TestingFacade.GetTestsForGenerator(GenHello3, @">>> GEN TriAxis.RunSharp.Tests.01_HelloWorld.GenHello3
=== RUN TriAxis.RunSharp.Tests.01_HelloWorld.GenHello3
Hello, World!
You entered the following 4 command line arguments:
arg1
arg2
arg3
arg4
<<< END TriAxis.RunSharp.Tests.01_HelloWorld.GenHello3

").RunAll();
        }

        // example based on the MSDN Hello World Sample (Hello3.cs)
        [TestArguments("arg1", "arg2", "arg3", "arg4")]
        public static void GenHello3(AssemblyGen ag)
        {
            ITypeMapper m = ag.TypeMapper;
            TypeGen Hello3 = ag.Public.Class("Hello3");
            {
                CodeGen g = Hello3.Public.Static.Method(typeof(void), "Main").Parameter(typeof(string[]), "args");
                {
                    var args = g.Arg("args");
                    g.WriteLine("Hello, World!");
                    g.WriteLine(
                        "You entered the following {0} command line arguments:",
                        args.ArrayLength());

                    var i = g.Local();
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