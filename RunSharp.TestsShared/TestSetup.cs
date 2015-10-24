using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [SetUpFixture]
    public class TestSetup
    {
        [SetUp]
        public void Setup()
        {
            ConsoleTester.Initialize();
        }
    }
}