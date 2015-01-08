using System.Collections.Generic;
using Chronos.Interfaces;
using ServiceStack;

namespace Chronos
{
    public class ServiceStackSerializer : ISerializer
    {
        public T Deserialize<T>(string s)
        {
            return s.FromJson<T>();
        }

        public string Serialize<T>(T obj)
        {
            return obj.ToJson();
        }
    }
}