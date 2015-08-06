// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

//using SolutionTraversal.Callgraph;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace ReachingTypeAnalysis
{
    public class Program : IDisposable
    {
        private StreamWriter outputWriter;

        public Program(string outputFileName)
        {
            outputWriter = File.CreateText(outputFileName);
            outputWriter.WriteLine("Test, Avg, Max, Min");
        }

        public void Dispose()
        {
			if (outputWriter != null)
			{
				outputWriter.Dispose();
				outputWriter = null;
			}
        }

        static void Main(string[] args)
        {
            //RunProgramWithOrleans(args);

			args = new string[]
			{
				//@"..\..\..\TestPlaylists\Generated.playlist", "10"
				@"ReachingTypeAnalysis.OrleansTests.LongGeneratedTestOrleansAsync4", "1"
			};

			if (args.Length == 2)
			{
				var inputName = args[0];
				var iterations = Convert.ToInt32(args[1]);

				if (inputName.EndsWith(".playlist"))
				{
					var outputFileName = Path.ChangeExtension(inputName, ".csv");
					var program = new Program(outputFileName);

					program.RunTestPlaylist(inputName, iterations);
				}
				else
				{
					var outputFileName = string.Concat(inputName, ".csv");
					var program = new Program(outputFileName);

					program.RunSingleTest(inputName, iterations);
				}
			}

            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        private void RunTestPlaylist(string playlistName, int iterations)
        {
            var xdoc = XDocument.Load(playlistName);
            var tests = from lv1 in xdoc.Descendants("Add")
                        select lv1.Attribute("Test").Value;

			foreach (var test in tests)
            {
				this.RunSingleTest(test, iterations);
            }
        }

		public void RunSingleTest(string testFullName, int iterations)
		{
			var index = testFullName.LastIndexOf('.');
			var testClass = testFullName.Substring(0, index);
			var testMethod = testFullName.Substring(index + 1);

			this.RunSingleTest(testClass, testMethod, iterations);
		}

        public void RunSingleTest(string testClass, string testMethod, int iterations)
        {
			try
			{
				Console.WriteLine("Executing {0}", testMethod);
				var minTime = long.MaxValue;
				var maxTime = 0L;
				var acumTime = 0D;

				for (var i = 0; i < iterations; i++)
				{
					Console.WriteLine("Iteration {0}", i); 
					var watch = new Stopwatch();
					var testType = Type.GetType(testClass+", ReachingTypeAnalysis");
					var test = Activator.CreateInstance(testType);
					var methodToExecute = test.GetType().GetMethod(testMethod);
                
					watch.Start();
					methodToExecute.Invoke(test, new object[0]);
					watch.Stop();

					var time = watch.ElapsedMilliseconds;
					if (time > maxTime) maxTime = time;
					if (time < minTime) minTime = time;

					acumTime += time;
				}

				var avgTime = acumTime / iterations;
                outputWriter.WriteLine("{3}, {0}, {1}, {2} {4]", avgTime, maxTime, minTime, testMethod, SolutionAnalyzer.MessageCounter);
				outputWriter.Flush();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
        }
    }
}
