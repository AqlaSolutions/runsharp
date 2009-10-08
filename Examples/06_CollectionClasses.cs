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
	static class _06_CollectionClasses
	{
		// example based on the MSDN Collection Classes Sample (tokens2.cs)
		public static void GenTokens2(AssemblyGen ag)
		{
			TypeGen Tokens = ag.Public.Class("Tokens", typeof(object), typeof(IEnumerable));
			{
				FieldGen elements = Tokens.Private.Field(typeof(string[]), "elements");

				CodeGen g = Tokens.Constructor()
					.Parameter(typeof(string), "source")
					.Parameter(typeof(char[]), "delimiters")
					;
				{
					g.Assign(elements, g.Arg("source").Invoke("Split", g.Arg("delimiters")));
				}

				// Inner class implements IEnumerator interface:

				TypeGen TokenEnumerator = Tokens.Public.Class("TokenEnumerator", typeof(object), typeof(IEnumerator));
				{
					FieldGen position = TokenEnumerator.Field(typeof(int), "position", -1);
					FieldGen t = TokenEnumerator.Field(Tokens, "t");

					g = TokenEnumerator.Public.Constructor().Parameter(Tokens, "tokens");
					{
						g.Assign(t, g.Arg("tokens"));
					}

					g = TokenEnumerator.Public.Method(typeof(bool), "MoveNext");
					{
						g.If(position < t.Field("elements").ArrayLength() - 1);
						{
							g.Increment(position);
							g.Return(true);
						}
						g.Else();
						{
							g.Return(false);
						}
						g.End();
					}

					g = TokenEnumerator.Public.Method(typeof(void), "Reset");
					{
						g.Assign(position, -1);
					}

					// non-IEnumerator version: type-safe
					g = TokenEnumerator.Public.Property(typeof(string), "Current").Getter();
					{
						g.Return(t.Field("elements")[position]);
					}

					// IEnumerator version: returns object
					g = TokenEnumerator.Public.PropertyImplementation(typeof(IEnumerator), typeof(object), "Current").Getter();
					{
						g.Return(t.Field("elements")[position]);
					}
				}

				// IEnumerable Interface Implementation:

				// non-IEnumerable version
				g = Tokens.Public.Method(TokenEnumerator, "GetEnumerator");
				{
					g.Return(Exp.New(TokenEnumerator, g.This()));
				}

				// IEnumerable version
				g = Tokens.Public.MethodImplementation(typeof(IEnumerable), typeof(IEnumerator), "GetEnumerator");
				{
					g.Return(Exp.New(TokenEnumerator, g.This()).Cast(typeof(IEnumerator)));
				}

				// Test Tokens, TokenEnumerator

				g = Tokens.Static.Method(typeof(void), "Main");
				{
					Operand f = g.Local(Exp.New(Tokens, "This is a well-done program.",
						Exp.NewInitializedArray(typeof(char), ' ', '-')));
					Operand item = g.ForEach(typeof(string), f);	// try changing string to int
					{
						g.WriteLine(item);
					}
					g.End();
				}
			}
		}
	}
}
