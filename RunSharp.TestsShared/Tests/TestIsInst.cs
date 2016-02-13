using System.Collections;
using System.IO;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class TestIsInst : TestBase
    {
        [Test]
        public void ExecuteIsInst()
        {
            TestingFacade.RunMethodTest(ExecuteIsInst);
        }

        public static void ExecuteIsInst(MethodGen m)
        {
            var g = m.GetCode();
            var localObj = g.Local(typeof(object));
            var localStream = g.Local(typeof(Stream));
            g.Assign(localObj, g.ExpressionFactory.New(typeof(MemoryStream)));
            g.DebugAssert(true, "True");
            g.DebugAssert(localObj.Is(typeof(MemoryStream)), "Is MS");
            g.DebugAssert(localObj.Is(typeof(MemoryStream)) == true, "Is MS true");
            g.DebugAssert(localObj.Is(typeof(Stream)), "Is MS");
            g.DebugAssert(localObj.Is(typeof(Stream)) == true, "Is MS true");
            g.DebugAssert(!localObj.Is(typeof(ArrayList)), "Is NOT ArrayList");
            g.DebugAssert(localObj.Is(typeof(ArrayList)) == false, "Is NOT ArrayList true");
            g.Invoke(localObj.As(typeof(Stream)), "WriteByte", 123);
            g.DebugAssert(localObj.As(typeof(Stream)).Property("Length") == 1);
            g.Assign(localStream, localObj.As(typeof(Stream)));
            g.DebugAssert(localStream.Property("Position") == 1, "Position");
        }

        [Test]
        public void ExecuteAsValueType()
        {
            TestingFacade.RunMethodTest(ExecuteAsValueType);
        }

        public static void ExecuteAsValueType(MethodGen m)
        {
            var g = m.GetCode();
            var localObj = g.Local(typeof(object));
            g.Assign(localObj, g.ExpressionFactory.New(typeof(MemoryStream)));
            g.ThrowAssert(!localObj.Is(typeof(int)), "Is MS");
        }

        [Test]
        public void ExecuteValueType()
        {
            TestingFacade.RunMethodTest(ExecuteValueType);
        }

        public static void ExecuteValueType(MethodGen m)
        {
            var g = m.GetCode();
            var localObj = g.Local(typeof(object));
            g.Assign(localObj, 1);
            g.DebugAssert(localObj.Is(typeof(int)), "Is int");
            g.DebugAssert(!localObj.Is(typeof(long)), "Is long");
            g.DebugAssert(!localObj.Is(typeof(long?)), "Is long?");
            g.DebugAssert(localObj.As(typeof(int)) == 1, "1");
            g.DebugAssert(localObj.As(typeof(int)) != 2, "21");
            g.DebugAssert(localObj.As(typeof(int?)) != 2, "22");
            g.DebugAssert(localObj.As(typeof(int?)) == 1, "23");
            g.ThrowAssert(localObj.Is(typeof(int)), "Is int");
            g.ThrowAssert(!localObj.Is(typeof(long)), "Is long");
            g.ThrowAssert(!localObj.Is(typeof(long?)), "Is long?");
            g.ThrowAssert(localObj.As(typeof(int)) == 1, "1");
            g.ThrowAssert(localObj.As(typeof(int)) != 2, "21");
            g.ThrowAssert(localObj.As(typeof(int?)) != 2, "22");
            g.ThrowAssert(localObj.As(typeof(int?)) == 1, "23");
        }
    }
}