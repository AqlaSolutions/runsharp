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
using System.IO;

namespace TriAxis.RunSharp.Examples
{
	static class Indexers
	{
		// example based on the MSDN Indexers Sample (indexer.cs)
		public static void GenIndexer(AssemblyGen ag)
		{
			// Class to provide access to a large file
			// as if it were a byte array.
			TypeGen FileByteArray = ag.Public.Class("FileByteArray");
			{
				FieldGen stream = FileByteArray.Field(typeof(Stream), "stream");	// Holds the underlying stream
				// used to access the file.

				// Create a new FileByteArray encapsulating a particular file.
				CodeGen g = FileByteArray.Public.Constructor(typeof(string));
				{
					g.Assign(stream, Exp.New(typeof(FileStream), g.Arg(0, "fileName"), FileMode.Open));
				}

				// Close the stream. This should be the last thing done
				// when you are finished.
				g = FileByteArray.Public.Method(typeof(void), "Close");
				{
					g.Invoke(stream, "Close");
					g.Assign(stream, null);
				}

				// Indexer to provide read/write access to the file.
				PropertyGen Item = FileByteArray.Public.Indexer(typeof(byte), typeof(long));	// long is a 64-bit integer
				{
					// Read one byte at offset index and return it.
					g = Item.Getter();
					{
						Operand buffer = g.Local(Exp.NewArray(typeof(byte), 1));
						g.Invoke(stream, "Seek", g.Arg(0, "index"), SeekOrigin.Begin);
						g.Invoke(stream, "Read", buffer, 0, 1);
						g.Return(buffer[0]);
					}
					// Write one byte at offset index and return it.
					g = Item.Setter();
					{
						Operand buffer = g.Local(Exp.NewInitializedArray(typeof(byte), g.PropertyValue()));
						g.Invoke(stream, "Seek", g.Arg(0, "index"), SeekOrigin.Begin);
						g.Invoke(stream, "Write", buffer, 0, 1);
					}
				}

				// Get the total length of the file.
				FileByteArray.Public.Property(typeof(long), "Length").Getter().Code
					.Return(stream.Invoke("Seek", 0, SeekOrigin.End));
			}

			// Demonstrate the FileByteArray class.
			// Reverses the bytes in a file.
			TypeGen Reverse = ag.Public.Class("Reverse");
			{
				CodeGen g = Reverse.Public.Static.Method(typeof(void), "Main", typeof(string[]));
				{
					Operand args = g.Arg(0, "args");

					// Check for arguments.
					g.If(args.ArrayLength() != 1);
					{
						g.WriteLine("Usage : Indexer <filename>");
						g.Return();
					}
					g.End();

					// Check for file existence
					g.If(!Static.Invoke(typeof(File), "Exists", args[0]));
					{
						g.WriteLine("File " + args[0] + " not found.");
						g.Return();
					}
					g.End();

					Operand file = g.Local(Exp.New(FileByteArray, args[0]));
					Operand len = g.Local(file.Property("Length"));

					// Swap bytes in the file to reverse it.
					Operand i = g.Local(typeof(long));
					g.For(i.Assign(0), i < len / 2, i.Increment());
					{
						Operand t = g.Local();

						// Note that indexing the "file" variable invokes the
						// indexer on the FileByteStream class, which reads
						// and writes the bytes in the file.
						g.Assign(t, file[i]);
						g.Assign(file[i], file[len - i - 1]);
						g.Assign(file[len - i - 1], t);
					}
					g.End();

					g.Invoke(file, "Close");
				}
			}
		}
	}
}
