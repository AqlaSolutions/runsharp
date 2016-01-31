using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    [TestFixture]
    public class TestIfStatement : TestBase
    {
        // g.If(l12 == l13 || l15 <= l14 || ((l15 < l14) && l12 == 2));
        [Test]
        public void Equality1()
        {
            TestingFacade.RunMethodTest(Equality1);
        }

        public static void Equality1(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If(l1 == l2);
            {
                g.ThrowAssert(false, "Equality");
            }
            g.Else();
            {
                g.Return();
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void Equality2()
        {
            TestingFacade.RunMethodTest(Equality2);
        }

        public static void Equality2(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If(l1 == l1);
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void And()
        {
            TestingFacade.RunMethodTest(And);
        }

        public static void And(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If((l1 == l1) && (l2 == l2));
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void And2()
        {
            TestingFacade.RunMethodTest(And2);
        }

        public static void And2(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If(l1 == l1 && l2 == l3);
            {
                g.ThrowAssert(false, "Equality");
            }
            g.Else();
            {
                g.Return();
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void Or()
        {
            TestingFacade.RunMethodTest(Or);
        }

        public static void Or(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If(l1 == l1 || l2 == l2);
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void Or2()
        {
            TestingFacade.RunMethodTest(Or2);
        }

        public static void Or2(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If(l1 == l1 || l2 == l3);
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void Or3()
        {
            TestingFacade.RunMethodTest(Or3);
        }

        public static void Or3(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If(l2 == l3 || l1 == l1);
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void Or4()
        {
            TestingFacade.RunMethodTest(Or4);
        }

        public static void Or4(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If(l2 == l3 || l1 == l2);
            {
                g.ThrowAssert(false, "Equality");
            }
            g.Else();
            {
                g.Return();
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void OrInAnd()
        {
            TestingFacade.RunMethodTest(OrInAnd);
        }

        public static void OrInAnd(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If((l2 == l2 || l1 == l2) && (l3 == l4));
            {
                g.ThrowAssert(false, "Equality");
            }
            g.Else();
            {
                g.Return();
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void OrInAnd2()
        {
            TestingFacade.RunMethodTest(OrInAnd2);
        }

        public static void OrInAnd2(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If((l2 == l2 || l1 == l2) && (l3 == l3));
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void OrInAnd3()
        {
            TestingFacade.RunMethodTest(OrInAnd3);
        }

        public static void OrInAnd3(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If((l3 == l3) && (l2 == l2 || l1 == l2));
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void OrInAnd4()
        {
            TestingFacade.RunMethodTest(OrInAnd4);
        }

        public static void OrInAnd4(MethodGen m)
        {
            var g = m.GetCode();
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If((l4 == l3) && (l2 == l2 || l1 == l2));
            {
                g.ThrowAssert(false, "Equality");
            }
            g.Else();
            {
                g.Return();
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void AndInOr()
        {
            TestingFacade.RunMethodTest(AndInOr);
        }

        public static void AndInOr(MethodGen m)
        {
            var g = m.GetCode();
            
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If((l4 == l3) || (l2 == l2 && l1 == l1));
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void AndInOr2()
        {
            TestingFacade.RunMethodTest(AndInOr2);
        }

        public static void AndInOr2(MethodGen m)
        {
            var g = m.GetCode();
            
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If((l3 == l3) || (l3 == l2 && l1 == l1));
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void AndInOr3()
        {
            TestingFacade.RunMethodTest(AndInOr3);
        }

        public static void AndInOr3(MethodGen m)
        {
            var g = m.GetCode();
            
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If((l3 == l4) || (l2 == l2 && l1 == l1));
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void AndInOr4()
        {
            TestingFacade.RunMethodTest(AndInOr4);
        }

        public static void AndInOr4(MethodGen m)
        {
            var g = m.GetCode();
            
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If((l2 == l2 && l1 == l1) || (l3 == l4));
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }

        [Test]
        public void AndInOr5()
        {
            TestingFacade.RunMethodTest(AndInOr5);
        }

        public static void AndInOr5(MethodGen m)
        {
            var g = m.GetCode();
            
            Operand l1 = 1;
            Operand l2 = 2;
            Operand l3 = 3;
            Operand l4 = 4;
            g.If((l2 == l2 && l1 == l2) || (l3 == l3));
            {
                g.Return();
            }
            g.Else();
            {
                g.ThrowAssert(false, "Equality");
            }
            g.End();
            g.ThrowAssert(false, "Not returned");
        }
    }
}