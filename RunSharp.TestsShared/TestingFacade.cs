namespace TriAxis.RunSharp.Tests
{
    public static class TestingFacade
    {
        public static void RunTestMethod(ExecutableTestHelper.Generator method, string expectedOutput)
        {
            ConsoleTester.ClearAndStartCapturing();
            ExecutableTestHelper.RunTest(method, true);
            ConsoleTester.AssertAndClear(expectedOutput);
#if !FEAT_IKVM
            ExecutableTestHelper.RunTest(method, false);
            ConsoleTester.AssertAndClear(expectedOutput);
#endif
        }
    }
}