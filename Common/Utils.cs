using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class PairIterator<T1, T2> : IEnumerable<Tuple<T1, T2>>
    {
        private IEnumerable<T1> enumerator1;
        private IEnumerable<T2> enumerator2;

        public PairIterator(IEnumerable<T1> enumerator1, IEnumerable<T2> enumerator2)
        {
            this.enumerator1 = enumerator1;
            this.enumerator2 = enumerator2;
        }

        public IEnumerator<Tuple<T1, T2>> GetEnumerator()
        {
            return new MyIEnumerator(this.enumerator1, this.enumerator2);
            //throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new MyIEnumerator(this.enumerator1, this.enumerator2);
        }

        internal class MyIEnumerator : IEnumerator<Tuple<T1, T2>>
        {
            private IEnumerator<T1> enumerator1;
            private IEnumerator<T2> enumerator2;

            public MyIEnumerator(IEnumerable<T1> ienumerable1, IEnumerable<T2> ienumerable2)
            {
                this.enumerator1 = ienumerable1.GetEnumerator();
                this.enumerator2 = ienumerable2.GetEnumerator();
            }

            public Tuple<T1, T2> Current
            {
                get
                {
                    return new Tuple<T1, T2>(enumerator1.Current, enumerator2.Current);
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return new Tuple<T1, T2>(enumerator1.Current, enumerator2.Current);
                }
            }

            public void Dispose()
            {
                this.enumerator1.Dispose();
                this.enumerator2.Dispose();
            }

            public bool MoveNext()
            {
                return this.enumerator1.MoveNext() && this.enumerator2.MoveNext();
            }

            public void Reset()
            {
                this.enumerator1.Reset();
                this.enumerator2.Reset();
            }
        }
    }

    /// <summary>
    /// We use this Bag for 'counting' number of references of concrete types.
    /// Actually we want to keep track of the number of paths with a concrete type leading to a node
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Bag<T>
    {
        private int count = 0;
        private IDictionary<T, int> multiset = new Dictionary<T, int>();
        private HashSet<T> internalSet = new HashSet<T>();
        internal int Add(T e)
        {
            int c;
            if (!multiset.TryGetValue(e, out c))
            {
                c = 0;
                internalSet.Add(e);
            }
            multiset[e] = c + 1;
            count++;

            return c + 1;
        }
        public int Occurrences(T e)
        {
            int c;
            if (multiset.TryGetValue(e, out c))
            {
                return c;
            }
            return 0;
        }
        public void UnionWith(IEnumerable<T> set)
        {
            foreach (var e in set)
            {
                Add(e);
            }
        }
        public bool Remove(T e)
        {
            int c;
            if (!multiset.TryGetValue(e, out c) || c <= 0)
            {
                return false;
            }
            else
            {
                if (c == 1)
                {
                    internalSet.Remove(e);
                    multiset.Remove(e);
                }
                else
                    multiset[e] = c - 1;
                count--;
            }
            return true;
        }


        public ISet<T> ExceptWith(IEnumerable<T> set)
        {
            var res = new HashSet<T>();
            foreach (var e in set)
            {
                if (Remove(e))
                    res.Add(e);
            }
            return res;
        }
        public bool Contains(T e)
        {
            return Occurrences(e) > 0;
        }
        public ISet<T> AsSet()
        {
            if (internalSet.Count > this.Count)
            {
                return new HashSet<T>(multiset.Keys);
            }
            return internalSet;
        }
        public int Count
        {
            get { return count; }
        }
    }

}
