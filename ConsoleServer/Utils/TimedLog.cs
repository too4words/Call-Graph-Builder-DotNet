using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleServer.Utils
{
    public class TimedLog : IDisposable
    {
        private Stopwatch stopwatch = Stopwatch.StartNew();

        public string LogMessage { get; set; }

        public static TimedLog Time(string message = "Time Elapsed: ")
        {
            return new TimedLog(message);
        }

        private TimedLog(string message)
        {
            LogMessage = message;
        }

        public void Dispose()
        {
            Debug.WriteLine(LogMessage + " :: Time: " + stopwatch.Elapsed);
        }
    }
}
