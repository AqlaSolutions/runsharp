using System;
using System.Reflection.Emit;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class DynamicMethodTest
    {
        [Test]
        public void Execute()
        {
            ConsoleTester.ClearAndStartCapturing();

            DynamicMethodGen dmg = DynamicMethodGen.Static(typeof(DynamicMethodTest)).Method(typeof(string), new TypeMapper())
                .Parameter(typeof(string), "name")
                .Parameter(typeof(string), "other");
            CodeGen g = dmg.GetCode();
            {
                g.Try();
                {
                    var name = g.Local(typeof(string), g.Arg("name"));
                    g.WriteLine("Hello " + name + "!");
                }
                g.CatchAll();
                {
                    g.WriteLine("Error");
                }
                g.End();
                g.Return(dmg.StaticFactory.Invoke(typeof(DynamicMethodTest), nameof(CallMe), g.Arg("other"), 2));
            }
            DynamicMethod dm = dmg.GetCompletedDynamicMethod(true);


            // reflection-style invocation
            Assert.That(dm.Invoke(null, new object[] { "Dynamic Method", "first1" }), Is.EqualTo("test240"));

            // delegate invocation
            Func<string, string, string> hello = (Func<string, string, string>)dm.CreateDelegate(typeof(Func<string, string, string>));

            Assert.That(hello("Delegate", "first1"), Is.EqualTo("test240"));

            ConsoleTester.AssertAndClear(@"Hello Dynamic Method!
Hello Delegate!
");
        }

        public static string CallMe(string first, int second)
        {
            Assert.That(first, Is.EqualTo("first1"));
            Assert.That(second, Is.EqualTo(2));
            return "test240";
        }
    }
}