using System;
using System.Collections.Generic;
using UnityEngine;

namespace BorritEditor.Database
{
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