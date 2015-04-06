namespace Chronos.RabbitMq
{
    public class HandleQueueConfig
    {
        /// <summary>
        /// Flag indicating whether the message
        /// should be requeued to this queue if you return
        /// a Success = false on the HandleQueueResult
        /// </summary>
        public bool RequeueOnFailure { get; set; } 

        /// <summary>
        /// If you are forwarding the result onward, the exchange
        /// to forward to's name
        /// </summary>
        public string ReplyToExchangeName { get; set; }

        /// <summary>
        /// The RouteKey if you forwarding the message
        /// </summary>
        public string ReplyToRouteKey { get; set; }

        /// <summary>
        /// The Queue name that we are handling
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Flag indicating whether or not we are forwarding 
        /// messages to another exchange when we successfully handle 
        /// this one
        /// </summary>
        public bool Reply { get; set; }

        /// <summary>
        /// How long to wait for a message before timing out and canceling
        /// the operation
        /// </summary>
        public int QueueReadTimeoutMs { get; set; }
    }
}