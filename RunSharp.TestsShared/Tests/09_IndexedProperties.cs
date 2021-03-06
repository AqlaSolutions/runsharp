/*
Copyright(c) 2009, Stefan Simek
Copyright(c) 2016, Vladyslav Taranov

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
using TriAxis.RunSharp;

namespace TriAxis.RunSharp.Tests
{
	[TestFixture]
    public class _09_IndexedProperties : TestBase
    {
        [Test]
        public void TestGenIndexedProperty()
        {
            TestingFacade.GetTestsForGenerator(GenIndexedProperty, @">>> GEN TriAxis.RunSharp.Tests.09_IndexedProperties.GenIndexedProperty
=== RUN TriAxis.RunSharp.Tests.09_IndexedProperties.GenIndexedProperty
PeneloPe PiPer Picked a Peck of Pickled PePPers. How many Pickled PePPers did PeneloPe PiPer Pick?
<<< END TriAxis.RunSharp.Tests.09_IndexedProperties.GenIndexedProperty

").RunAll();
        }

        // example based on the MSDN Indexed Properties Sample (indexedproperty.cs)
        public static void GenIndexedProperty(AssemblyGen ag)
		{
            var st = ag.StaticFactory;
            var exp = ag.ExpressionFactory;

            CodeGen g;
		    ITypeMapper m = ag.TypeMapper;
		    
            TypeGen Document = ag.Public.Class("Document");
		    {
				FieldGen TextArray = Document.Private.Field(typeof(char[]), "TextArray");  // The text of the document.

				// Type allowing the document to be viewed like an array of words:
				TypeGen WordCollection = Document.Public.Class("WordCollection");
				{
					FieldGen document = WordCollection.ReadOnly.Field(Document, "document");    // The containing document
                    var document_TextArray = document.Field("TextArray", m);	// example of a saved expression - it is always re-evaluated when used

					g = WordCollection.Internal.Constructor().Parameter(Document, "d");
					{
						g.Assign(document, g.Arg("d"));
					}

					// Helper function -- search character array "text", starting at
					// character "begin", for word number "wordCount." Returns false
					// if there are less than wordCount words.Sets "start" and
					// length" to the position and length of the word within text:
					g = WordCollection.Private.Method(typeof(bool), "GetWord")
						.Parameter(typeof(char[]), "text")
						.Parameter(typeof(int), "begin")
						.Parameter(typeof(int), "wordCount")
						.Out.Parameter(typeof(int), "start")
						.Out.Parameter(typeof(int), "length")
						;
					{
						ContextualOperand text = g.Arg("text"), begin = g.Arg("begin"), wordCount = g.Arg("wordCount"),
							start = g.Arg("start"), length = g.Arg("length");

                        var end = g.Local(text.ArrayLength());
                        var count = g.Local(0);
                        var inWord = g.Local(-1);
						g.Assign(start, length.Assign(0));

                        var i = g.Local();
						g.For(i.Assign(begin), i <= end, i.Increment());
						{
                            var isLetter = g.Local(i < end && st.Invoke(typeof(char), "IsLetterOrDigit", text[i]));

							g.If(inWord >= 0);
							{
								g.If(!isLetter);
								{
									g.If(count.PostIncrement() == wordCount);
									{
										g.Assign(start, inWord);
										g.Assign(length, i - inWord);
										g.Return(true);
									}
									g.End();

									g.Assign(inWord, -1);
								}
								g.End();
							}
							g.Else();
							{
								g.If(isLetter);
								{
									g.Assign(inWord, i);
								}
								g.End();
							}
							g.End();
						}
						g.End();

						g.Return(false);
					}

					// Indexer to get and set words of the containing document:
					PropertyGen Item = WordCollection.Public.Indexer(typeof(string)).Index(typeof(int), "index");
					{
						g = Item.Getter();
						{
                            var index = g.Arg("index");

                            var start = g.Local(0);
                            var length = g.Local(0);

							g.If(g.This().Invoke("GetWord", document_TextArray, 0, index, start.Ref(), length.Ref()));
							{
								g.Return(exp.New(typeof(string), document_TextArray, start, length));
							}
							g.Else();
							{
								g.Throw(exp.New(typeof(IndexOutOfRangeException)));
							}
							g.End();
						}
						g = Item.Setter();
						{
                            var index = g.Arg("index");
                            var value = g.PropertyValue();

                            var start = g.Local(0);
                            var length = g.Local(0);

							g.If(g.This().Invoke("GetWord", document_TextArray, 0, index, start.Ref(), length.Ref()));
							{
								// Replace the word at start/length with the 
								// string "value":
								g.If(length == value.Property("Length"));
								{
									g.Invoke(typeof(Array), "Copy", value.Invoke("ToCharArray"), 0,
										document_TextArray, start, length);
								}
								g.Else();
								{
                                    var newText = g.Local(exp.NewArray(typeof(char),
										document_TextArray.ArrayLength() + value.Property("Length") - length));

									g.Invoke(typeof(Array), "Copy", document_TextArray, 0, newText,
										0, start);
									g.Invoke(typeof(Array), "Copy", value.Invoke("ToCharArray"), 0, newText,
										start, value.Property("Length"));
									g.Invoke(typeof(Array), "Copy", document_TextArray, start + length,
										newText, start + value.Property("Length"),
										document_TextArray.ArrayLength() - start
										- length);
									g.Assign(document_TextArray, newText);
								}
								g.End();
							}
							g.Else();
							{
								g.Throw(exp.New(typeof(IndexOutOfRangeException)));
							}
							g.End();
						}
					}

					// Get the count of words in the containing document:
					g = WordCollection.Public.Property(typeof(int), "Count").Getter();
					{
						ContextualOperand count = g.Local(0), start = g.Local(0), length = g.Local(0);

						g.While(g.This().Invoke("GetWord", document_TextArray, start + length, 0, start.Ref(), length.Ref()));
						{
							g.Increment(count);
						}
						g.End();

						g.Return(count);
					}
				}

				// Type allowing the document to be viewed like an "array" 
				// of characters:
				TypeGen CharacterCollection = Document.Public.Class("CharacterCollection");
				{
					FieldGen document = CharacterCollection.ReadOnly.Field(Document, "document");   // The containing document
                    var document_TextArray = document.Field("TextArray", m);

					g = CharacterCollection.Internal.Constructor().Parameter(Document, "d");
					{
						g.Assign(document, g.Arg("d"));
					}

					// Indexer to get and set characters in the containing document:
					PropertyGen Item = CharacterCollection.Public.Indexer(typeof(char)).Index(typeof(int), "index");
					{
						g = Item.Getter();
						{
							g.Return(document_TextArray[g.Arg("index")]);
						}
						g = Item.Setter();
						{
							g.Assign(document_TextArray[g.Arg("index")], g.PropertyValue());
						}
					}

					// Get the count of characters in the containing document:
					g = CharacterCollection.Public.Property(typeof(int), "Count").Getter();
					{
						g.Return(document_TextArray.ArrayLength());
					}
				}

				// Because the types of the fields have indexers, 
				// these fields appear as "indexed properties":
				FieldGen Words = Document.Public.ReadOnly.Field(WordCollection, "Words");
				FieldGen Characters = Document.Public.ReadOnly.Field(CharacterCollection, "Characters");

				g = Document.Public.Constructor().Parameter(typeof(string), "initialText");
				{
					g.Assign(TextArray, g.Arg("initialText").Invoke("ToCharArray"));
					g.Assign(Words, exp.New(WordCollection, g.This()));
					g.Assign(Characters, exp.New(CharacterCollection, g.This()));
				}

				g = Document.Public.Property(typeof(string), "Text").Getter();
				{
					g.Return(exp.New(typeof(string), TextArray));
				}
			}

			TypeGen Test = ag.Class("Test");
			{
				g = Test.Public.Static.Method(typeof(void), "Main");
				{
					var d = g.Local(exp.New(Document, "peter piper picked a peck of pickled peppers. How many pickled peppers did peter piper pick?"));

                    // Change word "peter" to "penelope":
                    var i = g.Local();
					g.For(i.Assign(0), i < d.Field("Words").Property("Count"), i.Increment());
					{
						g.If(d.Field("Words")[i] == "peter");
						{
							g.Assign(d.Field("Words")[i], "penelope");
						}
						g.End();
					}
					g.End();

					// Change character "p" to "P"
					g.For(i.Assign(0), i < d.Field("Characters").Property("Count"), i.Increment());
					{
						g.If(d.Field("Characters")[i] == 'p');
						{
							g.Assign(d.Field("Characters")[i], 'P');
						}
						g.End();
					}
					g.End();

					g.WriteLine(d.Property("Text"));
				}
			}
		}
	}
}
