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
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace TriAxis.RunSharp
{
	class Program
	{
		delegate void Generator(AssemblyGen ag);

		static Generator[] examples 
		{
			get
			{
				List<Generator> list = new List<Generator>();

				foreach (Type t in typeof(Program).Assembly.GetTypes())
				{
					foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
					{
						ParameterInfo[] pi = mi.GetParameters();

						if (pi.Length == 1 && pi[0].ParameterType == typeof(AssemblyGen))
						{
							list.Add((Generator)Delegate.CreateDelegate(typeof(Generator), mi, true));
						}
					}
				}

				list.Sort(delegate(Generator g1, Generator g2)
				{
					int fileCompare = string.Compare(GetSourceFile(g1), GetSourceFile(g2), true);

					if (fileCompare != 0)
						return fileCompare;

					return string.Compare(g1.Method.Name, g2.Method.Name, true);
				});

				return list.ToArray();
			}
		}

		static string GetSourceFile(Generator g)
		{
			// this is an ugly hack, but there seems to be no easy way to retrieve source file
			// from a method
			try
			{
				g(null);
			}
			catch (NullReferenceException e)
			{
				StackTrace st = new StackTrace(e, true);
				return st.GetFrame(0).GetFileName();
			}

			return null;
		}

		static void Main(string[] args)
		{
			bool noexe = args.Length > 0 && args[0] == "/noexe";

			string exePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "out");
			Directory.CreateDirectory(exePath);

			foreach (Generator gen in examples)
			{
				string testName = gen.Method.DeclaringType.FullName + "." + gen.Method.Name;
				Console.WriteLine(">>> GEN {0}", testName);
				string name = noexe ? testName : Path.Combine(exePath, testName + ".exe");
				AssemblyGen asm = new AssemblyGen(name);
				gen(asm);
				if (!noexe)	asm.Save();
				Console.WriteLine("=== RUN {0}", testName);
                try
                {
					if (noexe)
					{
						Type entryType = asm.GetAssembly().EntryPoint.DeclaringType;
						MethodInfo entryMethod = entryType.GetMethod(asm.GetAssembly().EntryPoint.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
						object[] entryArgs = null;
						if (entryMethod.GetParameters().Length == 1)
						{
							entryArgs = new object[] { new string[] { "A", "B", "C", "D" } };
						}
						entryMethod.Invoke(null, entryArgs);
					}
					else
					{
						AppDomain.CurrentDomain.ExecuteAssembly(name, null,
							new string[] { "A", "B", "C", "D" });
					}
				}
                catch (Exception e)
                {
                    Console.WriteLine("!!! UNHANDLED EXCEPTION");
                    Console.WriteLine(e);
                }
				Console.WriteLine("<<< END {0}", testName);
				Console.WriteLine();
			}

			// dynamic method examples
			DynamicMethodExamples();
		}

		#region Dynamic Method examples
		static void DynamicMethodExamples()
		{
			DynamicMethodGen dmg = DynamicMethodGen.Static(typeof(Program)).Method(typeof(void), typeof(string));
			dmg.Code.WriteLine("Hello {0}!", dmg.Code.Arg(0, "name"));
			DynamicMethod dm = dmg.GetCompletedDynamicMethod(true);

			// reflection-style invocation
			dm.Invoke(null, new object[] { "Dynamic Method" });

			// delegate invocation
			Action<string> hello = (Action<string>)dm.CreateDelegate(typeof(Action<string>));
			hello("Delegate");
		}
		#endregion
	}
}
