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
using TryAxis.RunSharp;

namespace TriAxis.RunSharp.Tests
{
	[TestFixture]
    public class _07_Structs : TestBase
    {
        [Test]
        public void TestGenStruct1()
        {
            TestingFacade.GetTestsForGenerator(GenStruct1, @">>> GEN TriAxis.RunSharp.Tests.07_Structs.GenStruct1
=== RUN TriAxis.RunSharp.Tests.07_Structs.GenStruct1
The stored value is: 5
<<< END TriAxis.RunSharp.Tests.07_Structs.GenStruct1

").RunAll();
        }

        // example based on the MSDN Structs Sample (struct1.cs)
        public static void GenStruct1(AssemblyGen ag)
		{
            var st = ag.StaticFactory;
            var exp = ag.ExpressionFactory;

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
                    var ss = g.Local(SimpleStruct);
					g.InitObj(ss);
				    ITypeMapper typeMapper = ag.TypeMapper;
				    g.Assign(ss.Property("X"), 5);
					g.Invoke(ss, "DisplayX");
				}
			}
		}

		// example based on the MSDN Structs Sample (struct2.cs)
		public static void GenStruct2(AssemblyGen ag)
        {
            var st = ag.StaticFactory;
            var exp = ag.ExpressionFactory;

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
			        ITypeMapper typeMapper = ag.TypeMapper;
			        g.Assign(g.Arg("s").Field("x"), 5);
			    }

			    g = TestClass.Public.Static.Method(typeof(void), "classtaker").Parameter(TheClass, "c");
			    {
			        ITypeMapper typeMapper = ag.TypeMapper;
			        g.Assign(g.Arg("c").Field("x"), 5);
			    }

			    g = TestClass.Public.Static.Method(typeof(void), "Main");
				{
                    var a = g.Local(TheStruct);
					g.InitObj(a);
				    ITypeMapper typeMapper4 = ag.TypeMapper;
				    var b = g.Local(exp.New(TheClass));

				    ITypeMapper typeMapper = ag.TypeMapper;
				    g.Assign(a.Field("x"), 1);
				    ITypeMapper typeMapper2 = ag.TypeMapper;
				    g.Assign(b.Field("x"), 1);
					g.Invoke(TestClass, "structtaker", a);
					g.Invoke(TestClass, "classtaker", b);
				    ITypeMapper typeMapper3 = ag.TypeMapper;
				    g.WriteLine("a.x = {0}", a.Field("x"));
				    ITypeMapper typeMapper1 = ag.TypeMapper;
				    g.WriteLine("b.x = {0}", b.Field("x"));
				}
			}
		}
	}
}
