/*
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
using System.Collections;
using System.Collections.Generic;

namespace TriAxis.RunSharp
{    

    public class TestingFacade
    {

#if SILVERLIGHT && FEAT_IKVM
#error Wrong conditonal compilation symbols
#endif
        static readonly ExecutableTestHelper ExecutableTestHelper = new ExecutableTestHelper();

        public static IEnumerable<Action> GetTestsForGenerator(ExecutableTestHelper.Generator method, string expectedOutput, string name = null)
        {
#if !SILVERLIGHT
            yield return () =>
                {
                    ConsoleTester.ClearAndStartCapturing();
                    ExecutableTestHelper.RunTest(method, true, name);
                    ConsoleTester.AssertAndClear(expectedOutput);
                };
#endif
#if !FEAT_IKVM
            yield return () =>
                {
                    ConsoleTester.ClearAndStartCapturing();
                    ExecutableTestHelper.RunTest(method, false, name);
                    ConsoleTester.AssertAndClear(expectedOutput);
                };

#endif
        }

        public static void RunMethodTest(MethodGenerator method, string name = null)
        {
            ExecutableTestHelper.Generator gen = ag =>
            {
                TypeGen t = ag.Class("Test");
                {
                    method(t.Public.Static.Method(typeof(void), "Main"));
                }
            };
            RunTest(gen, ExecutableTestHelper.GetTestName(method.Method));
        }

        public delegate void MethodGenerator(MethodGen mg);

        public static void RunTest(ExecutableTestHelper.Generator method, string name = null)
        {
#if !SILVERLIGHT
            ExecutableTestHelper.RunTest(method, true, name);
            return;
#endif
#if !FEAT_IKVM
            ExecutableTestHelper.RunTest(method, false, name);
#endif
        }
    }
}