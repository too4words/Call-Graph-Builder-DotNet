// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReachingTypeAnalysis.Analysis;
using System;
using System.Collections.Generic;

namespace ReachingTypeAnalysis
{   
	 public partial class BasicTests 
	 {
	    public static void TestSimpleCall(AnalysisStrategyKind strategy)
        {
			#region source code
			var source = @"
using System;
public class D:C
{
    public override void m2(C b)
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
        C h = d.m1(d);
        h.m2(c);
        d.Equals(c);
    }
}";
			#endregion

			AnalyzeExample(source, (result, callgraph) =>
            {
                Assert.IsTrue(result.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(result.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                Assert.IsTrue(result.IsReachable(new MethodDescriptor(new TypeDescriptor("System", "Object","mscorlib"), "Equals", false), callgraph));
                Assert.IsFalse(result.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
                Assert.IsTrue(result.IsCaller(new MethodDescriptor("C", "m1"), new MethodDescriptor("D", "m2"), callgraph));

                // Assert.IsTrue(CallGraphQueryInterface.GetInvocationCountAsync(result.Strategy, new MethodDescriptor("C", "m1")).Result == 2);
                // I don't know why I started numbering by 1
                //var callees = CallGraphQueryInterface.GetCalleesAsync(result.Strategy, new MethodDescriptor("C", "m1"), 1).Result;
                //Assert.IsTrue(callees.Contains(new MethodDescriptor("D", "m2")));

            }, strategy);
        }

		public static void TestRemoveMethodSimpleCall(AnalysisStrategyKind strategy)
		{
			#region original source code
			var source = @"
using System;
public class D:C
{
    public override C m2(C b)
    {
        return new D();
    }
    public override void m3()
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
    public virtual C m2(C b)
    {
        Console.WriteLine(f);
        return new C();
    }
    public virtual void m3()
    {
    }
}
class Program
{
    public static void Main()
    {
        C d = new D();
        C c;
        c = new C();
        C h = d.m1(d);
        C k = h.m2(c);
        k.m3();
    }
}";
			#endregion

            #region modified source code
            var newSource = @"
using System;
public class D:C
{
    public override void m3()
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
    public virtual C m2(C b)
    {
        Console.WriteLine(f);
        return new C();
    }
    public virtual void m3()
    {
    }
}
class Program
{
    public static void Main()
    {
        C d = new D();
        C c;
        c = new C();
        C h = d.m1(d);
        C k = h.m2(c);
        k.m3();
    }
}";
            #endregion

			AnalyzeExample(source,
				(result, callgraph) =>
				{
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
//					Assert.IsTrue(result.IsReachable(new MethodDescriptor(new TypeDescriptor("System", "Object", "mscorlib"), "Equals", false), callgraph));
					Assert.IsFalse(result.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
					Assert.IsTrue(result.IsCaller(new MethodDescriptor("C", "m1"), new MethodDescriptor("D", "m2"), callgraph));
                    Assert.IsTrue(result.IsReachable(new MethodDescriptor("D", "m3"), callgraph));
                    Assert.IsFalse(result.IsReachable(new MethodDescriptor("C", "m3"), callgraph));

					// Assert.IsTrue(CallGraphQueryInterface.GetInvocationCountAsync(result.Strategy, new MethodDescriptor("C", "m1")).Result == 2);
					// I don't know why I started numbering by 1
					//var callees = CallGraphQueryInterface.GetCalleesAsync(result.Strategy, new MethodDescriptor("C", "m1"), 1).Result;
					//Assert.IsTrue(callees.Contains(new MethodDescriptor("D", "m2")));
				},
				(result) =>
				{
					result.RemoveMethodAsync(new MethodDescriptor(new TypeDescriptor("", "D"), "m2", false, (new TypeDescriptor[] { new TypeDescriptor("", "C") })), newSource).Wait();
				},
				(result, callgraph) =>
				{
					Assert.IsFalse(result.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                    Assert.IsTrue(result.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
                    Assert.IsFalse(result.IsReachable(new MethodDescriptor("D", "m3"), callgraph));
                    Assert.IsTrue(result.IsReachable(new MethodDescriptor("C", "m3"), callgraph));
				},
				strategy);
		}

		public static void TestAddMethodSimpleCall(AnalysisStrategyKind strategy)
		{
			#region original source code
			var source = @"
using System;

class Program
{
    public static void Main()
    {
    }
}";
			#endregion

			#region modified source code
			var newSource = @"
using System;

class Program
{
	public static void NewMethod(int p)
	{
		Console.WriteLine(p);
	}

    public static void Main()
    {
		NewMethod(5);
    }
}";
			#endregion

			AnalyzeExample(source,
				(result, callgraph) =>
				{
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("Program", "Main", true), callgraph));
				},
				(result) =>
				{
					result.AddMethodAsync(new MethodDescriptor("Program", "NewMethod", true), newSource).Wait();
					result.UpdateMethodAsync(new MethodDescriptor("Program", "Main", true), newSource).Wait();
				},
				(result, callgraph) =>
				{
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("Program", "Main", true), callgraph));
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("Program", "NewMethod", true), callgraph));
				},
				strategy);
		}

		public static void TestUpdateMethodSimpleCall(AnalysisStrategyKind strategy)
		{
			#region original source code
			var source = @"
using System;

class C
{
	public virtual void Middle(C p)
	{
		p.M1(5);
		p.M2(5);
	}

	public void M1(int p)
	{
		Console.WriteLine(p);
	}

	public virtual void M2(int p)
	{
		Console.WriteLine(p);
	}
}

class D : C
{
	public override void Middle(C p)
	{
		p.M1(3);
	}

	public override void M2(int p)
	{
		Console.WriteLine(p);
	}
}

class Program
{
    public static void Main()
    {
		C d = new D();
		d.Middle(d);
    }
}";
			#endregion

			#region modified source code
			var newSource = @"
using System;

class C
{
	public virtual void Middle(C p)
	{
		p.M1(5);
		p.M2(5);
	}

	public void M1(int p)
	{
		Console.WriteLine(p);
	}

	public virtual void M2(int p)
	{
		Console.WriteLine(p);
	}
}

class D : C
{
	public override void Middle(C p)
	{
		p.M2(3);
	}

	public override void M2(int p)
	{
		Console.WriteLine(p);
	}
}

class Program
{
    public static void Main()
    {
		C d = new D();
		d.Middle(d);
    }
}";
			#endregion

			AnalyzeExample(source,
				(result, callgraph) =>
				{
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("Program", "Main", true), callgraph));
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("D", "Middle"), callgraph));
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("C", "M1"), callgraph));
					Assert.IsFalse(result.IsReachable(new MethodDescriptor("C", "M2"), callgraph));
					Assert.IsFalse(result.IsReachable(new MethodDescriptor("D", "M2"), callgraph));
				},
				(result) =>
				{
					result.UpdateMethodAsync(new MethodDescriptor(new TypeDescriptor("", "D"), "Middle", false, (new TypeDescriptor[] { new TypeDescriptor("", "C") })), newSource).Wait();
				},
				(result, callgraph) =>
				{
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("Program", "Main", true), callgraph));
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("D", "Middle"), callgraph));
					Assert.IsFalse(result.IsReachable(new MethodDescriptor("C", "M1"), callgraph));
					Assert.IsFalse(result.IsReachable(new MethodDescriptor("C", "M2"), callgraph));
					Assert.IsTrue(result.IsReachable(new MethodDescriptor("D", "M2"), callgraph));
				},
				strategy);
		}

		public static  void TestRecursion(AnalysisStrategyKind strategy)
        {
			#region source code
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
			#endregion

			AnalyzeExample(source, (s,callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "r", true), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "r", true), new MethodDescriptor("C", "r", true), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "r", true), new MethodDescriptor("C", "q", true), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "q", true), new MethodDescriptor("C", "r", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "q", true), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("Program", "Main", true), new MethodDescriptor("C", "r", true), callgraph));
            }, strategy);
        }

        public static void TestIf(AnalysisStrategyKind strategy)
        {
			#region source code
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
			#endregion

			AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m1"), callgraph));
            }, strategy);
        }

        public static void TestVirtualCallViaSuperClass(AnalysisStrategyKind strategy)
        {
			#region source code
			var source =
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
			#endregion

			AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("SubClass", "M"), callgraph));
            },strategy);
        }

        public static  void TestCallViaInterface(AnalysisStrategyKind strategy)
        {
			#region source code
			var source =
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
			#endregion

			AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("SubClass", "M"), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("MainClass", "Main", true), new MethodDescriptor("SubClass", "M"), callgraph));
            }, strategy);
        }

        public static void TestForLoop(AnalysisStrategyKind strategy)
		{
			#region source code
			var source =
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
			#endregion

			AnalyzeExample(source, (s,callgraph) =>
            {
                // Need to include support for properties
                // Assert.IsTrue(s.IsReachable("C", "Inc"));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "Bound"), callgraph));
            }, strategy);
        }

        public static void TestForLoopQueuing(AnalysisStrategyKind strategy)
        {
			#region source code
			var source =
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
			#endregion

			AnalyzeExample(source, (s, callgraph) =>
            {
                // Need to include support for properties
                // Assert.IsTrue(s.IsReachable("C", "Inc"));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "Bound"), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "Main", true), new MethodDescriptor("C", "Bound"), callgraph));
                Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "Bound"), new MethodDescriptor("C", "testMethod"), callgraph));
            }, strategy);
        }

        public static void TestFieldAccess(AnalysisStrategyKind strategy)
        {
			#region source code
			var source =
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
			#endregion

			AnalyzeExample(source, (s,callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "Bound"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", ".ctor"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", ".ctor"), callgraph));
                Assert.IsTrue(s.IsCalled(
                    new MethodDescriptor("C", "Main", true),
                    new MethodDescriptor("C", "Bound"), callgraph));
                Assert.IsTrue(!s.IsCalled(
                    new MethodDescriptor("C", "Main", true),
                    new MethodDescriptor("D", "testMethod"), callgraph));
            }, strategy);
        }

		// Test to ensure that static methods called via delegates are reachable
        public static  void TestCallStaticDelegate(AnalysisStrategyKind strategy)
        {			
			#region source code
			var source =
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
			#endregion

			AnalyzeExample(source, (s,callgraph) =>
            {
                // Should not fail but it wrongly computes MainClass.HasS.S
                // This is because it does not understand "s" in "s.S"
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("M", "S", true), callgraph));

                Assert.IsFalse(s.IsReachable(new MethodDescriptor("M", "Unreachable", true), callgraph));
            }, strategy);
        }

        // Test to ensure that interface methods called via interfaces are reachable
        public static void TestCallInterfaceDelegate(AnalysisStrategyKind strategy)
        {
			#region source code
			var source =
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
			#endregion

			AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("SubClass", "S"), callgraph));

                Assert.IsFalse(s.IsReachable(new MethodDescriptor("SubClass", "Unreachable"), callgraph));
            }, strategy);
        }

        public static void TestClassesWithSameFieldName(AnalysisStrategyKind strategy)
        {
			#region source code
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
			#endregion

			AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
            }, strategy);
        }
        public static  void TestFieldLoadInCallee(AnalysisStrategyKind strategy)
        {
			#region source code
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
			#endregion

			AnalyzeExample(source, (s, callgraph) =>
            {
                callgraph.Save("cg.dot");
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                Assert.IsFalse(s.IsReachable(new MethodDescriptor("C", "m2"), callgraph));
            }, strategy);
        }

        public static  void TestProperty(AnalysisStrategyKind strategy)
        {
			#region source code
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
			#endregion

			AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "m1"), callgraph));
                // This should be reachable
                //Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "m2"), callgraph));
                // This is reachable because of the problem with loadnodes
                // Assert.IsFalse(s.IsReachable(new MethodDescriptor("C", "m2")));
            }, strategy);
        }

		public static void TestArrowMethodBody(AnalysisStrategyKind strategy)
		{
			#region source code
			var source = @"
using System;
class Program
{
	public static void Foo(int x)
	{
		Console.WriteLine(x);
	}	

    public static void Main() => Foo(5);
}";
			#endregion

			AnalyzeExample(source, (s, callgraph) =>
			{
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("Program", "Main", true), callgraph));
				// This should be reachable
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("Program", "Foo", true), callgraph));
			}, strategy);
		}

		public static void TestLambda(AnalysisStrategyKind strategy)
        {
            #region source code
            var source = @"
using System;
class Program
{
	public static void Foo(int x)
	{
		Console.WriteLine(x);
	}	

    public static void Main()
    {
        Func<int, int> lambda = x => { var i = x + 1; Foo(i); return i * x;  }; 
        var result = lambda(2);     
    }
}";
            #endregion

            AnalyzeExample(source, (s, callgraph) =>
            {
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("Program", "Main", true), callgraph));
				// This should be reachable
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("Program", "Foo", true), callgraph));
			}, strategy);
        }

		public static void TestNamedParameters(AnalysisStrategyKind strategy)
		{
			#region source code
			var source = @"
using System;

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
	public D(int v) : base(v)
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
}";
			#endregion

			AnalyzeExample(source, (s, callgraph) =>
			{
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("Program", "Main", true), callgraph));
				// This should be reachable
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("Program", "Foo", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("D", "GetValue"), callgraph));
			}, strategy);
		}
	}
}
