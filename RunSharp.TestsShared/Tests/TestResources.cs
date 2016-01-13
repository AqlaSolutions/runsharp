using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class TestResources
    {
        [Test]
        public void Execute()
        {
            Assert.That(RunSharpInternal.Test, Is.Not.Null.And.Not.Empty);
        }
    }
}