using System;
using System.Collections.Generic;
using UnityEngine;

namespace BorritEditor.Database
{
    public class InternalDataBase
    {
        [Serializable]
        public class EntryDTO
        {
            public string Guid;
            public string User;
            public long UtcDateTime;
        }
        
        private List<DatabaseRow> _data = new List<DatabaseRow>();
        private List<EntryDTO> _pendingBorrow = new List<EntryDTO>();
        private List<string> _pendingReturn = new List<string>();

        public bool Dirty => _pendingBorrow.Count > 0 || _pendingReturn.Count > 0;

        public void Clear()
        {
            _data.Clear();
            _pendingBorrow.Clear();
            _pendingReturn.Clear();
        }
        
        public void Return(List<string> update)
        {
            _pendingReturn.AddRange(update);
        }

        public void Borrow(List<EntryDTO> update)
        {
            _pendingBorrow.AddRange(update);
        }

        public void RollBack()
        {
            _pendingReturn.Clear();
            _pendingBorrow.Clear();
        }

        public void Commit()
        {
            foreach (string returned in _pendingReturn)
            {
                _data.RemoveAll(r => r.BorrowedAssetGuid == returned);
            }
            foreach (EntryDTO entry in _pendingBorrow)
            {
                _data.RemoveAll(r => r.BorrowedAssetGuid == entry.Guid);
                _data.Add(new DatabaseRow(entry.Guid, entry.User, entry.UtcDateTime));
            }

            _pendingReturn.Clear();
            _pendingBorrow.Clear();
        }
        
        public bool TryGetRow(string guid, out DatabaseRow row)
        {
            foreach (DatabaseRow rowEntry in _data)
            {
                if (rowEntry.BorrowedAssetGuid == guid)
                {
                    row = rowEntry;
                    return true;
                }
            }

            row = DatabaseRow.Empty;
            return false;
        }
    }
    
    [Serializable]
    public class RequestData 
    {
        public string Operation = "get";
        public byte[] Serialize() => System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    [Serializable]
    public class BorrowRequestData : RequestData
    {
        public BorrowRequestData() { Operation = "borrow"; }
        public List<InternalDataBase.EntryDTO> BorrowedEntries = new List<InternalDataBase.EntryDTO>();
    }

    [Serializable]
    public class ReturnRequestData : RequestData
    {
        public ReturnRequestData() { Operation = "return"; }
        public List<string> ReturnedGuids = new List<string>();
    }

    [Serializable]
    public class ResponseData
    {
        public void Deserialize(string data) => JsonUtility.FromJsonOverwrite(data, this);  
        public bool Success = true;
        public string Error = "";
    }
    
    [Serializable]
    public class GetResponseData : ResponseData
    {
        public List<InternalDataBase.EntryDTO> BorrowedEntries = new List<InternalDataBase.EntryDTO>();
    }
}