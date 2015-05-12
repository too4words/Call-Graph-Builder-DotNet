// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ReachingTypeAnalysis
{    public partial class Tests
    {
        public void TestSimpleCall(AnalysisStrategy strategy)
        {
            var source = @"
using System;
public class D:C
{
    public  override void m2(C b)
    {
    }
}
public class C 
{
    int f = 0;
    C g;
    public C m1(C a)
    {
         f = 0;
         g = this;
         this.m2(a);
         m2(g);
         return a;
    }
    public virtual void m2(C b)
    {
        Console.WriteLine(f);
    }
}
class Program
{

    public static void Main()
    {
        C d = new D();
        C c;
        c = new C();
        C h = d.m1(c);
        d.Equals(c);
    }
}";

            AnalyzeExample(source, (result, callgraph) =>
            {
                Assert.IsTrue(result.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(result.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                Assert.IsTrue(result.IsReachable(new MethodDescriptor("System","Object", "Equals"), callgraph));
                Assert.IsFalse(result.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
                Assert.IsTrue(result.IsCaller(new MethodDescriptor("C", "m1"), new MethodDescriptor("D", "m2"), callgraph));
            }, strategy);
        }

        public void TestRecursion(AnalysisStrategy strategy)
        {
            var source = @"
using System;
public class C 
{
    public static int r(int n)
    {
         if (n > 0) {
            return r(n-1) + 1;
         }
         else {
            q();
            return 0;   
         }
    }

    public static void q()
    {
        r(0);
    }
}
class Program
{
    public static void Main()
    {
        var x = C.r(100);
    }
}";

            AnalyzeExample(source, (s,callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "r"), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "r"), new MethodDescriptor("C", "r"), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "r"), new MethodDescriptor("C", "q"), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "q"), new MethodDescriptor("C", "r"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "q"), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("Program", "Main"), new MethodDescriptor("C", "r"), callgraph));
            }, strategy);
        }

        public void TestIf(AnalysisStrategy strategy)
        {
            var source = @"
public class D:C
{
    public  override C m1(C a)
    {
        return a;
    }
}
public class C 
{
    public virtual C m1(C a)
    {
         return this;
    }
}
class Program
{

    public static void Main()
    {
        C d = new D();
        C c = new C();
        C h; 
        if(d!=c) 
            h = c;
        else
            h = d; 
        C j = h.m1(c);
    }
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m1"), callgraph));
            }, strategy);
        }
        public void TestVirtualCallViaSuperClass(AnalysisStrategy strategy)
        {
            String source =
@"class MainClass {
    class SuperClass {
        public virtual void M() {

        }
    }
    class SubClass : SuperClass {
        public override void M() {
        }
    }
    
    static void Main(string[] argv) {
        SuperClass oSub = new SubClass();
        oSub.M(); // SubClass.M() should be reachable.
    } 
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("SubClass", "M"), callgraph));
            },strategy);
        }

        public void TestCallViaInterface(AnalysisStrategy strategy)
        {
            String source =
@"class MainClass {
    interface Interface {
        void M(int x);
    }
    class SubClass : Interface {
        public void M(int x) {

        }
    }
    
    static void Main(string[] argv) {
        var oSub = new SubClass();
        oSub.M(4); // SubClass.M() should be reachable.
    } 
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("SubClass", "M"), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("MainClass", "Main"), new MethodDescriptor("SubClass", "M"), callgraph));
            }, strategy);
        }

        public void TestForLoop(AnalysisStrategy strategy)
        {
            String source =
@"class C
{
    int f;
    int inc = 1;

    public int Inc
    {
        get { return inc; }
        set { inc = value; }
    }

    public C()
    {
        f = 5;
    }
    public int Bound()
    {
        return f;
    }
    static void Main(string[] argv)
    {
        int i = 0;
        for (C c = new C(); i < c.Bound(); i+=c.Inc)
        {
            C d = new C();
        }
    }
}";

            AnalyzeExample(source, (s,callgraph) =>
            {
                // Need to include support for properties
                // Assert.IsTrue(s.IsReachable("C", "Inc"));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "Bound"), callgraph));
            }, strategy);
        }

        public void TestForLoopQueuing(AnalysisStrategy strategy)
        {
            String source =
@"class C
{
    int f;
    int inc = 1;

    public int Inc
    {
        get { return inc; }
        set { inc = value; }
    }

    public C()
    {
        f = 5;
    }
    public int Bound()
    {
		testMethod();
        return f;
    }
	private static void testMethod(){}

    static void Main(string[] argv)
    {
        int i = 0;
        for (C c = new C(); i < c.Bound(); i+=c.Inc)
        {
            C d = new C();
        }
    }
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                // Need to include support for properties
                // Assert.IsTrue(s.IsReachable("C", "Inc"));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "Bound"), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "Main"), new MethodDescriptor("C", "Bound"), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "Bound"), new MethodDescriptor("C", "testMethod"), callgraph));
            }, strategy);
        }

        public void TestFieldAccess(AnalysisStrategy strategy)
        {
            String source =
@"class D : C
{
    public void testMethod(){}
}
class C
{
    int f;
    C g;
    C h;
    C data;

    public C Data
    {
        get { return data; }
        set { data = value; }
    }

    public C()
    {
        f = 5;
        this.h = this;

    }
    public int Bound()
    {
        return f;
    }
    static void Main(string[] argv)
    {
        C c = new C();
        c.g = new D();
        c.g.h = new C();

        D d = (D)c.g;


        c.h.g.Data = c.g;

        C o = c.h.g.Data;

        int k = o.Bound(); 

        c.h.g.Bound();

    }
}";

            AnalyzeExample(source, (s,callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "Bound"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", ".ctor"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", ".ctor"), callgraph));
                Assert.IsTrue(s.IsCalled(
                    new MethodDescriptor("C", "Main"),
                    new MethodDescriptor("C", "Bound"), callgraph));
                Assert.IsTrue(!s.IsCalled(
                    new MethodDescriptor("C", "Main"),
                    new MethodDescriptor("D", "testMethod"), callgraph));
            }, strategy);
        }

        public void TestCallStaticDelegate(AnalysisStrategy strategy)
        {
            // Test to ensure that static methods called via delegates are reachable

            String source =
      @"class MainClass {
  delegate void DoSomethingDelegate();
 
  class M {
      public static void S() {}
      public static void Unreachable() {}
   }
    static void Test()
    {
    }

    static void Main(string[] argv) {
       DoSomethingDelegate d = M.S;
       DoSomethingDelegate d2 = Test;

       d();
    }
}";
            AnalyzeExample(source, (s,callgraph) =>
            {
                // Should not fail but it wrongly computes MainClass.HasS.S
                // This is because it does not understand "s" in "s.S"
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("M", "S"), callgraph));

                Assert.IsFalse(s.IsReachable(new MethodDescriptor("M", "Unreachable"), callgraph));
            }, strategy);
        }

        public void TestCallInterfaceDelegate(AnalysisStrategy strategy)
        {
            // Test to ensure that interface methods called via interfaces are reachable

            String source =
      @"class MainClass {
  delegate void DoSomethingDelegate();
  interface HasS {
     void S();
  }
  class SubClass : HasS {
      public void S() {}
      public void Unreachable() {}
   }

    static void Main(string[] argv) {
      HasS s = new SubClass();
  
       DoSomethingDelegate d = s.S;

       d();
    }
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("SubClass", "S"), callgraph));

                Assert.IsFalse(s.IsReachable(new MethodDescriptor("SubClass", "Unreachable"), callgraph));
            }, strategy);
        }

        public void TestClassesWithSameFieldName(AnalysisStrategy strategy)
        {
            var source = @"
using System;
public class D
{
    public C f;
    public void m2(C b)
    {
        f = b;
    }
}
public class C 
{
    public C f;
    public C m1(C a)
    {
         f = a;
         return a;
    }
    public virtual void m2(C b)
    {
        Console.WriteLine(f);
    }
}
class Program
{

    public static void Main()
    {
        D d = new D();
        C c = new C();
        d.f  = c.m1(c);
        c.f   = c; 
        d.m2(c);

    }
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
            }, strategy);
        }
        public void TestFieldLoadInCallee(AnalysisStrategy strategy)
        {
            var source = @"
public class D
{
    public void m2()
    {}
}
public class C 
{    
    public D f;
    public virtual C m1()
    {
         var b = this.f;
         b.m2();
         return this;
    }
}
class Program
{

    public static void Main()
    {
        C c = new C();
        D d = new D();
        c.f = d;
        c.m1();
    }
}";
            AnalyzeExample(source, (s, callgraph) =>
            {
                callgraph.Save("cg.dot");
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
            }, strategy);
        }

        public void TestProperty(AnalysisStrategy strategy)
        {
            var source = @"
using System;
public class D: C 
{
   public override void m2(C b) 
   {}
}
public class C 
{
    public C F { get { return f; } } 
    C f;

    public C m1(C a)
    {
         f = a;
         F.m2(this);
         this.F.m2(a.F);
         return a;
    }
    public virtual void m2(C b)
    {
        Console.WriteLine(f);
    }
}
class Program
{

    public static void Main()
    {
        C d = new D();
        C c = new C();
        C h = c.m1(d);
    }
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                // This should be reachable
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                // This is reachable because of the problem with loadnodes
                // Assert.IsFalse(s.IsReachable(new MethodDescriptor("C", "m2")));
            }, strategy);
        }
    }
}
