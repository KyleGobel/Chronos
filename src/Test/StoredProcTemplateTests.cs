using System;
using System.Data.SqlClient;
using System.Reflection;
using Chronos;
using Chronos.Configuration;
using Chronos.Templates;
using Humanizer;
using Xunit;

namespace Test
{
    public class StoredProcTemplateTests
    {
        [Fact]
        public void CanMakeTemplate()
        {

            var hey = "StartDate".Humanize();
            var cstring = ConfigUtilities.GetConnectionStringFromNameOrConnectionString("TestDb");
            var html = default(string);
            var eng = new Engine(new TempEngine());
            using (var connection = new SqlConnection(cstring))
            {
                connection.Open();
                html = eng.GetTemplateForStoredProcedure(connection, "GetDaily");
            }
            Assert.NotNull(html);
        }
    }
}