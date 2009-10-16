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
	static class _11_OperatorOverloading
	{
		// example based on the MSDN Operator Overloading Sample (complex.cs)
		public static void GenComplex(AssemblyGen ag)
		{
			TypeGen Complex = ag.Public.Struct("Complex");
			{
				FieldGen real = Complex.Public.Field(typeof(int), "real");
				FieldGen imaginary = Complex.Public.Field(typeof(int), "imaginary");

				CodeGen g = Complex.Public.Constructor(typeof(int), typeof(int));
				{
					g.Assign(real, g.Arg(0, "real"));
					g.Assign(imaginary, g.Arg(1, "imaginary"));
				}

				// Declare which operator to overload (+), the types 
				// that can be added (two Complex objects), and the 
				// return type (Complex):
				g = Complex.Operator(Operator.Add, Complex, Complex, Complex);
				{
					Operand c1 = g.Arg(0, "c1"), c2 = g.Arg(1, "c2");
					g.Return(Exp.New(Complex, c1.Field("real") + c2.Field("real"), c1.Field("imaginary") + c2.Field("imaginary")));
				}

				// Override the ToString method to display an complex number in the suitable format:
				g = Complex.Public.Override.Method(typeof(string), "ToString");
				{
					g.Return(Static.Invoke(typeof(string), "Format", "{0} + {1}i", real, imaginary));
				}

				g = Complex.Public.Static.Method(typeof(void), "Main");
				{
					Operand num1 = g.Local(Exp.New(Complex, 2, 3));
					Operand num2 = g.Local(Exp.New(Complex, 3, 4));

					// Add two Complex objects (num1 and num2) through the
					// overloaded plus operator:
					Operand sum = g.Local(num1 + num2);

					// Print the numbers and the sum using the overriden ToString method:
					g.WriteLine("First complex number:  {0}", num1);
					g.WriteLine("Second complex number: {0}", num2);
					g.WriteLine("The sum of the two numbers: {0}", sum);
				}
			}
		}

		// example based on the MSDN Operator Overloading Sample (dbbool.cs)
		public static void GenDbBool(AssemblyGen ag)
		{
			TypeGen DBBool = ag.Public.Struct("DBBool");
			{
				// Private field that stores -1, 0, 1 for dbFalse, dbNull, dbTrue:
				FieldGen value = DBBool.Field(typeof(int), "value");

				// Private constructor. The value parameter must be -1, 0, or 1:
				CodeGen g = DBBool.Constructor(typeof(int));
				{
					g.Assign(value, g.Arg(0, "value"));
				}

				// The three possible DBBool values:
				FieldGen dbNull = DBBool.Public.Static.ReadOnly.Field(DBBool, "dbNull", Exp.New(DBBool, 0));
				FieldGen dbFalse = DBBool.Public.Static.ReadOnly.Field(DBBool, "dbFalse", Exp.New(DBBool, -1));
				FieldGen dbTrue = DBBool.Public.Static.ReadOnly.Field(DBBool, "dbTrue", Exp.New(DBBool, 1));

				// Implicit conversion from bool to DBBool. Maps true to 
				// DBBool.dbTrue and false to DBBool.dbFalse:
				g = DBBool.ImplicitConversionFrom(typeof(bool));
				{
					Operand x = g.Arg(0, "x");
					g.Return(x.Conditional(dbTrue, dbFalse));
				}

				// Explicit conversion from DBBool to bool. Throws an 
				// exception if the given DBBool is dbNull, otherwise returns
				// true or false:
				g = DBBool.ExplicitConversionTo(typeof(bool));
				{
					Operand x = g.Arg(0, "x");
					g.If(x.Field("value") == 0);
					{
						g.Throw(Exp.New(typeof(InvalidOperationException)));
					}
					g.End();

					g.Return(x.Field("value") > 0);
				}

				// Equality operator. Returns dbNull if either operand is dbNull, 
				// otherwise returns dbTrue or dbFalse:
				g = DBBool.Operator(Operator.Equality, DBBool, DBBool, DBBool);
				{
					Operand x = g.Arg(0, "x"), y = g.Arg(1, "y");
					g.If(x.Field("value") == 0 || y.Field("value") == 0);
					{
						g.Return(dbNull);
					}
					g.End();

					g.Return((x.Field("value") == y.Field("value")).Conditional(dbTrue, dbFalse));
				}

				// Inequality operator. Returns dbNull if either operand is
				// dbNull, otherwise returns dbTrue or dbFalse:
				g = DBBool.Operator(Operator.Inequality, DBBool, DBBool, DBBool);
				{
					Operand x = g.Arg(0, "x"), y = g.Arg(1, "y");
					g.If(x.Field("value") == 0 || y.Field("value") == 0);
					{
						g.Return(dbNull);
					}
					g.End();

					g.Return((x.Field("value") != y.Field("value")).Conditional(dbTrue, dbFalse));
				}

				// Logical negation operator. Returns dbTrue if the operand is 
				// dbFalse, dbNull if the operand is dbNull, or dbFalse if the
				// operand is dbTrue:
				g = DBBool.Operator(Operator.LogicalNot, DBBool, DBBool);
				{
					Operand x = g.Arg(0, "x");
					g.Return(Exp.New(DBBool, -x.Field("value")));
				}

				// Logical AND operator. Returns dbFalse if either operand is 
				// dbFalse, dbNull if either operand is dbNull, otherwise dbTrue:
				g = DBBool.Operator(Operator.And, DBBool, DBBool, DBBool);
				{
					Operand x = g.Arg(0, "x"), y = g.Arg(1, "y");
					g.Return(Exp.New(DBBool, (x.Field("value") < y.Field("value")).Conditional(x.Field("value"), y.Field("value"))));
				}

				// Logical OR operator. Returns dbTrue if either operand is 
				// dbTrue, dbNull if either operand is dbNull, otherwise dbFalse:
				g = DBBool.Operator(Operator.Or, DBBool, DBBool, DBBool);
				{
					Operand x = g.Arg(0, "x"), y = g.Arg(1, "y");
					g.Return(Exp.New(DBBool, (x.Field("value") > y.Field("value")).Conditional(x.Field("value"), y.Field("value"))));
				}

				// Definitely true operator. Returns true if the operand is 
				// dbTrue, false otherwise:
				g = DBBool.Operator(Operator.True, typeof(bool), DBBool);
				{
					Operand x = g.Arg(0, "x");
					g.Return(x.Field("value") > 0);
				}

				// Definitely false operator. Returns true if the operand is 
				// dbFalse, false otherwise:
				g = DBBool.Operator(Operator.False, typeof(bool), DBBool);
				{
					Operand x = g.Arg(0, "x");
					g.Return(x.Field("value") < 0);
				}

				// Overload the conversion from DBBool to string:
				g = DBBool.ImplicitConversionTo(typeof(string));
				{
					Operand x = g.Arg(0, "x");

					g.Return((x.Field("value") > 0).Conditional("dbTrue",
						(x.Field("value") < 0).Conditional("dbFalse",
						"dbNull")));
				}

				// Override the Object.Equals(object o) method:
				g = DBBool.Public.Override.Method(typeof(bool), "Equals", typeof(object));
				{
					g.Try();
					{
						g.Return((g.This() == g.Arg(0, "o").Cast(DBBool)).Cast(typeof(bool)));
					}
					g.CatchAll();
					{
						g.Return(false);
					}
					g.End();
				}

				// Override the Object.GetHashCode() method:
				g = DBBool.Public.Override.Method(typeof(int), "GetHashCode");
				{
					g.Return(value);
				}

				// Override the ToString method to convert DBBool to a string:
				g = DBBool.Public.Override.Method(typeof(string), "ToString");
				{
					g.Switch(value);
					{
						g.Case(-1);
						g.Return("DBBool.False");
						g.Case(0);
						g.Return("DBBool.Null");
						g.Case(1);
						g.Return("DBBool.True");
						g.DefaultCase();
						g.Throw(Exp.New(typeof(InvalidOperationException)));
					}
					g.End();
				}
			}

			TypeGen Test = ag.Class("Test");
			{
				CodeGen g = Test.Static.Method(typeof(void), "Main");
				{
					Operand a = g.Local(DBBool), b = g.Local(DBBool);
					g.Assign(a, Static.Field(DBBool, "dbTrue"));
					g.Assign(b, Static.Field(DBBool, "dbNull"));

					g.WriteLine("!{0} = {1}", a, !a);
					g.WriteLine("!{0} = {1}", b, !b);
					g.WriteLine("{0} & {1} = {2}", a, b, a & b);
					g.WriteLine("{0} | {1} = {2}", a, b, a | b);
					// Invoke the true operator to determine the Boolean 
					// value of the DBBool variable:
					g.If(b);
					{
						g.WriteLine("b is definitely true");
					}
					g.Else();
					{
						g.WriteLine("b is not definitely true");
					}
					g.End();
				}
			}
		}
	}
}
