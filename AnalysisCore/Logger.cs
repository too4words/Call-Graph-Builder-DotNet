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
	sealed public class Logger
	{
		private static readonly object syncObject = new object();
		private string filename;
		private static Logger instance;
		// private static Orleans.Runtime.Logger.Severity level = Orleans.Runtime.Logger.Severity.Info;
		public static Orleans.Runtime.Logger OrleansLogger;

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

        public static void LogVerbose(Orleans.Runtime.Logger orleansLog, string type, string method, string format, params object[] arguments)
        {
			if (orleansLog.IsVerbose)
			{
				var message = string.Format(format, arguments);
				var threadId = Thread.CurrentThread.ManagedThreadId;

				message = string.Format("[{0}] {1}::{2}: {3}", threadId, type, method, message);
				//Trace.TraceInformation(message);
				// Debug.WriteLine(message);
				//Console.WriteLine(message);
				orleansLog.Verbose(0, message);
			}
        }

        public static void LogWarning(Orleans.Runtime.Logger orleansLog, string type, string method, string format, params object[] arguments)
        {
            var message = string.Format(format, arguments);
            var threadId = Thread.CurrentThread.ManagedThreadId;

            message = string.Format("[{0}] {1}::{2}: {3}", threadId, type, method, message);
            //Trace.TraceInformation(message);
            // Debug.WriteLine(message);
            Console.WriteLine(message);
            orleansLog.Warn(0, message);
        }

        public static void LogError(Orleans.Runtime.Logger orleansLog, string type, string method, string format, params object[] arguments)
        {
            var message = string.Format(format, arguments);
            var threadId = Thread.CurrentThread.ManagedThreadId;

            message = string.Format("[{0}] {1}::{2}: {3}", threadId, type, method, message);
            //Trace.TraceInformation(message);
            // Debug.WriteLine(message);
            //Console.WriteLine(message);
            orleansLog.Error(0, message);
        }

        public static void LogInfo(Orleans.Runtime.Logger orleansLog, string type, string method, string format, params object[] arguments)
		{
			var message = string.Format(format, arguments);
			var threadId = Thread.CurrentThread.ManagedThreadId;

			message = string.Format("[{0}]{1}::{2}: {3}", threadId, type, method, message);

			//Trace.TraceInformation(message);
			// Debug.WriteLine(message);
			//Console.WriteLine(message);
			orleansLog.Info(0, message);
		}
		
		public static void LogForDebug(Orleans.Runtime.Logger orleansLog, string format, params object[] arguments)
		{
#if DEBUG
			orleansLog.Warn(0, format, arguments);
#endif
		}

		public static void LogInfoForDebug(Orleans.Runtime.Logger orleansLog, string format, params object[] arguments)
		{
#if DEBUG
			orleansLog.Info(0, format, arguments);
#endif
		}

		public static void LogS(string type, string method, string format, params object[] arguments)
        {
			if (OrleansLogger != null)
			{
				LogVerbose(OrleansLogger, type, method, format, arguments);
			}
			else 
			{
				Instance.Log(type, method, format, arguments);
			}
        }

		//public static async Task LogToFile(string format, params object[] arguments)
		//{
		//	var retryCount = 0;

		//	do
		//	{
		//		try
		//		{
		//			var message = string.Format(format, arguments);

		//			using (var writer = File.AppendText(@"C:\temp\debug.log"))
		//			{
		//				await writer.WriteLineAsync(message);
		//			}

		//			break;
		//		}
		//		catch
		//		{
		//			await Task.Delay(10);
		//			retryCount++;
		//		}
		//	}
		//	while (retryCount < 3);
		//}

		public static void Log(string format, params object[] arguments)
		{
			var message = string.Format(format, arguments);

			//Trace.TraceInformation(message);
			Debug.WriteLine(message);
			//Console.WriteLine(message);
		}

		public void Log(string type, string method, string format, params object[] arguments)
		{            
			var message = string.Format(format, arguments);
			var threadId = Thread.CurrentThread.ManagedThreadId;

			message = string.Format("[{0}] {1}::{2}: {3}", threadId, type, method, message);

			//Debug.WriteLine(message);
			//Console.WriteLine(message);
            /*
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
             */ 
		}

		public void Log(string type, string method, object argument)
		{
			this.Log(type, method, "{0}", argument);
		}
	}
}
