using System;
using System.IO;
using System.Messaging;
using System.Text;
using ServiceStack;
using ServiceStack.Text;

namespace Chronos
{
    public class JsonMessageFormatter<T> : IMessageFormatter
    {
        static JsonMessageFormatter()
        {
            JsConfig.AssumeUtc = true;
            JsConfig.AlwaysUseUtc = true;
        }
        public object Clone()
        {
            return new JsonMessageFormatter<T>();
        }

        public bool CanRead(Message message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var stream = message.BodyStream;

            return stream != null && stream.CanRead && stream.Length > 0;
        }

        public object Read(Message message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            if (CanRead(message) == false)
                return null;

            using (var reader = new StreamReader(message.BodyStream, Encoding.UTF8))
            {
                var json = reader.ReadToEnd();
                return json.FromJson<T>();
            }
        }

        public void Write(Message message, object obj)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            if (obj == null)
                throw new ArgumentNullException("obj");

            var json = obj.ToJson();

            message.BodyStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            //need to rese the body type in case the same message is reused by some other formatter
            message.BodyType = 0;
        }

    }
}