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
	static class _14_ExplicitImplementation
	{
		// example based on the MSDN Explicit Interface Implementation Sample (explicit.cs)
		public static void GenExplicit2(AssemblyGen ag)
		{
			// Declare the English units interface:
			TypeGen IEnglishDimensions = ag.Interface("IEnglishDimensions");
			{
				IEnglishDimensions.Method(typeof(float), "Length");
				IEnglishDimensions.Method(typeof(float), "Width");
			}

			// Declare the metric units interface:
			TypeGen IMetricDimensions = ag.Interface("IMetricDimensions");
			{
				IMetricDimensions.Method(typeof(float), "Length");
				IMetricDimensions.Method(typeof(float), "Width");
			}

			// Declare the "Box" class that implements the two interfaces:
			// IEnglishDimensions and IMetricDimensions:
			TypeGen Box = ag.Class("Box", typeof(object), IEnglishDimensions, IMetricDimensions);
			{
				FieldGen lengthInches = Box.Field(typeof(float), "lengthInches");
				FieldGen widthInches = Box.Field(typeof(float), "widthInches");

				CodeGen g = Box.Public.Constructor(typeof(float), typeof(float));
				{
					g.Assign(lengthInches, g.Arg(0, "length"));
					g.Assign(widthInches, g.Arg(0, "width"));
				}
				// Explicitly implement the members of IEnglishDimensions:
				g = Box.MethodImplementation(IEnglishDimensions, typeof(float), "Length");
				{
					g.Return(lengthInches);
				}
				g = Box.MethodImplementation(IEnglishDimensions, typeof(float), "Width");
				{
					g.Return(widthInches);
				}
				// Explicitly implement the members of IMetricDimensions:
				g = Box.MethodImplementation(IMetricDimensions, typeof(float), "Length");
				{
					g.Return(lengthInches * 2.54f);
				}
				g = Box.MethodImplementation(IMetricDimensions, typeof(float), "Width");
				{
					g.Return(widthInches * 2.54f);
				}
				g = Box.Public.Static.Method(typeof(void), "Main");
				{
					// Declare a class instance "myBox":
					Operand myBox = g.Local(Exp.New(Box, 30.0f, 20.0f));
					// Declare an instance of the English units interface:
					Operand eDimensions = g.Local(myBox.Cast(IEnglishDimensions));
					// Declare an instance of the metric units interface:
					Operand mDimensions = g.Local(myBox.Cast(IMetricDimensions));
					// Print dimensions in English units:
					g.WriteLine("Length(in): {0}", eDimensions.Invoke("Length"));
					g.WriteLine("Width (in): {0}", eDimensions.Invoke("Width"));
					// Print dimensions in metric units:
					g.WriteLine("Length(cm): {0}", mDimensions.Invoke("Length"));
					g.WriteLine("Width (cm): {0}", mDimensions.Invoke("Width"));
				}
			}
		}
	}
}
