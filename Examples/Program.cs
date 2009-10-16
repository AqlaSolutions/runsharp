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

namespace TriAxis.RunSharp
{
	[AttributeUsage(AttributeTargets.Method)]
	class TestArgumentsAttribute : Attribute
	{
		string[] args;

		public TestArgumentsAttribute(params string[] args)
		{
			this.args = args;
		}

		public string[] Arguments { get { return args; } }
	}

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
					int cmp = string.Compare(g1.Method.DeclaringType.Namespace, g2.Method.DeclaringType.Namespace, true);

					if (cmp == 0)
					{
						cmp = string.Compare(g1.Method.DeclaringType.Name.TrimStart('_'), g2.Method.DeclaringType.Name.TrimStart('_'), true);

						if (cmp == 0)
							cmp = string.Compare(g1.Method.Name, g2.Method.Name, true);
					}

					return cmp;
				});

				return list.ToArray();
			}
		}

		static string GetTestName(Generator g)
		{
			Type declType = g.Method.DeclaringType;
			return declType.Namespace + "." + declType.Name.TrimStart('_') + "." + g.Method.Name;
		}

		static string[] GetTestArguments(Generator g)
		{
			TestArgumentsAttribute taa = Attribute.GetCustomAttribute(g.Method, typeof(TestArgumentsAttribute)) as TestArgumentsAttribute;
			if (taa == null)
				return null;
			return taa.Arguments;
		}

		static void Main(string[] args)
		{
			bool noexe = args.Length > 0 && args[0] == "/noexe";

			string exePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "out");
			Directory.CreateDirectory(exePath);

			foreach (Generator gen in examples)
			{
				string testName = GetTestName(gen);
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
							entryArgs = new object[] { GetTestArguments(gen) };
						}
						entryMethod.Invoke(null, entryArgs);
					}
					else
					{
						AppDomain.CurrentDomain.ExecuteAssembly(name, null,
							GetTestArguments(gen));
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
			DynamicMethodGen dmg = DynamicMethodGen.Static(typeof(Program)).Method(typeof(void)).Parameter(typeof(string), "name");
			dmg.GetCode().WriteLine("Hello {0}!", dmg.GetCode().Arg("name"));
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
