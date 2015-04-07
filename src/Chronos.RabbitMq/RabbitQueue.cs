using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chronos.Configuration;
using Chronos.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServiceStack.Logging;

namespace Chronos.RabbitMq
{
    public class RabbitQueue : IMessageQueue
    {
        public static TimeSpan Timeout = new TimeSpan(0, 0, 0, 3);
  
        private readonly RabbitMqConnectionString _connStr;
        private readonly ISerializer _serializer;
        private static readonly ILog Log = LogManager.GetLogger(typeof(RabbitQueue));

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
        public void HandleQueue(HandleQueueConfig config, Func<string, HandleQueueResult> handler)
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
                    channel.BasicQos(0, 1, false);

                    channel.BasicConsume(config.QueueName, false, consumer);
                    bool cancel = false;

                    while (!cancel)
                    {
                        var ea = default(BasicDeliverEventArgs);
                        if (consumer.Queue.Dequeue(config.QueueReadTimeoutMs, out ea))
                        {
                            var json = Encoding.UTF8.GetString(ea.Body);

                            var result = handler(json);

                            if (result.Cancel)
                            {
                                cancel = true;
                            }

                            if (result.Success)
                            {
                                if (config.Reply)
                                {
                                    channel.BasicPublish(config.ReplyToExchangeName, config.ReplyToRouteKey, false, ea.BasicProperties, Encoding.UTF8.GetBytes(result.ReplyBody));
                                }
                                channel.BasicAck(ea.DeliveryTag, false);
                            }
                            else
                            {
                                channel.BasicNack(ea.DeliveryTag, false, config.RequeueOnFailure);
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
        public void HandleQueueMultiple(HandleQueueConfig config, int messagesToRecv, Func<Dictionary<ulong, string>,Dictionary<ulong, HandleQueueResult>> handler)
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
                    channel.BasicQos(0, 1, false);

                    channel.BasicConsume(config.QueueName, false, consumer);
                    bool cancel = false;

                    while (!cancel)
                    {
                        var ea = default(BasicDeliverEventArgs);

                        int msgCount = 0;

                        var deliveryArgs = new Dictionary<ulong, string>();
                        while (consumer.Queue.Dequeue(config.QueueReadTimeoutMs, out ea) && msgCount < messagesToRecv)
                        {
                            deliveryArgs.Add(ea.DeliveryTag, Encoding.UTF8.GetString(ea.Body));
                            msgCount += 1;
                        }

                        var results = handler(deliveryArgs);

                        foreach (var result in results)
                        {
                            if (result.Value.Success)
                            {
                                if (config.Reply)
                                {
                                    channel.BasicPublish(config.ReplyToExchangeName, config.ReplyToRouteKey, false, ea.BasicProperties, Encoding.UTF8.GetBytes(result.Value.ReplyBody));
                                }
                                channel.BasicAck(result.Key, false);
                            }
                            else
                            {
                                channel.BasicNack(result.Key, false, config.RequeueOnFailure);
                            }
                        }

                        if (results.Any(x => x.Value.Cancel))
                        {
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
                    Log.DebugFormat("Consuming Queue: {0}", queueName);

                    while (true)
                    {
                        var ea = consumer.Queue.Dequeue();

                        var message = _serializer.Deserialize<T>(Encoding.UTF8.GetString(ea.Body));
                        Log.DebugFormat("Message received and deserialized");
                        var result = handler(message);

                        if (result)
                        {
                            Log.DebugFormat("Message acknowledged");
                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                        else
                        {
                            Log.DebugFormat("Message negative acknowledge with reque: {0}", RequeueOnFailure);
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

                    Log.DebugFormat("Consuming Queue: {0}", queueName);
                    var ea = default(BasicDeliverEventArgs);

                    var messageReceived = consumer.Queue.Dequeue((int)Timeout.TotalMilliseconds, out ea);

                    if (!messageReceived)
                    {
                        Log.DebugFormat("No message received after timeout of {0}", Timeout);
                        receivedMsg = false;
                        return;
                    }
                    receivedMsg = true;


                    var message = _serializer.Deserialize<T>(Encoding.UTF8.GetString(ea.Body));
                    Log.DebugFormat("Message received and deserialized");
                    var result = handler(message);

                    if (result)
                    {
                        Log.DebugFormat("Message acknowledged");
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                    else
                    {
                        Log.DebugFormat("Message negative acknowledge with reque: {0}", RequeueOnFailure);
                        channel.BasicNack(ea.DeliveryTag, false, RequeueOnFailure);
                    }
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

                    Log.DebugFormat("{0} Message(s) published to queue {1}", messages.Count, queueName);
                }
            }

        }
        public void PublishMessage<T>(T message)
        {
            PublishMessages(new[]{message}.ToList());
        }
    }
}
