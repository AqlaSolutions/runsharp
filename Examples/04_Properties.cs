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
	class _04_Properties
	{
		// example based on the MSDN Properties Sample (person.cs)
		public static void GenPerson(AssemblyGen ag)
		{
			TypeGen Person = ag.Class("Person");
			{
				FieldGen myName = Person.Private.Field(typeof(string), "myName", "N/A");
				FieldGen myAge = Person.Private.Field(typeof(int), "myAge", 0);

				// Declare a Name property of type string:
				PropertyGen Name = Person.Public.SimpleProperty(myName, "Name");

				// Declare an Age property of type int:
				PropertyGen Age = Person.Public.SimpleProperty(myAge, "Age");

				CodeGen g = Person.Public.Override.Method(typeof(string), "ToString");
				{
					g.Return("Name = " + Name + ", Age = " + Age);
				}

				g = Person.Public.Static.Method(typeof(void), "Main");
				{
					g.WriteLine("Simple Properties");

					// Create a new Person object:
					Operand person = g.Local(Exp.New(Person));

					// Print out the name and the age associated with the person:
					g.WriteLine("Person details - {0}", person);

					// Set some values on the person object:
					g.Assign(person.Property("Name"), "Joe");
					g.Assign(person.Property("Age"), 99);
					g.WriteLine("Person details - {0}", person);

					// Increment the Age property:
					g.AssignAdd(person.Property("Age"), 1);
					g.WriteLine("Person details - {0}", person);
				}
			}
		}

		// example based on the MSDN Properties Sample (abstractshape.cs, shapes.cs, shapetest.cs)
		public static void GenShapeTest(AssemblyGen ag)
		{
			// abstractshape.cs
			TypeGen Shape = ag.Public.Abstract.Class("Shape");
			{
				FieldGen myId = Shape.Private.Field(typeof(string), "myId");

				PropertyGen Id = Shape.Public.SimpleProperty(myId, "Id");

				CodeGen g = Shape.Public.Constructor(typeof(string));
				{
					g.Assign(Id, g.Arg(0, "s"));	// calling the set accessor of the Id property
				}

				// Area is a read-only property - only a get accessor is needed:
				PropertyGen Area = Shape.Public.Abstract.Property(typeof(double), "Area");
				Area.Getter();

				g = Shape.Public.Override.Method(typeof(string), "ToString");
				{
					g.Return(Id + " Area = " + Static.Invoke(typeof(string), "Format", "{0:F2}", Area));
				}
			}

			// shapes.cs
			TypeGen Square = ag.Public.Class("Square", Shape);
			{
				FieldGen mySide = Square.Private.Field(typeof(int), "mySide");

				CodeGen g = Square.Public.Constructor(typeof(int), typeof(string));
				{
					g.InvokeBase(g.Arg(1, "id"));
					g.Assign(mySide, g.Arg(0, "side"));
				}

				PropertyGen Area = Square.Public.Override.Property(typeof(double), "Area");
				g = Area.Getter();
				{
					// Given the side, return the area of a square:
					g.Return(mySide * mySide);
				}
			}

			TypeGen Circle = ag.Public.Class("Circle", Shape);
			{
				FieldGen myRadius = Circle.Private.Field(typeof(int), "myRadius");

				CodeGen g = Circle.Public.Constructor(typeof(int), typeof(string));
				{
					g.InvokeBase(g.Arg(1, "id"));
					g.Assign(myRadius, g.Arg(0, "radius"));
				}

				PropertyGen Area = Circle.Public.Override.Property(typeof(double), "Area");
				g = Area.Getter();
				{
					// Given the radius, return the area of a circle:
					g.Return(myRadius * myRadius * Math.PI);
				}
			}

			TypeGen Rectangle = ag.Public.Class("Rectangle", Shape);
			{
				FieldGen myWidth = Rectangle.Private.Field(typeof(int), "myWidth");
				FieldGen myHeight = Rectangle.Private.Field(typeof(int), "myHeight");

				CodeGen g = Rectangle.Public.Constructor(typeof(int), typeof(int), typeof(string));
				{
					g.InvokeBase(g.Arg(2, "id"));
					g.Assign(myWidth, g.Arg(0, "width"));
					g.Assign(myHeight, g.Arg(1, "height"));
				}

				PropertyGen Area = Rectangle.Public.Override.Property(typeof(double), "Area");
				g = Area.Getter();
				{
					// Given the width and height, return the area of a rectangle:
					g.Return(myWidth * myHeight);
				}
			}

			// shapetest.cs
			TypeGen TestClass = ag.Public.Class("TestClass");
			{
				CodeGen g = TestClass.Public.Static.Method(typeof(void), "Main");
				{
					Operand shapes = g.Local(Exp.NewInitializedArray(Shape,
						Exp.New(Square, 5, "Square #1"),
						Exp.New(Circle, 3, "Circle #1"),
						Exp.New(Rectangle, 4, 5, "Rectangle #1")));

					g.WriteLine("Shapes Collection");
					Operand s = g.ForEach(Shape, shapes);
					{
						g.WriteLine(s);
					}
					g.End();
				}
			}
		}
	}
}
