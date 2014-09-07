﻿using System;
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
                    var bg = channel.BasicGet(this.GetInQueueName(typeof(T)), true);
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

        public void PublishMessage<T>(T message)
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
                    var basicProperties = channel.CreateBasicProperties();
                    basicProperties.ContentType = "application/json";
                    basicProperties.MessageId = Guid.NewGuid().ToString("D");
                    basicProperties.Timestamp = new AmqpTimestamp(DateTime.UtcNow.ToUnixTimestamp());
                    channel.QueueDeclare(this.GetInQueueName(typeof (T)), true, false, false, null);

                    var serializedMessage = _serializer.Serialize(message);
                    var body = Encoding.UTF8.GetBytes(serializedMessage);

                    channel.BasicPublish("", this.GetInQueueName(typeof(T)), basicProperties, body);
                    Log.DebugFormat("Message published to queue {0}", this.GetInQueueName(typeof(T)));
                }
            }
        }
    }
}
