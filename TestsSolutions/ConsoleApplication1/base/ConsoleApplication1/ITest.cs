using ClassLibrary1;

namespace ConsoleApplication1
{
    public interface ITest
    {
        void DoTest();
        void CallBar(IRemoteClass bar);
    }
}
