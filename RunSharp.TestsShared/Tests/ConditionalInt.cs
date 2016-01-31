using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class ConditionalInt : TestBase
    {
        [Test]
        public void ExecuteConditionalInt()
        {
            TestingFacade.RunMethodTest(ExecuteConditionalInt);
        }

        public static void ExecuteConditionalInt(MethodGen m)
        {
            var g = m.GetCode();
            var localInt = g.Local(typeof(int));
            g.Assign(localInt, 123);
            g.Assign(
                localInt,
                ((Operand)true).Conditional(((Operand)true).Conditional(124, 0), ((Operand)true).Conditional(125, 1)));
            g.DebugAssert(localInt == 124);

            //g.If(localInt == null);
            //{
            //    g.ThrowAssert(false, "If");
            //}
            //g.End();
            //g.DebugAssert(true, "true");
            //g.ThrowAssert(true, "true");

            //g.If(localInt != null);
            //g.Else();
            //g.ThrowAssert(false, "localInt != null");
            //g.End();
        }
    }
}