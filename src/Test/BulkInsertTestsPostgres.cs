using System;
using System.Collections.Generic;
using Chronos;
using Xunit;

namespace Test
{
    public class BulkInsertTestsPostgres
    {
        public class Db
        {
            public Guid DatabaseId { get; set; }
            public string Name { get; set; }
        }
        [Fact (Skip="No Postgres server")]
        public void CanBulkInsertIntoPostgres()
        {
            var mappings = new BulkInsertColumnMappings<Db>().MapColumnsAsLowercaseUnderscore();
            Chronos.PostgreSQL.PostgresBulkInserter<Db> bcp = new Chronos.PostgreSQL.PostgresBulkInserter<Db>("Server=127.0.0.1;Port=5432;Database=jinx;User Id=postgres;Password=postgres", mappings);
            var itemsToInsert = new List<Db>
            {
                new Db {DatabaseId = Guid.NewGuid(), Name = "Random1"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "Random2"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "Random4"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "Random3"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "Random5"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "Random6"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "Random7"},
            };

            bcp.Insert(itemsToInsert, "databases");
        }

        [Fact(Skip="No postgres server")]
        public void CanBulkInsertToPostgresNonGenericly()
        {
            var mappings = new BulkInsertColumnMappings(typeof(Db)).MapColumnsAsLowercaseUnderscore();
            var bcp = new Chronos.PostgreSQL.PostgresBulkInserter("Server=127.0.0.1;Port=5432;Database=jinx;User Id=postgres;Password=postgres", typeof(Db),mappings);
            var itemsToInsert = new List<Db>
            {
                new Db {DatabaseId = Guid.NewGuid(), Name = "NonGeneric"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "RNonGenericandom2"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "RNonGenericandom4"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "RNonGenericandom3"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "NonGenericRandom5"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "RNonGenericandom6"},
                new Db {DatabaseId = Guid.NewGuid(), Name = "RNonGenericandom7"},
            };

            bcp.Insert(itemsToInsert, "databases");
        }
    }
}