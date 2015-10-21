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
    public class CmdLineTestClass
    {
        int _value = 5;
        public int GetValue()
        {
            return _value;
        }
        public static CmdLineTestClass Default = new CmdLineTestClass();
    }
    static class _02_CommandLine
    {
        // example based on the MSDN Command Line Parameters Sample (CmdLine2.cs)
        [TestArguments("arg1", "arg2", "arg3", "arg4")]
        public static void GenCmdLine2(AssemblyGen ag)
        {
            TypeGen commandLine3 = ag.Public.Class("CommandLine3");
            {
                CodeGen g = commandLine3.Public.Method(typeof(void), "Main3").Parameter(typeof(string[]), "args");
                {
                    Operand args = g.Arg("args");
                    g.WriteLine("Number of command line 3 parameters = {0}",
                        args.Property("Length"));
                    Operand s = g.ForEach(typeof(string), args);
                    {
                        g.WriteLine(s);
                    }
                    g.End();
                    g.WriteLine(g.This().Invoke("GetType").Property("BaseType").Property("Name"));

                    Operand inst = Static.Field(typeof(CmdLineTestClass), "Default");
                    g.WriteLine(inst.Invoke("GetValue"));
                    //inst.Field("value").Assign(2);
                    /*g.Assign(inst.Field("value"), 2);
                    g.WriteLine(inst.Invoke("GetValue"));*/

                }


                commandLine3.Public.CommonConstructor();
            }

            TypeGen commandLine2 = ag.Public.Class("CommandLine2");
            {
                CodeGen g = commandLine2.Public.Static.Method(typeof(void), "Main").Parameter(typeof(string[]), "args");
                {
                    //g.Invoke(CommandLine3, "Main3", g.Arg("args"));
                    Operand cl = g.Local(Exp.New(commandLine3));
                    g.WriteLine(0);
                    g.Invoke(cl, "Main3", g.Arg("args"));
                    Operand args = g.Arg("args");
                    g.WriteLine("Number of command line parameters = {0}",
                        args.Property("Length"));
                    Operand s = g.ForEach(typeof(string), args);
                    {
                        g.WriteLine(s);
                    }
                    g.End();


                }
            }


        }
    }
}
