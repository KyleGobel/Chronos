using System.Collections.Generic;
using System.Data;
using System.Linq;
using Chronos.Interfaces;
using Chronos.SqlMetadata;

namespace Chronos.Templates
{
    public class Engine
    {
        private readonly ITemplateEngine _templateEngine;
        public Dictionary<DbType, string> DbTypeDictionary; 

        public Engine(ITemplateEngine templateEngine)
        {
            _templateEngine = templateEngine;
            DbTypeDictionary = new Dictionary<DbType, string>
            {
            };
        }

        public static string GetTemplateForStoredProcedure(IDbConnection connection ,string sprocName)
        {
            var p = connection.GetStoredProcedureParams(sprocName);
            var dict = p.ToDictionary(x => x.ParameterName, x => x.DbType);

            foreach (var kvp in dict)
            {
                switch (kvp.Value)
                {

                }
            }
            return string.Empty;
        }
    }
}