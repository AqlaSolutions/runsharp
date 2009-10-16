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
	class _05_Versioning
	{
		// example based on the MSDN Versioning Sample (versioning.cs)
		public static void GenVersioning(AssemblyGen ag)
		{
			TypeGen MyBase = ag.Public.Class("MyBase");
			{
				MyBase.Public.Virtual.Method(typeof(string), "Meth1").Code
					.Return("MyBase-Meth1");

				MyBase.Public.Virtual.Method(typeof(string), "Meth2").Code
					.Return("MyBase-Meth2");

				MyBase.Public.Virtual.Method(typeof(string), "Meth3").Code
					.Return("MyBase-Meth3");
			}

			TypeGen MyDerived = ag.Class("MyDerived", MyBase);
			{
				// Overrides the virtual method Meth1 using the override keyword:
				MyDerived.Public.Override.Method(typeof(string), "Meth1").Code
					.Return("MyDerived-Meth1");

				// Explicitly hide the virtual method Meth2 using the new
				// keyword:
				// remark: new is not supported/required in RunSharp
				MyDerived.Public.Method(typeof(string), "Meth2").Code
					 .Return("MyDerived-Meth2");

				// Because no keyword is specified in the following declaration
				// a warning will be issued to alert the programmer that 
				// the method hides the inherited member MyBase.Meth3():
				// remark: this warning is not supported in RunSharp
				MyDerived.Public.Method(typeof(string), "Meth3").Code
					 .Return("MyDerived-Meth3");

				CodeGen g = MyDerived.Public.Static.Method(typeof(void), "Main");
				{
					Operand mD = g.Local(Exp.New(MyDerived));
					Operand mB = g.Local(mD.Cast(MyBase));

					g.WriteLine(mB.Invoke("Meth1"));
					g.WriteLine(mB.Invoke("Meth2"));
					g.WriteLine(mB.Invoke("Meth3"));
				}
			}
		}
	}
}
