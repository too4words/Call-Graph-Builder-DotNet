using ClassLibrary1;
using System;

namespace ConsoleApplication1
{
    class Test : ITest
    {
        public void CallBar(IRemoteClass bar)
        {
            var receiver = bar;
             
            receiver.Bar();
        }

        public void DoTest()
        {
            Console.WriteLine("Test");
        }
        #region uncomment
        //bar = new LocalClass2();
        #endregion

    }


}
