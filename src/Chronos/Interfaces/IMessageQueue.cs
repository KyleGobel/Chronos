using System;
using System.Security.Cryptography.X509Certificates;

namespace Chronos.Interfaces
{
    public interface IMessageQueue
    {
        bool RequeueOnFailure { get; set; }

        /// <summary>
        /// Infinite looping of handling messages, should be started on a new thread
        /// </summary>
        /// <param name="handler">The handling function, return true to Ack the message, or false to Nack</param>
        void HandleMessages<T>(Func<T, bool> handler);

        /// <summary>
        /// Handles a single message, will wait for whatever Timeout is set to 
        /// </summary>
        /// <param name="handler">The handler function, return true to Ack a message, or false to Nack it</param>
        /// <param name="receivedMsg">returns if a message was handled or if no message was handled and the timeout was hit</param>
        void HandleMessage<T>(Func<T, bool> handler, out bool receivedMsg);

        int GetQueueCount<T>();

        void PublishMessage<T>(T message);
    }

    public static class MessageQueueExtensions
    {
        public static string GetInQueueName(this IMessageQueue mq, Type type)
        {
            return string.Format("mq.{0}.inq", type.Name);
        }
    }
}