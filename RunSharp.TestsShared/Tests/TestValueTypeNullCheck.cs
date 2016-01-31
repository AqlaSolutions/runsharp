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
                g.ThrowAssert(false, "If");
            }
            g.End();
            g.DebugAssert(true, "true");
            g.ThrowAssert(true, "true");

            g.If(localInt != null);
            g.Else();
            g.ThrowAssert(false, "localInt != null");
            g.End();
        }

        [Test]
        public void ExecuteWithValueTypeArg()
        {
            TestingFacade.RunMethodTest(ExecuteWithValueTypeArg);
        }

        public static void ExecuteWithValueTypeArg(MethodGen m)
        {
            var g = m.GetCode();
            var localInt = g.Local(typeof(int));
            g.Assign(localInt, 123);
            g.DebugAssert(localInt != null, "localInt != null");
        }

        [Test]
        public void ExecuteWithNullableArg()
        {
            TestingFacade.RunMethodTest(ExecuteWithNullableArg);
        }

        public static void ExecuteWithNullableArg(MethodGen m)
        {
            var g = m.GetCode();
            var localInt = g.Local(typeof(int?));
            g.Assign(localInt, 123);
            g.DebugAssert(localInt != null, "localInt != null");
            g.Assign(localInt, null);
            g.DebugAssert(localInt == null, "localInt == null");
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
            var test=g.Local(12);
            //var l12=g.Local(12);
            //var l13=g.Local(13);
            //var l14=g.Local(14);
            //var l15=g.Local(15);
            //g.If(l12 == l13 || l15 <= l14 || ((l15 < l14) && (l15 == 1 || l15 == 2 || localInt != null) && l12 == 2));
            var ok = g.Local(typeof(bool));
            g.If(localInt != null);
            {
                g.Assign(ok, true);
            }
            g.Else();
            {
                g.ThrowAssert(false, "if2");
            }
            g.End();
            g.ThrowAssert(ok, "if1");

            g.If((localInt == null).LogicalAnd(test.Eq(test)));
            {
                g.ThrowAssert(false, "if2-1");
            }
            g.End();

            g.If(localInt != null);
            {

            }
            g.Else();
            {
                g.ThrowAssert(false, "if3");
            }
            g.End();

            g.If(localInt == null);
            {
                g.ThrowAssert(false, "if4");
            }
            g.Else();
            {

            }
            g.End();
            g.ThrowAssert(localInt != null, "localInt != null");
            g.Assign(localInt, null);
            g.ThrowAssert(localInt == null, "localInt == null");

        }
    }
}