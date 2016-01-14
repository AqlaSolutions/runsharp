using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class TestResources : TestBase
    {
        [Test]
        public void Execute()
        {
            Assert.That(RunSharpInternal.Test, Is.Not.Null.And.Not.Empty);
        }
    }
}