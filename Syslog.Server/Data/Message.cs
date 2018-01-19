namespace Syslog.Server.Data
{
    using System;
    using System.Net;

    /// <summary>
    /// Message Data
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the Time on which the Syslog Message was receive
        /// </summary>
        public DateTime RecvTime { get; set; }

        /// <summary>
        /// Gets or sets the Message Text of the Syslog Package
        /// </summary>
        public string MessageText { get; set; }

        /// <summary>
        /// Gets or sets the source IP of the Syslog Sender
        /// </summary>
        public IPAddress SourceIP { get; set; }
    }
}