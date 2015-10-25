using System;
using System.Collections;
using System.Collections.Generic;

namespace TriAxis.RunSharp
{    

    public class TestingFacade
    {
        private static readonly ExecutableTestHelper ExecutableTestHelper = new ExecutableTestHelper();

        public static IEnumerable<Action> GetTestsForGenerator(ExecutableTestHelper.Generator method, string expectedOutput)
        {
            yield return () =>
                {
                    ConsoleTester.ClearAndStartCapturing();
                    ExecutableTestHelper.RunTest(method, true);
                    ConsoleTester.AssertAndClear(expectedOutput);
                };
#if !FEAT_IKVM
            yield return () =>
                {
                    ConsoleTester.ClearAndStartCapturing();
                    ExecutableTestHelper.RunTest(method, false);
                    ConsoleTester.AssertAndClear(expectedOutput);
                };

#endif
        }
    }
}