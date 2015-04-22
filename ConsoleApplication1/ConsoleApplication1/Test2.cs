using ClassLibrary1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    public class Test2 : ITest
    {
        public void CallBar(IRemoteClass bar)
        {
            throw new NotImplementedException();
        }

        public void DoTest()
        {
            Console.WriteLine("Test2");           
            IRemoteClass remote = new RemoteClass1();
            remote.Bar();
            CallBar(remote);
        }
    }
}
