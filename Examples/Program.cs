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
	class Program
	{
		delegate void Generator(AssemblyGen ag);

		static Generator[] examples = { 
			Examples.HelloWorld.GenHello1,
			Examples.HelloWorld.GenHello3,
			Examples.CommandLine.GenCmdLine2,
			Examples.Arrays.GenArrays,
			Examples.Properties.GenPerson,
			Examples.Properties.GenShapeTest,
			Examples.Versioning.GenVersioning,
			Examples.CollectionClasses.GenTokens2,
			Examples.Structs.GenStruct1,
			Examples.Structs.GenStruct2,
			Examples.Indexers.GenIndexer,
			Examples.IndexedProperties.GenIndexedProperty,
			Examples.Conversions.GenConversion,
			Examples.Conversions.GenStructConversion,
			Examples.OperatorOverloading.GenComplex,
			Examples.OperatorOverloading.GenDbBool,
			Examples.Delegates.GenBookstore,
			Examples.Delegates.GenCompose,
			Examples.Events.GenEvents1,
			Examples.ExplicitImplementation.GenExplicit2,
			Examples.BreakContinue.GenBreakContinue,
		};

		static void Main(string[] args)
		{
			string exePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "out");
			Directory.CreateDirectory(exePath);

			foreach (Generator gen in examples)
			{
				string testName = gen.Method.DeclaringType.FullName + "." + gen.Method.Name;
				Console.WriteLine(">>> GEN {0}", testName);
				string name = Path.Combine(exePath, testName + ".exe");
				AssemblyGen asm = new AssemblyGen(name);
				gen(asm);
				asm.Save();
				Console.WriteLine("=== RUN {0}", testName);
				AppDomain.CurrentDomain.ExecuteAssembly(name, null,
					new string[] { "A", "B", "C", "D" });
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
