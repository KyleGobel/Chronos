﻿using System;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

namespace Chronos.RabbitMq
{
    /// <summary>
    /// Object that indicates back to the rabbit client the results
    /// of your operation
    /// </summary>
    public class HandleQueueResult
    {
        /// <summary>
        /// If true, the message will be acknowledged
        /// if false the message will be negative-acknowledged
        /// </summary>
        public bool Success { get; set; }
         
        /// <summary>
        /// If you indicated to ReplyToQueue on the HandleQueueConfig
        /// this is the message that will be forwarded to that queue
        /// only when Success = true
        /// </summary>
        public string ReplyBody { get; set; }

        /// <summary>
        /// Setting Cancel to true will break the infinite loop after this
        /// message is handled
        /// </summary>
        public bool Cancel { get; set; }

        public IBasicProperties ReplyToProperties { get; set; }
    }

    public class QueueMessage
    {
        public IBasicProperties MessageProperties { get; set; }

        public Lazy<string> Message
        {
            get
            {
                return new Lazy<string>(() =>
                {
                    if (RawMessage != null && RawMessage.Any())
                        return Encoding.UTF8.GetString(RawMessage);
                    return String.Empty;

                });
            }
        }

        public byte[] RawMessage { get; set; }
    }
}