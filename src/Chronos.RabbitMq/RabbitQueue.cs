using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chronos.Configuration;
using Chronos.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using ServiceStack.Logging;

namespace Chronos.RabbitMq
{
    public class RabbitQueue : IMessageQueue
    {
        public static TimeSpan Timeout = new TimeSpan(0, 0, 0, 3);
  
        private readonly RabbitMqConnectionString _connStr;
        private readonly ISerializer _serializer;
        private static readonly ILogger Log = Serilog.Log.ForContext<RabbitQueue>();

        public RabbitQueue(RabbitMqConnectionString connStr, ISerializer serializer = null, bool requeueOnFailure = true)
        {
            _connStr = connStr;
            _serializer = serializer ?? new ServiceStackSerializer();
            RequeueOnFailure = requeueOnFailure;
        }

        public bool RequeueOnFailure { get; set; }


        /// <summary>
        /// Handle a queue
        /// </summary>
        /// <param name="config">Various configuration options about how to handle the queue</param>
        /// <param name="handler">Given a string message, return a result a object indcating what to do next</param>
        public void HandleQueue(HandleQueueConfig config, Func<QueueMessage, HandleQueueResult> handler)
        {
            var factory = new ConnectionFactory
            {
                HostName = _connStr.Host,
                Port = _connStr.Port,
                UserName = _connStr.Username,
                Password = _connStr.Password
            };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var consumer = new QueueingBasicConsumer(channel);

                    //only get one message at a time
                    Log.Verbose("Setting Prefetch count to 1");
                    channel.BasicQos(0, 1, false);

                    channel.BasicConsume(config.QueueName, false, consumer);
                    bool cancel = false;

                    while (!cancel)
                    {
                        var ea = default(BasicDeliverEventArgs);
                        if (consumer.Queue.Dequeue(config.QueueReadTimeoutMs, out ea))
                        {
                            var qMsg = new QueueMessage()
                            {
                                MessageProperties = ea.BasicProperties,
                                RawMessage = ea.Body
                            };

                            var result = handler(qMsg);

                            if (result.Cancel)
                            {
                                Log.Information("Canceling queue handling");
                                cancel = true;
                            }

                            if (result.Success)
                            {
                                if (ea.BasicProperties.ReplyToAddress != null || result.ReplyToProperties.ReplyToAddress != null)
                                {
                                    var rta = result.ReplyToProperties.ReplyToAddress ??
                                                     ea.BasicProperties.ReplyToAddress;
                                    Log.Debug("Publishing message to {ExchangeAddress}",rta.ToString());
                                    channel.BasicPublish(rta.ExchangeName,rta.RoutingKey, false, result.ReplyToProperties, Encoding.UTF8.GetBytes(result.ReplyBody));
                                }
                                channel.BasicAck(ea.DeliveryTag, false);
                                Log.Debug("Message Acknowledged");
                            }
                            else
                            {
                                channel.BasicNack(ea.DeliveryTag, false, config.RequeueOnFailure);
                                Log.Debug("Message Negative Acknowleded with retry: {Reque}", config.RequeueOnFailure);
                            }
                        }
                        else
                        {
                            cancel = true;
                        }
                    }
                }
            }
        }
        public void HandleQueueMultiple(HandleQueueConfig config, ushort messagesToRecv, Func<Dictionary<ulong, QueueMessage>,Dictionary<ulong, HandleQueueResult>> handler)
        {
            var factory = new ConnectionFactory
            {
                HostName = _connStr.Host,
                Port = _connStr.Port,
                UserName = _connStr.Username,
                Password = _connStr.Password
            };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var consumer = new QueueingBasicConsumer(channel);

                    //only get one message at a time
                    Log.Verbose("Setting Prefetch count to {PrefetchCount}", messagesToRecv);
                    channel.BasicQos(0, messagesToRecv, false);

                    channel.BasicConsume(config.QueueName, false, consumer);
                    bool cancel = false;

                    while (!cancel)
                    {
                        var ea = default(BasicDeliverEventArgs);

                        int msgCount = 0;

                        var deliveryArgs = new Dictionary<ulong, QueueMessage>();
                        Log.Debug("Consuming Messages");
                        while (consumer.Queue.Dequeue(config.QueueReadTimeoutMs, out ea) && msgCount < messagesToRecv)
                        {
                            var qMsg = new QueueMessage
                            {
                                MessageProperties = ea.BasicProperties,
                                RawMessage = ea.Body
                            };
                            deliveryArgs.Add(ea.DeliveryTag, qMsg);
                            msgCount += 1;
                        }

                        Log.Debug("Handling Messages");
                        var results = handler(deliveryArgs);

                        foreach (var result in results)
                        {
                            var initialMsg = deliveryArgs[result.Key];
                            if (result.Value.Success)
                            {
                                if (initialMsg.MessageProperties.ReplyToAddress != null || result.Value.ReplyToProperties.ReplyToAddress != null)
                                {
                                    var rta = result.Value.ReplyToProperties.ReplyToAddress ??
                                              initialMsg.MessageProperties.ReplyToAddress;
                                    channel.BasicPublish(rta.ExchangeName, rta.RoutingKey, 
                                        false, result.Value.ReplyToProperties, Encoding.UTF8.GetBytes(result.Value.ReplyBody));
                                }
                                channel.BasicAck(result.Key, false);
                                Log.Debug("{MessageId} Message Acknowledged", initialMsg.MessageProperties.MessageId);
                            }
                            else
                            {
                                channel.BasicNack(result.Key, false, config.RequeueOnFailure);
                                Log.Debug("{MessageId} Message Negative Acknowledged.  Retry: {Reque}", initialMsg.MessageProperties.MessageId, config.RequeueOnFailure);
                            }
                        }

                        if (results.Any(x => x.Value.Cancel))
                        {
                            Log.Information("Canceling queue handling"); ;
                            cancel = true;
                        }
                    }
                }
            }
        }
        public void HandleMessages<T>(Func<T, bool> handler)
        {
             var factory = new ConnectionFactory
            {
                HostName = _connStr.Host,
                Port = _connStr.Port,
                UserName = _connStr.Username,
                Password = _connStr.Password
            };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicQos(0,1,false);
                    var queueName = this.GetInQueueName(typeof (T));
                    channel.BasicConsume(queueName, false, consumer);
                    Log.Debug("Consuming Queue: {Queue}", queueName);

                    while (true)
                    {
                        var ea = consumer.Queue.Dequeue();

                        var message = _serializer.Deserialize<T>(Encoding.UTF8.GetString(ea.Body));
                        Log.Debug("Message received and deserialized");
                        var result = handler(message);

                        if (result)
                        {
                            Log.Debug("Message acknowledged");
                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                        else
                        {
                            Log.Debug("Message negative acknowledge with reque: {RequeOnFailure}", RequeueOnFailure);
                            channel.BasicNack(ea.DeliveryTag, false, RequeueOnFailure);
                        }
                    }
                }
            }

        }

        public int GetQueueCount<T>()
        {
            var factory = new ConnectionFactory
            {
                HostName = _connStr.Host,
                Port = _connStr.Port,
                UserName = _connStr.Username,
                Password = _connStr.Password
            };


            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var bg = channel.BasicGet(this.GetInQueueName(typeof(T)), false);
                    var count = (bg != null) ? bg.MessageCount : 0;
                    return (int)count;
                }
            }
        }

        public void HandleMessage<T>(Func<T,bool> handler, out bool receivedMsg) 
        {
            var factory = new ConnectionFactory
            {
                HostName = _connStr.Host,
                Port = _connStr.Port,
                UserName = _connStr.Username,
                Password = _connStr.Password
            };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var consumer = new QueueingBasicConsumer(channel);

                    var queueName = this.GetInQueueName(typeof (T));
                    channel.BasicQos(0,1,false);
                    channel.BasicConsume(queueName, false, consumer);

                    Log.Debug("Consuming Queue: {Queue}", queueName);
                    var ea = default(BasicDeliverEventArgs);

                    var messageReceived = consumer.Queue.Dequeue((int)Timeout.TotalMilliseconds, out ea);

                    if (!messageReceived)
                    {
                        Log.Debug("No message received after timeout of {Timeout}", Timeout);
                        receivedMsg = false;
                        return;
                    }
                    receivedMsg = true;


                    var message = _serializer.Deserialize<T>(Encoding.UTF8.GetString(ea.Body));
                    Log.Debug("Message received and deserialized");
                    var result = handler(message);

                    if (result)
                    {
                        Log.Debug("Message acknowledged");
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                    else
                    {
                        Log.Debug("Message negative acknowledge with reque: {RequeueOnFailure}", RequeueOnFailure);
                        channel.BasicNack(ea.DeliveryTag, false, RequeueOnFailure);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new queue, if the queue already exists there is no effect
        /// </summary>
        /// <param name="queueName">The name of the queue</param>
        /// <param name="durable">Should the queue survive a server restart?</param>
        /// <param name="autoDelete">Should the queue be deleted when every consumer is finished with it (has no effect to existing queues)</param>
        /// <param name="exchangeToBind">If this isn't null the queue will be bound to this exchange</param>
        /// <param name="routingKeyToBind">The routing key to bind to if not empty</param>
        public void DeclareQueue(string queueName, bool durable = true, bool autoDelete = false, string exchangeToBind = null, string routingKeyToBind = "")
        {
            var factory = new ConnectionFactory
            {
                HostName = _connStr.Host,
                Port = _connStr.Port,
                UserName = _connStr.Username,
                Password = _connStr.Password
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queueName, durable, exclusive: false, autoDelete: autoDelete, arguments: null);
                Log.Debug("Declared Queue {QueueName}, Auto Delete: {AutoDelete}",queueName, autoDelete);
                if (exchangeToBind != null)
                {
                    channel.QueueBind(queueName, exchangeToBind, routingKeyToBind);
                    Log.Debug("Bound Queue {QueueName} to {ExchangeAddress}", queueName, new PublicationAddress(null, exchangeToBind, routingKeyToBind).ToString());
                }
            }
        }


        public void Publish(byte[] message,
            string contentType = "application/json",
            string messageId = "",
            string type = "",
            bool persistant = true,
            string exchange = "",
            string routingKey = "",
            string replyTo = "",
            Dictionary<string, object> headers = null)
        {
            Publish(new List<byte[]> { message}, contentType, messageId, type, persistant,exchange, routingKey, replyTo, headers);
        }
        public void Publish(string message,
            string contentType = "application/json",
            string messageId = "",
            string type = "",
            bool persistant = true,
            string exchange = "",
            string routingKey = "",
            string replyTo = "",
            Dictionary<string, object> headers = null)
        {
            Publish(new List<byte[]> { Encoding.UTF8.GetBytes(message)}, contentType, messageId, type, persistant,exchange, routingKey, replyTo, headers);
        }
        public void Publish(List<string> messages,
            string contentType = "application/json",
            string messageId = "",
            string type = "",
            bool persistant = true,
            string exchange = "",
            string routingKey = "",
            string replyTo = "",
            Dictionary<string, object> headers = null)
        {
            Publish(messages.Select(x => Encoding.UTF8.GetBytes(x)).ToList(), contentType, messageId, type, persistant,exchange, routingKey, replyTo, headers);
        }
        public void Publish(List<byte[]> messages, 
            string contentType = "application/json", 
            string messageId = "",
            string type = "", 
            bool persistant = true, 
            string exchange = "", 
            string routingKey = "", 
            string replyTo = "",
            Dictionary<string, object> headers = null)
        {
             var factory = new ConnectionFactory
            {
                HostName = _connStr.Host,
                Port = _connStr.Port,
                UserName = _connStr.Username,
                Password = _connStr.Password
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var basicProps = channel.CreateBasicProperties();
                basicProps.ContentType = contentType;
                basicProps.MessageId = messageId;
                basicProps.SetPersistent(persistant);
                basicProps.Type = type;
                basicProps.ReplyTo = replyTo;
                basicProps.Headers = headers;

                foreach (var msg in messages)
                {
                    basicProps.Timestamp = new AmqpTimestamp(DateTime.UtcNow.ToUnixTimestamp());
                    channel.BasicPublish(exchange,routingKey, basicProps, msg);
                }
            }
        }
        public void PublishMessages<T>(List<T> messages)
        {
            var queueName = this.GetInQueueName(typeof (T));
            var factory = new ConnectionFactory
            {
                HostName = _connStr.Host,
                Port = _connStr.Port,
                UserName = _connStr.Username,
                Password = _connStr.Password
            };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var basicProperties = channel.CreateBasicProperties();
                    basicProperties.ContentType = "application/json";
                    basicProperties.MessageId = Guid.NewGuid().ToString("D");
                    basicProperties.SetPersistent(true);
                    channel.QueueDeclare(this.GetInQueueName(typeof (T)), true, false, false, null);

                    var msgs = messages.Select(msg => _serializer.Serialize(msg))
                        .Select(body => Encoding.UTF8.GetBytes(body));

                    foreach (var messageBody in msgs)
                    {
                        basicProperties.Timestamp = new AmqpTimestamp(DateTime.UtcNow.ToUnixTimestamp());
                        channel.BasicPublish("", queueName, basicProperties, messageBody);
                    }

                    Log.Debug("{MessageCount} Message(s) published to queue {QueueName}", messages.Count, queueName);
                }
            }

        }
        public void PublishMessage<T>(T message)
        {
            PublishMessages(new[]{message}.ToList());
        }
    }
}
