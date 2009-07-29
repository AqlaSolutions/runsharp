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
using System.Collections;
using System.Text;

namespace TriAxis.RunSharp.Examples
{
	class _13_Events
	{
		// example based on the MSDN Events Sample (events1.cs)
		public static void GenEvents1(AssemblyGen ag)
		{
			TypeGen ChangedEventHandler, ListWithChangedEvent;

			using (ag.Namespace("MyCollections"))
			{
				// A delegate type for hooking up change notifications.
				ChangedEventHandler = ag.Delegate(typeof(void), "ChangedEventHandler", typeof(object), typeof(EventArgs));

				// A class that works just like ArrayList, but sends event
				// notifications whenever the list changes.
				ListWithChangedEvent = ag.Public.Class("ListWithChangedEvent", typeof(ArrayList));
				{
					// An event that clients can use to be notified whenever the
					// elements of the list change.
					EventGen Changed = ListWithChangedEvent.Public.Event(ChangedEventHandler, "Changed");

					// Invoke the Changed event; called whenever list changes
					CodeGen g = ListWithChangedEvent.Protected.Virtual.Method(typeof(void), "OnChanged", typeof(EventArgs));
					{
						g.If(Changed != null);
						{
							g.InvokeDelegate(Changed, g.This(), g.Arg(0, "e"));
						}
						g.End();
					}

					// Override some of the methods that can change the list;
					// invoke event after each
					g = ListWithChangedEvent.Public.Override.Method(typeof(int), "Add", typeof(object));
					{
						Operand i = g.Local(g.Base().Invoke("Add", g.Arg(0, "value")));
						g.Invoke(g.This(), "OnChanged", Static.Field(typeof(EventArgs), "Empty"));
						g.Return(i);
					}

					g = ListWithChangedEvent.Public.Override.Method(typeof(void), "Clear");
					{
						g.Invoke(g.Base(), "Clear");
						g.Invoke(g.This(), "OnChanged", Static.Field(typeof(EventArgs), "Empty"));
					}

					g = ListWithChangedEvent.Public.Override.Indexer(typeof(object), typeof(int)).Setter();
					{
						g.Assign(g.Base()[g.Arg(0, "index")], g.PropertyValue());
						g.Invoke(g.This(), "OnChanged", Static.Field(typeof(EventArgs), "Empty"));
					}
				}
			}

			using (ag.Namespace("TestEvents"))
			{
				TypeGen EventListener = ag.Class("EventListener");
				{
					FieldGen List = EventListener.Field(ListWithChangedEvent, "List");

					// This will be called whenever the list changes.
					CodeGen g = EventListener.Private.Method(typeof(void), "ListChanged", typeof(object), typeof(EventArgs));
					{
						g.WriteLine("This is called when the event fires.");
					}

					g = EventListener.Public.Constructor(ListWithChangedEvent);
					{
						g.Assign(List, g.Arg(0, "list"));
						// Add "ListChanged" to the Changed event on "List".
						g.SubscribeEvent(List, "Changed", Exp.NewDelegate(ChangedEventHandler, g.This(), "ListChanged"));
					}

					g = EventListener.Public.Method(typeof(void), "Detach");
					{
						// Detach the event and delete the list
						g.UnsubscribeEvent(List, "Changed", Exp.NewDelegate(ChangedEventHandler, g.This(), "ListChanged"));
						g.Assign(List, null);
					}
				}

				TypeGen Test = ag.Class("Test");
				{
					// Test the ListWithChangedEvent class.
					CodeGen g = Test.Public.Static.Method(typeof(void), "Main");
					{
						// Create a new list.
						Operand list = g.Local(Exp.New(ListWithChangedEvent));

						// Create a class that listens to the list's change event.
						Operand listener = g.Local(Exp.New(EventListener, list));

						// Add and remove items from the list.
						g.Invoke(list, "Add", "item 1");
						g.Invoke(list, "Clear");
						g.Invoke(listener, "Detach");
					}
				}
			}
		}
	}
}
