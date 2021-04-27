using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace BorritEditor.Database.GoogleAppScript
{
    public class GoogleAppScriptDatabase: IDatabase
    {
        public bool IsInitialized { get; private set; }
        public IDatabaseSettings Settings { get; private set; }

        public event EventHandler<bool> OnInitialized;
        
        private InternalDataBase _data = new InternalDataBase();
        private bool _refreshing = false;

        public void Initialize(string borrowerName, string projectName)
        {
            Settings = new GoogleAppScriptSettings();
            IsInitialized = true;
            OnInitialized?.Invoke(this, true);
        }

        public void Reset()
        {
            if (!IsInitialized)
                return;

            _refreshing = false;
            Settings = null;
            IsInitialized = false;
            _data.Clear();
        }

        public void BorrowAssets(string[] guids, string borrowerName)
        {
            if (!IsInitialized)
                return;

            if (_refreshing || _data.Dirty)
            {
                Debug.LogError("There is a pending petition in progress");
                return;
            }
            
            List<InternalDataBase.EntryDTO> newEntries = new List<InternalDataBase.EntryDTO>();
            long time = DateTime.UtcNow.ToBinary();
            foreach (string guid in guids)
            {
                newEntries.Add(new InternalDataBase.EntryDTO
                {
                    Guid = guid,
                    User = borrowerName,
                    UtcDateTime = time
                });
            }
            _data.Borrow(newEntries);
            SendWebRequest(new BorrowRequestData{BorrowedEntries = newEntries}, OnUpdateResponse);
        }

        public void ReturnAssets(string[] guids)
        {
            if (!IsInitialized)
                return;

            if (_refreshing || _data.Dirty)
            {
                Debug.LogError("There is a pending petition in progress");
                return;
            }
            
            ReturnRequestData requestData = new ReturnRequestData();
            requestData.ReturnedGuids.AddRange(guids);
            _data.Return(requestData.ReturnedGuids);
            SendWebRequest(requestData, OnUpdateResponse);
        }

        public void Refresh()
        {
            if (!IsInitialized)
                return;
            
            _refreshing = true;
            SendWebRequest(new RequestData(), OnGetDataResponse);
        }

        private void OnGetDataResponse(string responseRaw)
        {
            GetResponseData response = new GetResponseData();
            response.Deserialize(responseRaw);
            _refreshing = false;
            if (!response.Success)
            {
                Debug.LogError(response.Error);
                return;
            }
            _data.Clear();
            _data.Borrow(response.BorrowedEntries);
            _data.Commit();
        }

        private void OnUpdateResponse(string responseRaw)
        {
            ResponseData response = new ResponseData();
            response.Deserialize(responseRaw);
            _refreshing = false;
            if (!response.Success)
            {
                Debug.LogError(response.Error);
                _data.RollBack();
                return;
            }
            _data.Commit();
        }

        public DatabaseRow GetBorrowedAssetData(string guid)
        {
            return _data.TryGetRow(guid, out DatabaseRow row) ? row : DatabaseRow.Empty;
        }

        public bool IsAssetBorrowed(string guid)
        {
            return _data.TryGetRow(guid, out DatabaseRow row);
        }

        private void SendWebRequest(RequestData data, Action<string> response)
        {
            string url = BorritSettings.Instance.Get<string>(GoogleAppScriptSettings.Keys.ScriptUrl);
            var request = new UnityWebRequest(url);
            request.method = UnityWebRequest.kHttpVerbPOST;
            request.useHttpContinue = false;
            request.redirectLimit = 10;
            request.timeout = 60;
            request.downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = new UploadHandlerRaw(data.Serialize());
            request.SetRequestHeader("Content-Type", "application/json");
            request.SendWebRequest().completed += op =>
            {
                UnityWebRequest resp = ((UnityWebRequestAsyncOperation) op).webRequest;
#if UNITY_2020_1_OR_NEWER
                if (resp.result == UnityWebRequest.Result.ConnectionError)
#else
                if (request.isNetworkError || request.isHttpError) 
#endif
                {
                    Debug.LogError($"Error communicating with google app script {resp.error}");
                    return;
                }
                response(resp.downloadHandler.text);
            };
        }
    }
}