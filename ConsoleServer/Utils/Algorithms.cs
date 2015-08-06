using CodeGraphModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleServer.Utils
{
    public static class Algorithms
    {
        internal static T UpdateSymbolPairCountsX<T>(T pairs, Vertex v)
            where T : Dictionary<string, SymbolReferenceCount>
        {
            // get unique symbols            
            var uniqueSymbols = (v.DeclAnnotations ?? Enumerable.Empty<DeclarationAnnotation>()).Select(ann => ann.symbolId)
                .Concat((v.RefAnnotations ?? Enumerable.Empty<ReferenceAnnotation>()).Select(ann => ann.symbolId))
                .Distinct();

            // create all the unique pairs of different symbols
            // (allows "A@^B" but not "B@^A"
            var uniquePairs = uniqueSymbols.SelectMany(firstSym =>
                uniqueSymbols
                    .Where(secondSym => firstSym.CompareTo(secondSym) < 0)
                    .Select(s => firstSym + "@^" + s));

            // increment count in pairmap if it exists

            return pairs;
        }

        internal static T UpdateSymbolPairCounts<T>(T pairCountsMap, Vertex v)
            where T : Dictionary<string, SymbolReferenceCount>
        {
            // get unique symbol annotations            
            var uniqueSymbolMap = (v.DeclAnnotations ?? Enumerable.Empty<Annotation>())
                .Concat(          (v.RefAnnotations ?? Enumerable.Empty<Annotation>()))
                .GroupBy(ann => ann.symbolId)
                .ToDictionary(group => group.Key, group => group.First());

            // create all the unique pairs of different symbols
            // (allows "A@^B" but not "B@^A"
            var uniquePairs = uniqueSymbolMap.Keys.SelectMany(firstSym =>
                uniqueSymbolMap.Keys
                    .Where(secondSym => firstSym.CompareTo(secondSym) < 0)
                    .Select(s => firstSym + "@" + s));

            // increment count in pairmap if it exists
            foreach (var pairKey in uniquePairs)
            {
                SymbolReferenceCount refCount;
                if (!pairCountsMap.TryGetValue(pairKey, out refCount))
                {
                    var tmp = pairKey.Split('@')
                        .Select(key => uniqueSymbolMap[key])
                        .ToList();

                    // create entry
                    refCount = new SymbolReferenceCount()
                    {
                        label = tmp[0].label + "<->" + tmp[1].label,
                        fullName = tmp[0].hover + "\n" + tmp[1].hover,
                        count = 0
                    };

                    pairCountsMap.Add(pairKey, refCount);
                }
                refCount.count++;
            }

            return pairCountsMap;
        }
    }
}
