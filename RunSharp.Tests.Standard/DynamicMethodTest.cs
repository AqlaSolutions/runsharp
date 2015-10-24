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

            DynamicMethodGen dmg = DynamicMethodGen.Static(typeof(DynamicMethodTest)).Method(typeof(void), new TypeMapper()).Parameter(typeof(string), "name");
            CodeGen g = dmg.GetCode();
            g.Try();
            {
                var name = g.Local(typeof(string), g.Arg("name"));
                g.WriteLine("Hello {0}!", name);
            }
            g.CatchAll();
            {
                g.WriteLine("Error");
            }
            g.End();
            DynamicMethod dm = dmg.GetCompletedDynamicMethod(true);


            // reflection-style invocation
            dm.Invoke(null, new object[] { "Dynamic Method" });

            // delegate invocation
            Action<string> hello = (Action<string>)dm.CreateDelegate(typeof(Action<string>));

            hello("Delegate");

            ConsoleTester.AssertAndClear(@"Hello Dynamic Method!
Hello Delegate!
");

        }
    }

}