using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Chronos.Interfaces;

namespace Chronos.AWS
{
    public class Dynamo
    {
        private readonly ISerializer _serializer;
        private readonly AWSCredentials _credentials;


        private readonly AmazonDynamoDBClient _client; 
        public Dynamo(AWSCredentials credentials,RegionEndpoint region, ISerializer serializer = null)
        {
            _serializer = serializer ?? new ServiceStackSerializer();
            _credentials = credentials;
            _client = new AmazonDynamoDBClient(credentials, region);
        }

        /// <summary>
        /// Gets a single record from dynamodb returned as json
        /// </summary>
        /// <param name="tableName">The dynamodb table name</param>
        /// <param name="indexName">The index to use</param>
        /// <param name="keyName">The 'column' to equal compare against</param>
        /// <param name="keyValueAsString">The value to compare againt (as a string)</param>
        /// <returns>Single json object</returns>
        public string Get(string tableName, string indexName,string keyName, string keyValueAsString)
        {
            var request = new QueryRequest
            {
                TableName= tableName,
                KeyConditions = new Dictionary<string, Condition>
                {
                    {
                       keyName,
                       new Condition
                       {
                           ComparisonOperator = "EQ",
                           AttributeValueList = new List<AttributeValue>
                           {
                               new AttributeValue { S = keyValueAsString}
                           }
                       }
                    }
                },
                IndexName = indexName
            };

            var resp = _client.Query(request);
            var item = resp.Items.FirstOrDefault();
            if (item != null)
            {
                var dict = item.ToDictionary(x => x.Key, x => GetValueFromAttribute(x.Value));
                return _serializer.Serialize(dict);               
            }
            return string.Empty;
        }

        public string Get(string tableName, string keyName, string keyValue)
        {
            var request = new QueryRequest
            {
                TableName = tableName,
                KeyConditions = new Dictionary<string, Condition>
                {
                    {
                       keyName,
                       new Condition
                       {
                           ComparisonOperator = "EQ",
                           AttributeValueList = new List<AttributeValue>
                           {
                               new AttributeValue { S = keyValue}
                           }
                       }
                    }
                },
            };

            var resp = _client.Query(request); 
            var item = resp.Items.FirstOrDefault();
            if (item != null)
            {
                var dict = item.ToDictionary(x => x.Key, x => GetValueFromAttribute(x.Value));
                return _serializer.Serialize(dict);               
            }
            return string.Empty;
        }

        public void WriteSingle(string tableName, Dictionary<string,object> dictionary)
        {
            var transformDictionary = dictionary.ToDictionary(x => x.Key, x => GetAttributeValueFromObject(x.Value));
            _client.PutItem(tableName, transformDictionary);
        }

        public void WriteMany(Dictionary<string,List<Dictionary<string, object>>> items)
        {
            var d = new Dictionary<string, List<WriteRequest>>();

            foreach (var kvp in items)
            {
                var tableName = kvp.Key;
                var requests = kvp.Value.Select(x => x
                    .ToDictionary(
                        k => k.Key,
                        k => GetAttributeValueFromObject(k.Value))
                    )
                    .Select(x => new WriteRequest(new PutRequest(x)))
                    .ToList();
                d.Add(tableName,requests);
            }

            var res = _client.BatchWriteItem(d);
        }

        public void WriteMany(string table, List<Dictionary<string, object>> reqs, int writeThrougput)
        {
            var d = reqs.Select(
                x => new WriteRequest(new PutRequest(
                    x.ToDictionary(
                        k => k.Key,
                        k => GetAttributeValueFromObject(k.Value)))
                    )
                )
                .ToList();

            var count = 0;

            while (count < d.Count)
            {
                var items = d.Skip(count).Take(writeThrougput).ToList();
                _client.BatchWriteItem(new Dictionary<string, List<WriteRequest>>
                {
                    {table, items}
                });
                count += items.Count;
            }
        }


        private Type[] _numberTypes = new[]
        {
            typeof (int), typeof (float), typeof (decimal), typeof (long)
        };
        private AttributeValue GetAttributeValueFromObject(object obj)
        {
            if (obj == null)
            {
                return new AttributeValue { NULL = true};
            }
            var t = obj.GetType();
            var isNumber = _numberTypes.Contains(obj);
            if (isNumber)
                return new AttributeValue() { N = obj.ToString() };
            else if (t == typeof (string))
            {
                return new AttributeValue((string)obj);
            }
            else if (t == typeof (bool))
            {
                return new AttributeValue { BOOL = (bool)obj};
            }
            else if (t.GetInterfaces().Contains(typeof (IEnumerable<string>)))
            {
                return new AttributeValue(((IEnumerable<string>)obj).ToList());
            }
            else if (t.GetInterfaces().Contains(typeof(IEnumerable)))
            {
                return new AttributeValue(_serializer.ParseAsString(obj));
            }
            else return new AttributeValue(_serializer.ParseAsString(obj));
        }
        private object GetValueFromAttribute(AttributeValue value)
        {
            if (value.N != null)
            {
                int iValue = 0;
                decimal dValue = 0;
                if (int.TryParse(value.N, out iValue))
                {
                    return iValue;
                }
                if (decimal.TryParse(value.N, out dValue))
                {
                    return dValue;
                }
                return value.N;
            }
            if (value.S != null)
                return value.S;
            return null;
        }
    }
}