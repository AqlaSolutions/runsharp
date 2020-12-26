using System;
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
            var prev = RunSharpDebug.LeakingDetection;
            // exception in a finalizer may terminate runtime
            // we don't want this
            RunSharpDebug.LeakingDetection = LeakingDetectionMode.StoreAndContinue;
            try
            {
                RunSharpDebug.RetrieveLeaks();
                Assert.IsEmpty(RunSharpDebug.RetrieveLeaks());
                TestingFacade.RunTest(
                    ag =>
                    {
                        Assert.IsEmpty(RunSharpDebug.RetrieveLeaks());
                        TypeGen DeclareArraysSample = ag.Class("DecalreArraysSample");
                        {
                            CodeGen g = DeclareArraysSample.Public.Static.Method(typeof(void), "Main");
                            {
                                var asStream = ag.ExpressionFactory.New(typeof(MemoryStream)).Cast(typeof(Stream));
                                asStream.GetType().GetHashCode();
                            }
                        }

                    });

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Assert.IsNotEmpty(RunSharpDebug.RetrieveLeaks());
            }
            finally
            {
                RunSharpDebug.LeakingDetection = prev;
            }
        }
    }
}