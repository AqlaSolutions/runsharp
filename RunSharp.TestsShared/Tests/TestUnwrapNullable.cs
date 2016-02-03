using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class TestUnwrapNullable : TestBase
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
            Assert.That(() => TestingFacade.RunMethodTest(UnwrapNullableNull), 
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
            Assert.That(() => TestingFacade.RunMethodTest(UnwrapNullableImplicit),
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
    }
}