namespace Syslog.Server
{
    using Syslog.Server.Data;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    class Program
    {
        /// <summary>
        /// As long this is true the Service will continue to receive new Messages.
        /// </summary>
        private static bool queueing = true;

        /// <summary>
        /// Message Queue of the type Data.Message.
        /// </summary>
        private static Queue<Message> messages = new Queue<Message>();

        /// <summary>
        /// Message Trigger
        /// </summary>
        private static AutoResetEvent messageTrigger = new AutoResetEvent(false);

        /// <summary>
        /// Listener Address
        /// </summary>
        private static IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

        /// <summary>
        /// Listener Port and Protocol
        /// </summary>
        private static UdpClient udpListener = new UdpClient(514);

        /// <summary>
        /// Received Message
        /// </summary>
        private static byte[] bytesReceive;

        private static string logFile;

        static void Main(string[] args)
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
            while (queueing || messages.Count != 0)
            {
                try
                {
                    anyIP.Port = 514;
                    // Receive the message
                    bytesReceive = udpListener.Receive(ref anyIP);

                    // push the message to the queue, and trigger the queue
                    Data.Message msg = new Data.Message
                    {
                        MessageText = Encoding.ASCII.GetString(bytesReceive),
                        RecvTime = DateTime.Now,
                        SourceIP = anyIP.Address
                    };

                    lock (messages)
                    {
                        messages.Enqueue(msg);
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
        /// Internal Message handler
        /// </summary>
        private static void HandleMessage()
        {
            while (queueing)
            {
                messageTrigger.WaitOne(5000);    // A 5000ms timeout to force processing
                Message[] messageArray = null;

                lock (messages)
                {
                    messageArray = messages.ToArray();
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
                Thread dbhandler = new Thread(() => LogToFile(message.MessageText, message.SourceIP, message.RecvTime))
                {
                    IsBackground = true
                };

                dbhandler.Start();
                Console.WriteLine(message.MessageText);
            }
        }

        /// <summary>
        /// handles the log Update, call in a new thread to reduce performance impacts on the service handling.
        /// </summary>
        /// <param name="msg">Message which was sent from the Syslog Client</param>
        /// <param name="ipSourceAddress">Source IP of the Syslog Sender</param>
        /// <param name="receiveTime">Receive Time of the Syslog Message</param>
        private static void LogToFile(string msg , IPAddress ipSourceAddress, DateTime receiveTime)
        {
            Log log = new Log();
            log.WriteToLog(msg, logFile);
        }
    }
}
