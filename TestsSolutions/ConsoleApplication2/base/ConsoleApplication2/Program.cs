using System;

namespace ConsoleApplication2
{

    class C
    {
        protected int value;

        public virtual int GetValue()
        {
            return 0;
        }

        public C(int v)
        {
            this.value = v;
        }
    }

    class D : C
    {
        public D(int v)
            : base(v)
        {
        }
        public override int GetValue()
        {
            return this.value;
        }
    }

    class Program
    {
        public static int Foo(C a, C x = null, C y = null)
        {
            return y.GetValue();
        }

        public static void Main()
        {
            Foo(new C(0), y: new D(30));
        }
    }
}
