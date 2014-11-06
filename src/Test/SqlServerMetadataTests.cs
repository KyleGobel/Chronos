using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Chronos.SqlMetadata;
using Xunit;

namespace Test
{
    
    public class SqlServerMetadataTests : IDisposable
    {
        private SqlConnection connection;

        public SqlServerMetadataTests()
        {
            connection = new SqlConnection("Server=localhost;Database=Main;Trusted_Connection=True;");
            connection.Open();
        }
        

        [Fact(Skip="No sql server", DisplayName = "Can get parameters from a stored procedure")]
        public void CanGetStoredProcedureParams()
        {
            var parameters = connection.GetStoredProcedureParams("IMS.usp_sync_adjustments");

            Assert.True(parameters.Any(x => x.ParameterName == "@FulfillmentCenter_ID"));
        }

        [Fact(Skip = "No sql server", DisplayName = "Can get the tables from sql server")]
        public void CanGetTables()
        {
            var metadata = connection.GetTables();

            Assert.NotEmpty(metadata);
        }

        [Fact(Skip = "No sql server", DisplayName = "Can get types from a sql query")]
        public void CanGetTypesFromSqlQuery()
        {
            var typeDictionary = connection.GetColumnTypesFromQuery("select id, reportName from Amazon.Reports");

            Assert.Equal(2,typeDictionary.Count);
            Assert.True(typeDictionary.ContainsKey("id"));
            Assert.True(typeDictionary.ContainsKey("reportName"));
            Assert.Equal(typeof(int),typeDictionary["id"]);
            Assert.Equal(typeof(string), typeDictionary["reportName"]);
        }

        [Fact(Skip = "No sql server", DisplayName = "Can test whether column is nullable")]
        public void CanCheckIfColumnIsNullable()
        {
            Assert.True(connection.IsColumnNullable("Amazon.Reports", "packageName") ?? false);
            Assert.False(connection.IsColumnNullable("Amazon.Reports", "downloadDate") ?? true);
            Assert.Null(connection.IsColumnNullable("Amazon.Reports", "thisColumnDoesntExist"));
        }
        public void Dispose()
        {
            connection.Close();
        }
    }
}