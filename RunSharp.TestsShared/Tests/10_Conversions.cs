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
    public class _10_Conversions : TestBase
    {
        [Test]
        public void TestGenConversion()
        {
            TestingFacade.GetTestsForGenerator(GenConversion, @">>> GEN TriAxis.RunSharp.Tests.10_Conversions.GenConversion
=== RUN TriAxis.RunSharp.Tests.10_Conversions.GenConversion
10
Conversion not yet implemented
10
<<< END TriAxis.RunSharp.Tests.10_Conversions.GenConversion

").RunAll();
        }
        
        // example based on the MSDN User-Defined Conversions Sample (conversion.cs)
        public static void GenConversion(AssemblyGen ag)
		{
            var st = ag.StaticFactory;
            var exp = ag.ExpressionFactory;

			TypeGen RomanNumeral = ag.Struct("RomanNumeral");
			{
				FieldGen value = RomanNumeral.Private.Field(typeof(int), "value");

				CodeGen g = RomanNumeral.Public.Constructor().Parameter(typeof(int), "value");
				{
					g.Assign(value, g.Arg("value"));
				}

				// Declare a conversion from an int to a RomanNumeral. Note the
				// the use of the operator keyword. This is a conversion 
				// operator named RomanNumeral:
				g = RomanNumeral.Public.ImplicitConversionFrom(typeof(int));
				{
					// Note that because RomanNumeral is declared as a struct, 
					// calling new on the struct merely calls the constructor 
					// rather than allocating an object on the heap:
					g.Return(exp.New(RomanNumeral, g.Arg("value")));
				}

				// Declare an explicit conversion from a RomanNumeral to an int:
				g = RomanNumeral.Public.ExplicitConversionTo(typeof(int), "roman");
			    {
			        ITypeMapper typeMapper = ag.TypeMapper;
			        g.Return(g.Arg("roman").Field("value"));
			    }

			    // Declare an implicit conversion from a RomanNumeral to 
				// a string:
				g = RomanNumeral.Public.ImplicitConversionTo(typeof(string));
				{
					g.Return("Conversion not yet implemented");
				}
			}

			TypeGen Test = ag.Class("Test");
			{
				CodeGen g = Test.Public.Static.Method(typeof(void), "Main");
				{
                    var numeral = g.Local(RomanNumeral);

					g.Assign(numeral, 10);

					// Call the explicit conversion from numeral to int. Because it is
					// an explicit conversion, a cast must be used:
					g.WriteLine(numeral.Cast(typeof(int)));

					// Call the implicit conversion to string. Because there is no
					// cast, the implicit conversion to string is the only
					// conversion that is considered:
					g.WriteLine(numeral);

                    // Call the explicit conversion from numeral to int and 
                    // then the explicit conversion from int to short:
                    var s = g.Local(numeral.Cast(typeof(short)));

					g.WriteLine(s);
				}
			}
		}

        [Test]
        public void TestGenStructConversion()
        {
            TestingFacade.GetTestsForGenerator(GenStructConversion, @">>> GEN TriAxis.RunSharp.Tests.10_Conversions.GenStructConversion
=== RUN TriAxis.RunSharp.Tests.10_Conversions.GenStructConversion
10
Conversion not yet implemented
<<< END TriAxis.RunSharp.Tests.10_Conversions.GenStructConversion

").RunAll();
        }

        // example based on the MSDN User-Defined Conversions Sample (structconversion.cs)
        public static void GenStructConversion(AssemblyGen ag)
        {
            var st = ag.StaticFactory;
            var exp = ag.ExpressionFactory;

            TypeGen BinaryNumeral = ag.Struct("BinaryNumeral");
			{
				FieldGen value = BinaryNumeral.Private.Field(typeof(int), "value");

				CodeGen g = BinaryNumeral.Public.Constructor().Parameter(typeof(int), "value");
				{
					g.Assign(value, g.Arg("value"));
				}

				g = BinaryNumeral.Public.ImplicitConversionFrom(typeof(int));
				{
					g.Return(exp.New(BinaryNumeral, g.Arg("value")));
				}

				g = BinaryNumeral.Public.ImplicitConversionTo(typeof(string));
				{
					g.Return("Conversion not yet implemented");
				}

				g = BinaryNumeral.Public.ExplicitConversionTo(typeof(int), "binary");
			    {
			        ITypeMapper typeMapper = ag.TypeMapper;
			        g.Return(g.Arg("binary").Field("value"));
			    }
			}

			TypeGen RomanNumeral = ag.Struct("RomanNumeral");
			{
				FieldGen value = RomanNumeral.Private.Field(typeof(int), "value");

				CodeGen g = RomanNumeral.Public.Constructor().Parameter(typeof(int), "value");
				{
					g.Assign(value, g.Arg("value"));
				}

				g = RomanNumeral.Public.ImplicitConversionFrom(typeof(int));
				{
					g.Return(exp.New(RomanNumeral, g.Arg("value")));
				}

				g = RomanNumeral.Public.ImplicitConversionFrom(BinaryNumeral, "binary");
				{
					g.Return(exp.New(RomanNumeral, g.Arg("binary").Cast(typeof(int))));
				}

				g = RomanNumeral.Public.ExplicitConversionTo(typeof(int), "roman");
			    {
			        ITypeMapper typeMapper = ag.TypeMapper;
			        g.Return(g.Arg("roman").Field("value"));
			    }

			    g = RomanNumeral.Public.ImplicitConversionTo(typeof(string));
				{
					g.Return("Conversion not yet implemented");
				}
			}

			TypeGen Test = ag.Class("Test");
			{
				CodeGen g = Test.Public.Static.Method(typeof(void), "Main");
				{
                    var roman = g.Local(RomanNumeral);
					g.Assign(roman, 10);
                    var binary = g.Local(BinaryNumeral);
					// Perform a conversion from a RomanNumeral to a
					// BinaryNumeral:
					g.Assign(binary, roman.Cast(typeof(int)).Cast(BinaryNumeral));
					// Performs a conversion from a BinaryNumeral to a RomanNumeral.
					// No cast is required:
					g.Assign(roman, binary);
					g.WriteLine(binary.Cast(typeof(int)));
					g.WriteLine(binary);
				}
			}
		}
	}
}
