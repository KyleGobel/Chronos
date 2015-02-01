using System;

namespace Chronos.ProtoBuffers
{
    public class CacheWrapper
    {
        [Order(1)]
        public DateTime CacheDate { get; set; } 

        [Order(3)]
        public string CacheKey { get; set; }

        [Order(4)]
        public byte[] ProtoBuffObject { get; set; }
    }

    public static class CacheExtensions
    {
        public static byte[] ToProtoBufCacheObj<T>(this T obj, string cachePrefix = null)
        {
            var bytes = obj.ToProtoBufByteArray();

            var cacheKey = (cachePrefix ?? "") + Encrypt.Sha1(bytes);

            var cacheObj = new CacheWrapper
            {
                CacheDate = DateTime.UtcNow,
                CacheKey = cacheKey,
                ProtoBuffObject = bytes
            };

            return cacheObj.ToProtoBufByteArray();
        }

        public static CacheWrapper FromCacheWrapper(this byte[] bytes)
        {
            return bytes.FromProtoBufByteArray<CacheWrapper>();
        }

        public static T FromCache<T>(this byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return default(T);
            var cacheWrapper = bytes.FromCacheWrapper();
            if (cacheWrapper == null)
            {
                return default(T);
            }
            return cacheWrapper.ProtoBuffObject.FromProtoBufByteArray<T>();
        }
    }
}