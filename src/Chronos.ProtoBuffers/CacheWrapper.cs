using System;
using Chronos.Interfaces;

namespace Chronos.ProtoBuffers
{
    public class ProtoBufCacheWrapper
    {
        [Order(1)]
        public DateTime CacheDate { get; set; } 

        [Order(3)]
        public string CacheKey { get; set; }

        [Order(4)]
        public byte[] ProtoBuffObject { get; set; }
    }

    public class JsonCacheWrapper
    {
        [Order(1)]
        public DateTime CacheDate { get; set; } 

        [Order(3)]
        public string CacheKey { get; set; }

        [Order(4)]
        public string JsonObject { get; set; }
    }

    public static class CacheExtensions
    {
        public static string ToCacheKey<TRequestObj>(this TRequestObj obj, string cachePrefix = null)
        {
            var bytes = obj.ToProtoBufByteArray();
            return (cachePrefix ?? "") + Encrypt.Sha1(bytes);
        }
        public static byte[] ToProtoBufCacheObj<TKeyObj, TCacheObj>(this TKeyObj keyObj, TCacheObj cacheObj, string cachePrefix = null)
        {
            var key = ToCacheKey(keyObj, cachePrefix);
            var bytes = cacheObj.ToProtoBufByteArray();

            var res = new ProtoBufCacheWrapper
            {
                CacheDate = DateTime.UtcNow,
                CacheKey = key,
                ProtoBuffObject = bytes
            };

            return res.ToProtoBufByteArray();
        }

        public static string ToJsonCacheObj<TKeyObj, TCacheObj>(this TKeyObj keyObj,TCacheObj cacheObj, ISerializer serializer = null, string cachePrefix = null)
        {
            serializer = serializer ?? new ServiceStackSerializer();
            var json = serializer.Serialize(cacheObj);

            var key = ToCacheKey(keyObj, cachePrefix);

            var res = new JsonCacheWrapper
            {
                CacheDate = DateTime.UtcNow,
                CacheKey = key,
                JsonObject = json
            };

            return serializer.Serialize(res);
        }

        public static ProtoBufCacheWrapper FromCacheWrapper(this byte[] cacheObj)
        {
            return cacheObj.FromProtoBufByteArray<ProtoBufCacheWrapper>();
        }

        public static JsonCacheWrapper FromCacheWrapper(this string cacheObj, ISerializer deserializer = null)
        {
            deserializer = deserializer ?? new ServiceStackSerializer();
            return deserializer.Deserialize<JsonCacheWrapper>(cacheObj);
        }

        public static T FromCache<T>(this byte[] cacheObj)
        {
            if (cacheObj == null || cacheObj.Length == 0)
                return default(T);
            var cacheWrapper = cacheObj.FromCacheWrapper();
            if (cacheWrapper == null)
            {
                return default(T);
            }
            return cacheWrapper.ProtoBuffObject.FromProtoBufByteArray<T>();
        }

        public static T FromCache<T>(this string cacheObj, ISerializer serializer = null)
        {
            serializer = serializer ?? new ServiceStackSerializer();
            if (string.IsNullOrEmpty(cacheObj))
                return default(T);
            var cacheWrapper = cacheObj.FromCacheWrapper();
            if (cacheWrapper == null)
            {
                return default(T);
            }
            return serializer.Deserialize<T>(cacheWrapper.JsonObject);
        }
    }
}