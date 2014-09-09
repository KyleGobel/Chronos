using Chronos;
using Chronos.Configuration;
using Xunit;

namespace Test
{
    public class SqlQueriesTest
    {

        [Fact]
        public void CanLoadQueryByRequestDto()
        {
            const string expectedOutput = @"select test_column from test_database..test_table";

            var testQuery = new TestQuery();
            var sql = testQuery.GetSqlQuery();

            Assert.Equal(expectedOutput, sql);
        }

        [Fact]
        public void CanLoadQueryByNameWithoutExt()
        {
            const string expectedOutput = @"select test_column from test_database..test_table";

            var sql = EmbeddedSql.GetSqlQuery("testquery");

            Assert.Equal(expectedOutput, sql);
        }

        [Fact]
        public void CanLoadQueryByNameWithExt()
        {
            const string expectedOutput = @"select test_column from test_database..test_table";

            var sql = EmbeddedSql.GetSqlQuery("testquery.sql");

            Assert.Equal(expectedOutput, sql);
        }
    }

    public class TestQuery
    {
        
    }
}