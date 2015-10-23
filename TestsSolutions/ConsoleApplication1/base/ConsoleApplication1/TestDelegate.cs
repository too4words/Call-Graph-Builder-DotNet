using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class TestDelegate
    {
        delegate void DoSomethingDelegate();
        interface HasS
        {
            void S();
        }
        class SubClass : HasS
        {
            public void S()
            {
                var test = new Test();
                test.CallBar(null);
            }
            
            public void Unreachable() { }
        }

        class M
        {
            public static void S()
            {
                //var anotherTest = new Test2();
                //anotherTest.CallBar(null);
            }
            public static void Unreachable() { }
        }
        public static void Test()
        {
            DoSomethingDelegate d = M.S;
            d();
        }

        public static void Test2()
        {
            var s = new SubClass();
            DoSomethingDelegate d1 = M.S;
            DoSomethingDelegate d2 = s.S;
            d1();
            d2();
        }
    }
}
