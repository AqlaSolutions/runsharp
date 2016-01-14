using System.IO;
using System.Threading;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class LeakDetectionTest : TestBase
    {
        [Test]
        public void TestNotLeaked()
        {
            TestingFacade.RunTest(
                ag =>
                    {
                        TypeGen DeclareArraysSample = ag.Class("DecalreArraysSample");
                        {
                            CodeGen g = DeclareArraysSample.Public.Static.Method(typeof(void), "Main");
                            {
                                var asStream = ag.ExpressionFactory.New(typeof(MemoryStream)).Cast(typeof(Stream)).SetNotLeaked();
                                asStream.GetType().GetHashCode();
                            }
                        }
                    });
        }

        [Test]
        public void TestLeaked()
        {
            UnhandledExceptionCheck = ex => Assert.That(ex.Count, Is.EqualTo(2));
            TestingFacade.RunTest(
                ag =>
                    {
                        TypeGen DeclareArraysSample = ag.Class("DecalreArraysSample");
                        {
                            CodeGen g = DeclareArraysSample.Public.Static.Method(typeof(void), "Main");
                            {
                                var asStream = ag.ExpressionFactory.New(typeof(MemoryStream)).Cast(typeof(Stream));
                                asStream.GetType().GetHashCode();
                            }
                        }
                    });
        }
    }
}