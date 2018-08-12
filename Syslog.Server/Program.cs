//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="https://github.com/jhueppauff/Syslog.Server">
// Copyright 2018 Jhueppauff
// MIT License
// For licence details visit https://github.com/jhueppauff/Syslog.Server/blob/master/LICENSE
// </copyright>
//-----------------------------------------------------------------------

namespace Syslog.Server
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Syslog.Server.Data;

    /// <summary>
    /// Program class
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class Program : IDisposable
    {
        /// <summary>
        /// As long this is true the Service will continue to receive new Messages.
        /// </summary>
        private static readonly bool queueing = true;

        /// <summary>
        /// Message Queue of the type Data.Message.
        /// </summary>
        private static Queue<Message> messageQueue = new Queue<Message>();

        /// <summary>
        /// Message Trigger
        /// </summary>
        private static AutoResetEvent messageTrigger = new AutoResetEvent(false);

        /// <summary>
        /// Listener Address
        /// </summary>
        private static IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 514);
        
        /// <summary>
        /// Listener Port and Protocol
        /// </summary>
        private static UdpClient udpListener = new UdpClient(514);

        /// <summary>
        /// The log file
        /// </summary>
        private static string logFile;

        /// <summary>
        /// The disposed value
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            if (args[0] != null)
            {
                logFile = args[0];
            }
            else
            {
                Console.WriteLine("Missing Argument (logfile)");
            }

            // Main processing Thread
            Thread handler = new Thread(new ThreadStart(HandleMessage))
            {
                IsBackground = true
            };
            handler.Start();
          
            /* Main Loop */
            /* Listen for incoming data on udp port 514 (default for SysLog events) */
            while (queueing || messageQueue.Count != 0)
            {
                try
                {
                    anyIP.Port = 514;

                    // Receive the message
                    byte[] bytesReceive = udpListener.Receive(ref anyIP);

                    // push the message to the queue, and trigger the queue
                    Data.Message msg = new Data.Message
                    {
                        MessageText = Encoding.ASCII.GetString(bytesReceive),
                        RecvTime = DateTime.Now,
                        SourceIP = anyIP.Address
                    };

                    lock (messageQueue)
                    {
                        messageQueue.Enqueue(msg);
                    }

                    messageTrigger.Set();
                }
                catch (Exception)
                {
                    // ToDo: Add Error Handling
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    udpListener.Dispose();
                }

                this.disposedValue = true;
            }
        }

        /// <summary>
        /// Internal Message handler
        /// </summary>
        private static void HandleMessage()
        {
            while (queueing)
            {
                messageTrigger.WaitOne(5000);    // A 5000ms timeout to force processing
                Message[] messageArray = null;

                lock (messageQueue)
                {
                    messageArray = messageQueue.ToArray();
                }

                Thread messageprochandler = new Thread(() => HandleMessageProcessing(messageArray))
                {
                    IsBackground = true
                };
                messageprochandler.Start();
            }
        }

        /// <summary>
        /// Message Processing handler, call in a new thread
        /// </summary>
        /// <param name="messages">Array of type <see cref="Data.Message"/></param>
        private static void HandleMessageProcessing(Data.Message[] messages)
        {
            foreach (Data.Message message in messages)
            {
                LogToFile(message.MessageText, message.SourceIP, message.RecvTime);
                Console.WriteLine(message.MessageText);

                if (Program.messageQueue.Count != 0)
                {
                    Program.messageQueue.Dequeue();
                }
            }
        }

        /// <summary>
        /// handles the log Update, call in a new thread to reduce performance impacts on the service handling.
        /// </summary>
        /// <param name="msg">Message which was sent from the Syslog Client</param>
        /// <param name="ipSourceAddress">Source IP of the Syslog Sender</param>
        /// <param name="receiveTime">Receive Time of the Syslog Message</param>
        private static void LogToFile(string msg, IPAddress ipSourceAddress, DateTime receiveTime)
        {
            Log log = new Log();
            log.WriteToLog($"{msg}; {ipSourceAddress}; {receiveTime}\n", logFile);
        }
    }
}
