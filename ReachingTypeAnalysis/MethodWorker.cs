using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
    
     class MethodWorkers<Exp, Type, M> 
    {
         private static IDictionary<M, MethodWorker<Exp, Type, M>> workers = new Dictionary<M, MethodWorker<Exp, Type, M>>();
         
         public static MethodWorker<Exp, Type, M> Worker(M m)
         {
             MethodWorker<Exp, Type, M> worker; 
             if(!workers.TryGetValue(m, out worker))
             {
                 IList<Exp> parameters = new List<Exp>();
                 worker = new MethodWorker<Exp, Type, M>(m, default(Exp), default(Exp),parameters);
             }
             return worker;
         }
        public static bool RegisterWorker(MethodWorker<Exp, Type, M> methodWorker)
         {
             workers[methodWorker.Method] = methodWorker;
             return true;
         }


        public static ICollection<M> Methods()
        {
            return workers.Keys;
        }

    }
    public class CallConext<M,Exp>
    {
        public M Caller;
        public Exp CallLHS;
    }

     class MethodWorker<Exp,Type,M> 
    {
        M method;
        PropagationGraph<Exp, Type,M> propGraph = new PropagationGraph<Exp, Type,M>();

        Exp thisRef;

        public Exp ThisRef
        {
            get { return thisRef; }
            set { thisRef = value; }
        }
        IList<Exp> parameterNodes = new List<Exp>();

        public IList<Exp> ParameterNodes
        {
            get { return parameterNodes; }
            set { parameterNodes = value; }
        }
        Exp retVar;
        // t-digarb: Should I include context information?

        ISet<CallConext<M, Exp>> callers = new HashSet<CallConext<M, Exp>>();



        public PropagationGraph<Exp, Type,M> PropGraph
        {
            get { return propGraph; }
            set { propGraph = value; }
        }
        public M Method { get { return method;  } }

        public ISet<Type> GetTypes(Exp n)
        {
            return propGraph.GetTypes(n);
        }
        public MethodWorker(M m, Exp rv, Exp thisRef, IList<Exp> parameters)
        {
            this.retVar = rv;
            this.method = m;
            this.parameterNodes = parameters;
            this.thisRef = thisRef;
            if(rv!=null)
               propGraph.AddRet(rv);
            if(thisRef!=null)
                propGraph.Add(thisRef);

            foreach (var p in parameterNodes) propGraph.Add(p);

            MethodWorkers<Exp, Type, M>.RegisterWorker(this);
        }

        public void HandleCallEvent(CallMessageInfo<M,Type,Exp> callMessage)
        {
            // HandleCallEvent(callMessage.Caller, callMessage.Receivers, callMessage.ArgumentValues, callMessage.LHS);
            M caller = callMessage.Caller;
            ISet<Type> receivers = callMessage.Receivers;
            IList<ISet<Type>> argumentValues = callMessage.ArgumentValues;
            Exp lhs = callMessage.LHS;

            // Save caller info
            var context = new CallConext<M,Exp>() 
                {
                    Caller =  caller, CallLHS = lhs,
                };
            callers.Add(context);

            // Propagate type info in method 
            //propGraph.Add(thisRef, receivers);
            if(thisRef!=null)
                propGraph.DiffProp(receivers, thisRef);

            for(int i=0; i< parameterNodes.Count; i++)
            {
                var pn = parameterNodes[i];
                //propGraph.Add(pn, argumentValues[i]);
                propGraph.DiffProp(argumentValues[i], pn);
            }

            // This should be Async
            Propagate();
        }

        public void Propagate()
        {
            var callsAndRets = propGraph.Propagate();
            var calls = callsAndRets.Item1;
            var retn = callsAndRets.Item2;
            ProcessCallNodes(calls);
            if(retn) ProcessRet();

            EndOfPropagationEvent();
        }

        internal void ProcessRet()
        {
            foreach(var callerContex in callers)
            {
                DispachReturnMessage(callerContex.Caller, callerContex.CallLHS, this.retVar);
            }
        }

        internal void ProcessCallNodes(IEnumerable<CallExp<M, Type, Exp>> callNodes)
        {
            foreach (var cn in callNodes)
            {
                DispatchCallMessage(cn);
            }
        }

        public void DispatchCallMessage(CallExp<M,Type,Exp> cn)
        {
            CallMessageInfo<M, Type, Exp> callMessage = new CallMessageInfo<M, Type, Exp>(cn, this);
            var calleeWorker = MethodWorkers<Exp, Type, M>.Worker(cn.Callee);
            calleeWorker.HandleCallEvent(callMessage);
        }
        public void DispachReturnMessage(M caller, Exp lhs, Exp retVar)
        {
            var types = retVar != null ? propGraph.GetTypes(retVar) : new HashSet<Type>();

            // Jump to caller
            var callerWorker = MethodWorkers<Exp, Type, M>.Worker(caller);
            if (lhs != null)
                callerWorker.HandleReturnEvent(lhs, propGraph.GetTypes(retVar));
        }

        public void EndOfPropagationEvent()
        {
            foreach(var caller in callers)
            {
                DispachReturnMessage(caller.Caller, caller.CallLHS, retVar);
            }
        }

        public void HandleReturnEvent(Exp lhs,  IEnumerable<Type> retValues)
        {
            //propGraph.Add(lhs, retValues);
            propGraph.DiffProp(retValues, lhs);
            // This should be Async
            Propagate();

        }

    }
    
}
