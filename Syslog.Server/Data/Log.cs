using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Syslog.Server.Data
{
    class Log
    {
        public Log()
        {

        }

        private static readonly object locker = new object();

        public void WriteToLog (string message, string path)
        {
            lock(locker)
            {
                StreamWriter sw;
                sw = File.AppendText(path);
                sw.WriteLine(message);
                sw.Close();
            }
        }

    }
}
