using ClassLibrary1;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
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
