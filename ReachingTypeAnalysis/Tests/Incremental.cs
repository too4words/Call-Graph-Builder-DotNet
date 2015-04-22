// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if False
namespace ReachingTypeAnalysis
{
    public partial class Tests
    {
        [TestMethod]
        [TestCategory("Incremental")]
        public void TestRemoveAsignment()
        {
            var source = @"
public class D:C
{
    public C f;
    public override void m2(C b)
    {
        f = b;
    }
    public override  void m3()
    {
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
    }
    public virtual void m3()
    {
    }
}
class Program
{

    public static void Main()
    {
        D d = new D();
        C c = new C();
        C e; 

        if(true) 
            e = c;
        else
            e = d;

        d.f  = e.m1(e);
        c.f   = e; 
        e.m2(c);
        d.f.m3();
    }
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m3"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m3"), callgraph));

                s.RemoveAsignment(new MethodDescriptor("Program", "Main"), "e", "d");

                var newCallgraph = s.GenerateCallGraph();

                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), newCallgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m2"), newCallgraph));
                // since we remove e = d d.m2 is not longer reachable
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("D", "m2"), newCallgraph));
                // This one should be reachable
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m3"), newCallgraph));
                // this one shouldn't be reachable
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("D", "m3"), newCallgraph));
            });
        }

        [TestMethod]
        [TestCategory("Incremental")]
        public void TestRemoveAlloc()
        {
            var source = @"
public class D:C
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
    }
}
class Program
{

    public static void Main()
    {
        D d = new D();
        C c = new C();
        C e; 

        if(true) 
            e = c;
        else
            e = d;

        d.f  = e.m1(c);
        c.f   = e; 
        e.m2(c);

    }
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m2"), callgraph));

                s.RemoveTypesFromNode(new MethodDescriptor("Program", "Main"), "c");

                var newCallgraph = s.GenerateCallGraph();

                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), newCallgraph));
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("C", "m2"), newCallgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m2"), newCallgraph));
            });
        }

        [TestMethod]
        [TestCategory("Incremental")]
        public void TestReplaceMethod()
        {
            var source = @"
public class D:C
{
    public C f;
    public override void m2(C b)
    {
        f = b;
    }
    public override  void m3()
    {
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
    }
    public virtual void m3()
    {
    }
}
class Program
{

    public static void Main()
    {
        D d = new D();
        C c = new C();
        C e; 

        if(true) 
            e = c;
        else
            e = d;

        d.f  = e.m1(e);
        c.f   = e; 
        e.m2(c);
        d.f.m3();
    }
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m3"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m3"), callgraph));

                var newBody= @"    
    {
        D d = new D();
        C c = new C();
        C e; 

        if(true) 
            e = c;
//        else
//            e = d;

        d.f  = e.m1(e);
        c.f   = e; 
        e.m2(c);
        d.f.m3();
    }";
                s.UpdateMethod(new MethodDescriptor("Program", "Main"), newBody, callgraph);

                var newCallgraph = s.GenerateCallGraph();

                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), newCallgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m2"), newCallgraph));
                // since we remove e = d d.m2 is not longer reachable
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("D", "m2"), newCallgraph));
                // This one should be reachable
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m3"), newCallgraph));
                // this one shouldn't be reachable
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("D", "m3"), newCallgraph));
            });
        }
        [TestMethod]
        [TestCategory("Incremental")]
                public void TestReplaceMethod2()
        {
            var source = @"
public class D:C
{
    public C f;
    public override void m2(C b)
    {
        f = b;
    }
    public override  void m3()
    {
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
    }
    public virtual void m3()
    {
    }
}
class Program
{

    public static void Main()
    {
        D d = new D();
        C c = new C();
        C e; 
        e = c;

        d.f  = e.m1(e);
        c.f   = e; 
        e.m2(c);
        d.f.m3();
    }
}";

            AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m3"), callgraph));
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("D", "m3"), callgraph));

                var newBody= @"    
{
    f = a;
    return new D();
}";
                s.UpdateMethod(new MethodDescriptor("C", "m1"), newBody, callgraph);

                var newCallgraph = s.GenerateCallGraph();

                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), newCallgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m2"), newCallgraph));
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("D", "m2"), newCallgraph));
                // This one shouldn't be reachable because now D.f = D instead of C
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("C", "m3"), newCallgraph));
                // this one now is  reachable
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m3"), newCallgraph));
            });
        }
    }
}
#endif