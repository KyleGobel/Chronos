﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Chronos;
using Chronos.ProtoBuffers;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var requestItem = new TestDto
            {
                DateField1 = DateTime.Now
            };

            var testItem = new TestDto
            {
                DateField1 = DateTime.Now,
                Field1 = GenerateRandom.String(25),
                Field2 = GenerateRandom.String(50),
                Field3 = GenerateRandom.String(100),
                Field4 = GenerateRandom.Int()
            };
            var proto = requestItem.ToProtoBufCacheObj(testItem);
            var json = requestItem.ToJsonCacheObj(testItem);

            Enumerable.Range(1, 3).ForEach(x =>
            {
                var iterations = 50000*x;  
                Console.WriteLine("-----------------------------------------------");
                Console.WriteLine("----------Running for {0} iterations--------", iterations);
                Console.WriteLine("-----------------------------------------------");
                Profiling.Profile("ProtoBuf Cache Serialization Only", iterations, () => requestItem.ToProtoBufCacheObj(testItem));
                Profiling.Profile("ProtoBuf Deserialization Only", iterations, () => proto.FromCache<TestDto>());
                Profiling.Profile("ProtoBuf Cache Serialize/Deserialize", iterations, () => requestItem
                    .ToProtoBufCacheObj(testItem)
                    .FromCache<TestDto>());
                Console.WriteLine("-----------------------------------------------");
                Profiling.Profile("Json Cache Serialization Only", iterations, () => requestItem.ToJsonCacheObj(testItem));
                Profiling.Profile("Json Cache Deserialization Only", iterations, () => json.FromCache<TestDto>());
                Profiling.Profile("Json Cache Serialize/Deserialize", iterations, () => requestItem
                    .ToJsonCacheObj(testItem)
                    .FromCache<TestDto>());

            });
            Console.ReadLine();

        }
    }

    public class TestDto
    {
        [Order(1)]
        public string Field1 { get; set; }
        [Order(2)]
        public string Field2 { get; set; }
        [Order(3)]
        public string Field3 { get; set; }
        [Order(4)]
        public DateTime DateField1 { get; set; }
        [Order(5)]
        public int Field4 { get; set; }
    }
}
