using System;
using System.Collections;
using System.Collections.Generic;

namespace Chronos.Interfaces
{
    public interface IBulkInserter<T> where T : class
    {
        BulkInsertColumnMappings<T> ColumnMappings { get; set; } 
        void Insert(List<T> items, string tableName, Action<long> notifyRowsCopied, Action<Exception> onError);
    }

    public interface IBulkInserter
    {
        BulkInsertColumnMappings ColumnMappings { get; set; }
        void Insert(IEnumerable items, string tableName, Action<long> notifyRowsCopied, Action<Exception> onError);
    }
}