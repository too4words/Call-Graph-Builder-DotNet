using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
	sealed class Logger
	{
		private static readonly object syncObject = new object();
		private string filename;
		private static Logger instance;

		public Logger(string filename)
		{
			this.filename = filename;
			File.Delete(filename);
		}

		public static Logger Instance
		{
			get
			{
				lock (syncObject)
				{
					if (instance == null)
					{
						var filename = @"log.txt";
						instance = new Logger(filename);
					}
				}

				return instance;
			}
		}

        public static void Log(Orleans.Runtime.Logger orleansLog, string type, string method, string format, params object[] arguments)
        {
            var message = string.Format(format, arguments);
			var threadId = Thread.CurrentThread.ManagedThreadId;

			message = string.Format("[{0}] {1}::{2}: {3}", threadId, type, method, message);

			Debug.WriteLine(message);
			//Console.WriteLine(message);
            orleansLog.Info(0, message);
        }

		public void Log(string type, string method, string format, params object[] arguments)
		{
			var message = string.Format(format, arguments);
			var threadId = Thread.CurrentThread.ManagedThreadId;

			message = string.Format("[{0}] {1}::{2}: {3}", threadId, type, method, message);

			Debug.WriteLine(message);
			//Console.WriteLine(message);

			lock (syncObject)
            {
                try
                {
                    using (var writer = File.AppendText(filename))
                    {
                        writer.WriteLine(message);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error writing log");
                }
			}
		}

		public void Log(string type, string method, object argument)
		{
			this.Log(type, method, "{0}", argument);
		}
    }
}
