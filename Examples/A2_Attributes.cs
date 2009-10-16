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
using System.ComponentModel;

namespace TriAxis.RunSharp.Examples
{
	static class A2_Attributes
	{
		public static void GenTypeAttributeTest(AssemblyGen ag)
		{
			TypeGen MyAttribute = ag.Public.Class("MyAttribute", typeof(Attribute))
				.BeginAttribute(typeof(AttributeUsageAttribute), AttributeTargets.Class).Set("AllowMultiple", true).End()
				;
			FieldGen testField = MyAttribute.Field(typeof(object), "testField")
				.Attribute(typeof(DescriptionAttribute), "Test field")
				;
			PropertyGen testProperty = MyAttribute.Public.SimpleProperty(testField, "TestProperty")
				.Attribute(typeof(ObsoleteAttribute), "Do not use this")
				;

			testProperty.Getter().Attribute(typeof(DescriptionAttribute), "Getter method");
			testProperty.Getter().ReturnParameter.Attribute(typeof(DescriptionAttribute), "Getter return value");
			testProperty.Setter().Attribute(typeof(DescriptionAttribute), "Setter method");

			TypeGen tg = ag.Class("Test")
				.BeginAttribute(MyAttribute).Set("TestProperty", 3).End()
				.Attribute(typeof(System.ComponentModel.DescriptionAttribute), "Test class")
				;

			tg.Static.Method(typeof(void), "Main")
				.BeginAttribute(MyAttribute).Set("TestProperty", 3).End()
				;

			TypeGen SimpleDelegate = ag.Delegate(typeof(void), "SimpleDelegate")
				.Attribute(typeof(DescriptionAttribute), "Test delegate")
				;

			EventGen TestEvent = tg.Static.Event(SimpleDelegate, "TestEvent")
				.Attribute(typeof(DescriptionAttribute), "Test event")
				;

			TestEvent.AddMethod().Attribute(typeof(DescriptionAttribute), "Event add method");
			TestEvent.RemoveMethod().Attribute(typeof(DescriptionAttribute), "Event remove method");
		}
	}
}
