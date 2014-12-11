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
        public string GetSingle(string tableName, string indexName,string keyName, string keyValueAsString)
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
            var dict = resp.Items[0].ToDictionary(x => x.Key, x => GetValueFromAttribute(x.Value));
            return _serializer.Serialize(dict);
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