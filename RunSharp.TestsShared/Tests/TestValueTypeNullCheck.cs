using System.Collections;
using System.IO;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class TestValueTypeNullCheck : TestBase
    {
        [Test]
        public void ExecuteWithValueType()
        {
            TestingFacade.RunMethodTest(ExecuteWithValueType);
        }

        public static void ExecuteWithValueType(MethodGen m)
        {
            var g = m.GetCode();
            var localInt = g.Local(typeof(int));
            g.Assign(localInt, 123);
            g.If(localInt == null);
            {
                g.DebugAssert(false);
            }
            g.End();
            g.DebugAssert(localInt != null);
            g.DebugAssert(true);


            g.If(localInt != null);
            g.Else();
            g.DebugAssert(false);
            g.End();
        }

        [Test]
        public void ExecuteWithNullable()
        {
            TestingFacade.RunMethodTest(ExecuteWithNullable);
        }

        public static void ExecuteWithNullable(MethodGen m)
        {
            var g = m.GetCode();
            var localInt = g.Local(typeof(int?));
            g.Assign(localInt, 123);
            g.If(localInt == null);
            {
                g.DebugAssert(false);
            }
            g.End();
            g.DebugAssert(localInt != null);
            g.Assign(localInt, null);
            g.DebugAssert(localInt == null);

        }
    }
}