using System.Collections.Generic;

namespace BorritEditor.Database
{
    public readonly partial struct DatabaseRow
    {
        public DatabaseRow(IList<object> row)
        {
            _borrowedAssetGuid = row[DatabaseColumn.BorrowedAssetIndex] as string;
            _borrowerName = row[DatabaseColumn.BorrowerNameIndex] as string;
            long.TryParse(row[DatabaseColumn.BorrowBinaryUtcDateTimeIndex] as string, out _borrowBinaryUtcDateTime);
        }
    }
}