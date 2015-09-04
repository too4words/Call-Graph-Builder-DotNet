/*
Project Orleans Cloud Service SDK ver. 1.0
 
Copyright (c) Microsoft Corporation
 
All rights reserved.
 
MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
associated documentation files (the ""Software""), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;

using Orleans;
using Orleans.Runtime;
using System.Threading.Tasks;
using System.Text;

namespace OrleansManager
{
    class Program
    {
        private static IManagementGrain systemManagement;
        const int RETRIES = 3;

        static void Main(string[] args)
        {
            Console.WriteLine("Invoked OrleansManager.exe with arguments {0}", Utils.EnumerableToString(args));

            var command = args.Length > 0 ? args[0].ToLowerInvariant() : "";

            try
            {
                RunCommand(command, args);
                Environment.Exit(0);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Terminating due to exception:");
                Console.WriteLine(exc.ToString());
            }
        }

        public static async Task<string> RunCommand(string command, string[] args)
        {
            //GrainClient.Initialize();

			systemManagement = GrainClient.GrainFactory.GetGrain<IManagementGrain>(RuntimeInterfaceConstants.SYSTEM_MANAGEMENT_ID);
            Dictionary<string, string> options = args.Skip(1)
                .Where(s => s.StartsWith("-"))
                .Select(s => s.Substring(1).Split('='))
                .ToDictionary(a => a[0].ToLowerInvariant(), a => a.Length > 1 ? a[1] : "");

            var restWithoutOptions = args.Skip(1).Where(s => !s.StartsWith("-")).ToArray();

			var res = "";
            switch (command)
            {
                case "grainstats":
                    PrintSimpleGrainStatistics(restWithoutOptions);
                    break;

                case "fullgrainstats":
                    res = await PrintGrainStatistics(restWithoutOptions);
                    break;

                case "collect":
                    CollectActivations(options, restWithoutOptions);
                    break;
            
            }
			return res;

        }

      
        private static void CollectActivations(IReadOnlyDictionary<string, string> options, IEnumerable<string> args)
        {
            var silos = args.Select(ParseSilo).ToArray();
            int ageLimitSeconds = 0;
            string s;

            if (options.TryGetValue("age", out s))
                Int32.TryParse(s, out ageLimitSeconds);

            var ageLimit = TimeSpan.FromSeconds(ageLimitSeconds);
            if (ageLimit > TimeSpan.Zero)
                systemManagement.ForceActivationCollection(silos, ageLimit);
            else
                systemManagement.ForceGarbageCollection(silos);
        }

        private static void PrintSimpleGrainStatistics(IEnumerable<string> args)
        {
            var silos = args.Select(ParseSilo).ToArray();
            var stats = systemManagement.GetSimpleGrainStatistics(silos).Result;
            Console.WriteLine("Silo                   Activations  Type");
            Console.WriteLine("---------------------  -----------  ------------");
            foreach (var s in stats.OrderBy(s => s.SiloAddress + s.GrainType))
                Console.WriteLine("{0}  {1}  {2}", s.SiloAddress.ToString().PadRight(21), Pad(s.ActivationCount, 11), s.GrainType);
        }
        
        private static async Task<string> PrintGrainStatistics(IEnumerable<string> args)
        {
            var silos = args.Select(ParseSilo).ToArray();
			if(silos.Length==0)
			{
				var hosts = await systemManagement.GetHosts();
				silos = hosts.Keys.ToArray();
			}

			
            // var stats = await systemManagement.GetSimpleGrainStatistics(silos);
			await systemManagement.ForceGarbageCollection(silos);


			var stats = await systemManagement.GetRuntimeStatistics(silos);

			var res = new StringBuilder();


            res.AppendLine("Act  Type");
			res.AppendLine("--------  -----  ------  ------------");
            foreach (var s in stats)
				res.AppendFormat("Act;{0};  Mem;{1}; CPU;{2}; Rec;{3}; Sent;{4} \n", Pad(s.ActivationCount, 8), s.MemoryUsage/1024, s.CpuUsage,
					s.ReceiveQueueLength, s.SendQueueLength);
			return res.ToString();
        }



        private static string Pad(int value, int width)
        {
            return value.ToString("d").PadRight(width);
        }

        private static SiloAddress ParseSilo(string s)
        {
            return SiloAddress.FromParsableString(s);
        }

        public static void WriteStatus(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ResetColor();
        }
    }
}
