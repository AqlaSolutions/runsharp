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
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace TriAxis.RunSharp
{
    [AttributeUsage(AttributeTargets.Method)]
    class TestArgumentsAttribute : Attribute
    {
        string[] _args;

        public TestArgumentsAttribute(params string[] args)
        {
            this._args = args;
        }

        public string[] Arguments { get { return _args; } }
    }

    public class Program
    {
        delegate void Generator(AssemblyGen ag);

        static Generator[] Examples
        {
            get
            {
                List<Generator> list = new List<Generator>();
                
                foreach (Type t in typeof(Program).Assembly.GetTypes())
                {
                    foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        ParameterInfo[] pi = mi.GetParameters();

                        if (pi.Length == 1 && pi[0].ParameterType == typeof(AssemblyGen)/* && (mi.Name == "GenCmdLine2")*/)
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

        public static void Main(string[] args)
        {
            bool noexe = !((args != null) && args.Length > 0 && args[0] == "/exe");
            string exePath = string.Empty;
            if (!noexe)
            {
                exePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "out");
                Directory.CreateDirectory(exePath);
            }

            foreach (Generator gen in Examples)
            {
                string testName = GetTestName(gen);
                Console.WriteLine(">>> GEN {0}", testName);
                string name = testName;
                AssemblyGen asm;
                asm = noexe ? new AssemblyGen(name, new CompilerOptions())
                          : new AssemblyGen(name, new CompilerOptions() { OutputPath = Path.Combine(exePath, name + ".exe") });
                gen(asm);
                if (!noexe)
                    asm.Save();
                Console.WriteLine("=== RUN {0}", testName);
                try
                {
                    if (noexe)
                    {
                        Type entryType = ((TypeBuilder)asm.GetAssembly().EntryPoint.DeclaringType).CreateType();
                        //Console.WriteLine("x2");
                        MethodInfo entryMethod = entryType.GetMethod(asm.GetAssembly().EntryPoint.Name, BindingFlags.Public | BindingFlags.Static);
                        //Console.WriteLine("x3");
                        object[] entryArgs = null;
                        if (entryMethod.GetParameters().Length == 1)
                        {
                            //Console.WriteLine("x4");
                            entryArgs = new object[] { GetTestArguments(gen) };
                        }
                        //Console.WriteLine("x5");
                        entryMethod.Invoke(null, entryArgs);
                        //Console.WriteLine("x6");
                    }
                    else
                    {
                        AppDomain.CurrentDomain.ExecuteAssembly(name, null, GetTestArguments(gen));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("!!! UNHANDLED EXCEPTION");
                    Console.WriteLine(e);
                    e = e.InnerException;
                    while (e != null)
                    {
                        Console.WriteLine(e);
                        e = e.InnerException;
                    }
                }
                Console.WriteLine("<<< END {0}", testName);
                Console.WriteLine();
                //Console.ReadLine();
            }

            // dynamic method examples
            DynamicMethodExamples();
        }

        #region Dynamic Method examples
        static void DynamicMethodExamples()
        {
            DynamicMethodGen dmg = DynamicMethodGen.Static(typeof(Program)).Method(typeof(void)).Parameter(typeof(string), "name");
            CodeGen g = dmg.GetCode();
            g.Try();
            {
                Operand name = g.Local(typeof(string), g.Arg("name"));
                g.WriteLine("Hello {0}!", name);
            }
            g.CatchAll();
            {
                g.WriteLine("Error");
            }
            g.End();
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
