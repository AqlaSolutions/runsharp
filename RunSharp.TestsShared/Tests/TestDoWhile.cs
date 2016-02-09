using System;
using System.Reflection;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class TestDoWhile : TestBase
    {
        [Test]
        public void FalseCondition()
        {
            TestingFacade.RunMethodTest(FalseCondition);
        }

        public static void FalseCondition(MethodGen m)
        {
            var g = m.GetCode();
            var local = g.Local(false);
            var counter = g.Local(0);
            g.DoWhile();
            {
                g.Assign(local, true);
                g.Increment(counter);
            }
            g.EndDoWhile(false);
            g.ThrowAssert(counter == 1);
            g.ThrowAssert(local == true);
        }

        [Test]
        public void FalseVarCondition()
        {
            TestingFacade.RunMethodTest(FalseVarCondition);
        }

        public static void FalseVarCondition(MethodGen m)
        {
            var g = m.GetCode();
            var local = g.Local(false);
            var condition = g.Local(123);
            var counter = g.Local(0);
            g.DoWhile();
            {
                g.Assign(local, true);
                g.Increment(counter);
            }
            g.EndDoWhile(condition != 123);
            g.ThrowAssert(local);
            g.ThrowAssert(counter == 1);
        }

        [Test]
        public void CounterCondition()
        {
            TestingFacade.RunMethodTest(CounterCondition);
        }

        public static void CounterCondition(MethodGen m)
        {
            var g = m.GetCode();
            var counter = g.Local(10);
            g.DoWhile();
            {
                g.Decrement(counter);
            }
            g.EndDoWhile(counter >= 0);
            g.ThrowAssert(counter == -1);
        }
    }
}