#if !FEAT_IKVM
using System;
using System.Reflection.Emit;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class TryCatchReachibilityBug : TestBase
    {
        [Test]
        public void Execute()
        {
            DynamicMethodGen dmg = DynamicMethodGen.Static(typeof(TryCatchReachibilityBug)).Method(typeof(string), new TypeMapper())
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
                    g.Return("a");
                }
                g.End();
                var l = g.Local();
                g.Eval(l.Assign(dmg.StaticFactory.Invoke(typeof(TryCatchReachibilityBug), nameof(CallMe), g.Arg("other"), 2)));
                g.Return(l);
            }
            dmg.GetCompletedDynamicMethod(true);
        }

        public static string CallMe(string first, int second)
        {
            return "test240";
        }
    }
}
#endif
