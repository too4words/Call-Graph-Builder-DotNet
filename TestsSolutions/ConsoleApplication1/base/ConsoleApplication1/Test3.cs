using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1;

namespace ConsoleApplication1
{
    public class Test3 : ITest
    {
        public void CallBar(IRemoteClass bar)
        {
            throw new NotImplementedException();
        }

        public void DoTest()
        {
            Console.WriteLine("Test3");
        }
    }
}
