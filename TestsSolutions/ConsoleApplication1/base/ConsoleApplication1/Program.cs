using ClassLibrary1;
using System.Linq;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
			var queryvar = (from arg in args
							where arg.Length > 0
						    select arg)
							.Where(arg2 => arg2.Length == 0)
							.ToList();

			var testing = new Testing();
			testing.Testing1();

			IUnique monoType = null;
            monoType.DoSomething();

            var test = new Test();
            test.DoTest();

            var test3 = new Test3();

            test3.DoTest();

            var c2 = new LocalClass2();
            c2.Bar();

            var c3 = new RemoteClass1();
            c3.Bar();

            test.CallBar(c3); // Recall type(c3) = RemoteClass1

            #region Demo Delegates
            TestDelegate.Test();
            TestDelegate.Test2();
            #endregion
        }
    }
}
