using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Chronos.ProtoBuffers
{
    public class ChronosProtoBuf
    {
        private static readonly HashSet<Type> AddedTypes = new HashSet<Type>(); 
        /// <summary>
        /// Sets up a type to be ready for proto buff use (looks for the order attribute)
        /// </summary>
        /// <param name="type">The type to setup to serialize/deserialize</param>
        /// <param name="overwrite">Normally the results are cached, this will overwrite the cache</param>
        public static void SetupType(Type type, bool overwrite = false)
        {
            if (overwrite || !AddedTypes.Contains(type))
            {
                RuntimeTypeModel.Default.Add(type, false);
                var props = TsvFormatter.GetSerializablePropertiesInOrder(type);
                props.Aggregate(1, (count, memberName) =>
                {
                    RuntimeTypeModel.Default[type].AddField(count, memberName);
                    return count + 1;
                });

                AddedTypes.Add(type);
            }

        }
    }
    public static class ProtoBufferExtensions
    {
        /// <summary>
        /// To a proto buffers byte array
        /// </summary>
        public static byte[] ToProtoBufByteArray<T>(this T obj)
        {
            ChronosProtoBuf.SetupType(typeof(T));
           
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, obj);
                var bytes = ms.ToArray();
                return bytes;
            }
        }

        public static T FromProtoBufByteArray<T>(this byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return default(T);
            ChronosProtoBuf.SetupType(typeof(T));
            using (var ms = new MemoryStream(bytes))
            {
                var obj = (T)Serializer.Deserialize<T>(ms);
                return obj;
            }
        }

        public static void ToProtoBufFile<T>(this T obj, string path)
        {
            ChronosProtoBuf.SetupType(typeof(T));
            using (var file = File.Create(path))
                Serializer.Serialize(file, obj);
        }

        public static T FromProtoBufFile<T>(this string path) 
        {
            ChronosProtoBuf.SetupType(typeof(T));
            using (var file = File.OpenRead(path))
            {
               var obj = (T)Serializer.Deserialize<T>(file);
                return obj;
            }
        }
         
    }
}