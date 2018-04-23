//-----------------------------------------------------------------------
// <copyright file="Log.cs" company="https://github.com/jhueppauff/Syslog.Server">
// Copyright 2018 Jhueppauff
// MIT License
// For licence details visit https://github.com/jhueppauff/Syslog.Server/blob/master/LICENSE
// </copyright>
//-----------------------------------------------------------------------

namespace Syslog.Server.Data
{
    using System.IO;
    using System.Text;

    /// <summary>
    /// Log Class
    /// </summary>
    public class Log
    {
        /// <summary>
        /// Lock object to log file access
        /// </summary>
        private static readonly object Locker = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="Log"/> class.
        /// </summary>
        public Log()
        {
        }

        /// <summary>
        /// Writes to log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="path">The path of the file.</param>
        public void WriteToLog(string message, string path)
        {
            lock (Locker)
            {
                using (FileStream fileStream = new FileStream(path: path, mode: FileMode.Append))
                {
                    byte[] encodedText = Encoding.Unicode.GetBytes(message);
                    fileStream.Write(encodedText, 0, encodedText.Length);
                }
            }
        }
    }
}