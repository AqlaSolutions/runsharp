using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class TestNullable : TestBase
    {
        [Test]
        public void UnwrapNullableValue()
        {
            TestingFacade.RunMethodTest(UnwrapNullableValue);
        }

        public static void UnwrapNullableValue(MethodGen m)
        {
            var g = m.GetCode();
            var nullable = g.Local(typeof(int?));
            var result = g.Local(typeof(int));
            g.Assign(nullable, 123);
            g.Assign(result, nullable.Cast(typeof(int)));
            g.ThrowAssert(result == 123);
        }

        [Test]
        public void UnwrapNullableNull()
        {
            Assert.That(
                () => TestingFacade.RunMethodTest(UnwrapNullableNull),
                Throws.TypeOf<TargetInvocationException>().With.InnerException.Message.EqualTo("Nullable object must have a value."));
        }

        public static void UnwrapNullableNull(MethodGen m)
        {
            var g = m.GetCode();
            var nullable = g.Local(typeof(int?));
            var result = g.Local(typeof(int));
            g.Assign(nullable, null);
            g.Assign(result, nullable.Cast(typeof(int)));
        }

        [Test]
        public void UnwrapNullableImplicit()
        {
            Assert.That(
                () => TestingFacade.RunMethodTest(UnwrapNullableImplicit),
                Throws.TypeOf<InvalidCastException>().With.Message.StartsWith("Cannot convert type from 'System.Nullable`1[[System.Int32"));
        }

        public static void UnwrapNullableImplicit(MethodGen m)
        {
            var g = m.GetCode();
            var nullable = g.Local(typeof(int?));
            var result = g.Local(typeof(int));
            g.Assign(nullable, 123);
            g.Assign(result, nullable);
            g.ThrowAssert(result == 123);
        }


        [Test]
        public void ExecuteWrapNullable()
        {
            TestingFacade.RunMethodTest(ExecuteWrapNullable);
        }

        public static void ExecuteWrapNullable(MethodGen m)
        {
            var g = m.GetCode();
            var localIntNullable = g.Local(typeof(int?));
            var localInt = g.Local(typeof(int));
            g.Assign(localInt, 123);
            g.Assign(localIntNullable, localInt);
            // branch
            g.ThrowAssert(localIntNullable != null, "localIntNullable != null");
            g.ThrowAssert(localIntNullable == localInt, "localIntNullable == localInt");
            g.ThrowAssert(localIntNullable == 123, "localIntNullable == 123");
            g.ThrowAssert(localIntNullable != 1234, "localIntNullable != 1234");
            // condition
            g.DebugAssert(localIntNullable != null, "localIntNullable != null");
            g.DebugAssert(localIntNullable == localInt, "localIntNullable == localInt");
            g.DebugAssert(localIntNullable == 123, "localIntNullable == 123");
            g.DebugAssert(localIntNullable != 1234, "localIntNullable != 1234");
        }

        [Test]
        public void ExecuteIncrementNullable()
        {
            TestingFacade.RunMethodTest(ExecuteIncrementNullable);
        }

        public static void ExecuteIncrementNullable(MethodGen m)
        {
            var g = m.GetCode();
            var localIntNullable = g.Local(typeof(int?));
            g.Assign(localIntNullable, 1);
            g.Increment(localIntNullable);
            // condition
            g.ThrowAssert(localIntNullable == 2, "2");
        }

        [Test]
        public void ExecutePostIncrementNullable()
        {
            TestingFacade.RunMethodTest(ExecutePostIncrementNullable);
        }

        public static void ExecutePostIncrementNullable(MethodGen m)
        {
            var g = m.GetCode();
            var localIntNullable = g.Local(typeof(int?));
            g.Assign(localIntNullable, 1);
            g.ThrowAssert(localIntNullable.PostIncrement() == 1, "1");
            // condition
            g.ThrowAssert(localIntNullable == 2, "2");
        }

        [Test]
        public void ExecutePreIncrementNullable()
        {
            TestingFacade.RunMethodTest(ExecutePreIncrementNullable);
        }

        public static void ExecutePreIncrementNullable(MethodGen m)
        {
            var g = m.GetCode();
            var localIntNullable = g.Local(typeof(int?));
            g.Assign(localIntNullable, 1);
            g.ThrowAssert(localIntNullable.PreIncrement() == 2, "2");
            // condition
            g.ThrowAssert(localIntNullable == 2, "2");
        }

        [Test]
        public void ExecuteCompareNullables()
        {
            TestingFacade.RunMethodTest(ExecuteCompareNullables);
        }

        public static void ExecuteCompareNullables(MethodGen m)
        {
            var g = m.GetCode();
            var a = g.Local(typeof(int?));
            var b = g.Local(typeof(int?));
            g.ThrowAssert(a == b, "a==b");
            g.Assign(a, 1);
            g.ThrowAssert(a != b, "a!=b 1");
            g.ThrowAssert(a == 1, "a==1");
            g.ThrowAssert(b != 1, "b!=1");
            g.ThrowAssert(a >= 1, "a !>=");
            g.ThrowAssert(!(a < 1), "a !<");
            g.ThrowAssert(!(b > 1), "!>");
            g.ThrowAssert(!(b < 1), "!<");

            g.Assign(b, 1);
            g.ThrowAssert(a == b, "a==b");
            g.ThrowAssert(!(a > 1), "!>");
            g.ThrowAssert(!(a < 1), "!<");

            g.Assign(b, 2);
            g.ThrowAssert(a != b, "a!=b 3");
            g.ThrowAssert(a < b, "<");
            g.ThrowAssert(a <= b, ">=");
            g.ThrowAssert(!(a > b), "!>");
        }

        [Test]
        public void ExecuteAdd()
        {
            TestingFacade.RunMethodTest(ExecuteAdd);
        }

        public static void ExecuteAdd(MethodGen m)
        {
            var g = m.GetCode();
            var a = g.Local(typeof(int?));
            var b = g.Local(typeof(int?));
            g.ThrowAssert(a + 1 != 0, "!=0");
            g.ThrowAssert(!(a + 1 == 0), "!(==0)");
            g.Assign(a, 1);
            g.ThrowAssert(a + 1 == 2, "==");
            g.ThrowAssert(a == 1, "1");
            g.Assign(b, a + 1);
            g.ThrowAssert(b == 2, "2");

        }

        [Test]
        public void ExecuteAddConvertable()
        {
            TestingFacade.RunMethodTest(ExecuteAddConvertable);
        }

        public static void ExecuteAddConvertable(MethodGen m)
        {
            var g = m.GetCode();
            var a = g.Local(typeof(int?));
            var b = g.Local(typeof(long?));
            g.Assign(a, 1);
            g.Assign(b, a + (long)1);
            g.ThrowAssert(b == (long)2, "2");
            g.ThrowAssert(b == 2, "2");
        }

        [Test]
        public void ExecuteAssignImplicit()
        {
            TestingFacade.RunMethodTest(ExecuteAssignImplicit);
        }

        public static void ExecuteAssignImplicit(MethodGen m)
        {
            var g = m.GetCode();
            var a = g.Local(typeof(int?));
            var b = g.Local(typeof(long?));
            g.Assign(a, 1);
            g.Assign(b, a);
            g.ThrowAssert(b == 1, "1");

        }

        [Test]
        public void ExecuteAssignNonNullableImplicit()
        {
            TestingFacade.RunMethodTest(ExecuteAssignNonNullableImplicit);
        }

        public static void ExecuteAssignNonNullableImplicit(MethodGen m)
        {
            var g = m.GetCode();
            var a = g.Local(typeof(long?));
            var b = g.Local(typeof(int));
            g.Assign(b, 1);
            g.Assign(a, b);
            g.ThrowAssert(b == 1, "1");
        }

        [Test]
        public void ExecuteCast()
        {
            TestingFacade.RunMethodTest(ExecuteCast);
        }

        public static void ExecuteCast(MethodGen m)
        {
            var g = m.GetCode();
            var a = g.Local(typeof(long?));
            var b = g.Local(typeof(int?));
            g.Assign(a, 1);
            g.Assign(b, a.Cast(typeof(int?)));
            g.ThrowAssert(b == 1, "1");

        }
    }
}