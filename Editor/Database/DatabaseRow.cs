using System;

namespace BorritEditor.Database
{
    public readonly struct DatabaseRow : IEquatable<DatabaseRow>
    {
        public static readonly DatabaseRow Empty = new DatabaseRow(string.Empty, string.Empty, 0);
        
        private readonly string _borrowedAssetGuid;
        private readonly string _borrowerName;
        private readonly long _borrowBinaryUtcDateTime;

        public string BorrowedAssetGuid => _borrowedAssetGuid;
        public string BorrowerName => _borrowerName;
        public long BorrowBinaryUtcDateTime => _borrowBinaryUtcDateTime;
        public bool IsEmpty => Equals(Empty);

        public DatabaseRow(string borrowedAssetGuid, string borrowerName, long borrowBinaryUtcDateTime)
        {
            _borrowedAssetGuid = borrowedAssetGuid;
            _borrowerName = borrowerName;
            _borrowBinaryUtcDateTime = borrowBinaryUtcDateTime;
        }

        public bool Equals(DatabaseRow other)
        {
            return _borrowedAssetGuid == other._borrowedAssetGuid;
        }

        public override bool Equals(object obj)
        {
            return obj is DatabaseRow other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _borrowedAssetGuid.GetHashCode();
        }

        public static bool operator ==(DatabaseRow left, DatabaseRow right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DatabaseRow left, DatabaseRow right)
        {
            return !left.Equals(right);
        }
    }
}