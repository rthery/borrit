using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;

namespace BorritEditor.Database.GoogleAppScript
{
    public class GoogleAppScriptDatabase : IDatabase
    {
        public bool IsInitialized { get; private set; }
        public IDatabaseSettings Settings { get; private set; }

        public event EventHandler<bool> OnInitialized;
        public event EventHandler OnUpdated;
        
        private InternalDataBase _data = new InternalDataBase();

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

            Settings = null;
            IsInitialized = false;
            _data.Clear();
        }

        public void BorrowAssets(string[] guids, string borrowerName)
        {
            if (!IsInitialized)
                return;

            if (_data.Dirty)
            {
                Debug.LogError("[Borrit] Failed to borrow! Another request is in progress. Wait for it to end and try again.");
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
            
            EditorCoroutineUtility.StartCoroutineOwnerless(SendWebRequestCoroutine(new BorrowRequestData{BorrowedEntries = newEntries}, OnUpdateResponse));
        }

        public void ReturnAssets(string[] guids)
        {
            if (!IsInitialized)
                return;

            if (_data.Dirty)
            {
                Debug.LogError("[Borrit] Failed to return! Another request is in progress. Wait for it to end and try again.");
                return;
            }
            
            ReturnRequestData requestData = new ReturnRequestData();
            requestData.ReturnedGuids.AddRange(guids);
            _data.Return(requestData.ReturnedGuids);
            EditorCoroutineUtility.StartCoroutineOwnerless(SendWebRequestCoroutine(requestData, OnUpdateResponse));
        }

        public void Refresh()
        {
            if (!IsInitialized)
                return;
            
            EditorCoroutineUtility.StartCoroutineOwnerless(SendWebRequestCoroutine(new RequestData(), OnGetDataResponse));
        }

        private void OnGetDataResponse(string responseRaw)
        {
            if (!IsInitialized)
                return;
            
            GetResponseData response = new GetResponseData();
            response.Deserialize(responseRaw);
            if (!response.Success)
            {
                Debug.LogError(response.Error);
                return;
            }
            _data.Clear();
            _data.Borrow(response.BorrowedEntries);
            _data.Commit();
            
            OnUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnUpdateResponse(string responseRaw)
        {
            if (!IsInitialized)
                return;
            
            ResponseData response = new ResponseData();
            response.Deserialize(responseRaw);
            if (!response.Success)
            {
                Debug.LogError(response.Error);
                _data.RollBack();
                return;
            }
            _data.Commit();
            
            OnUpdated?.Invoke(this, EventArgs.Empty);
        }

        public DatabaseRow GetBorrowedAssetData(string guid)
        {
            return _data.TryGetRow(guid, out DatabaseRow row) ? row : DatabaseRow.Empty;
        }

        public bool IsAssetBorrowed(string guid)
        {
            return _data.TryGetRow(guid, out DatabaseRow row);
        }

        private UnityWebRequest _currentGetOperationWebRequest;
        private int _progressId = -1;
        private IEnumerator SendWebRequestCoroutine(RequestData data, Action<string> response)
        {
            string url = BorritSettings.Instance.Get<string>(GoogleAppScriptSettings.Keys.ScriptUrl);
            if (string.IsNullOrEmpty(url))
            {
                yield break;
            }
            
            bool isGetOperation = data.Operation == "get";
            if (isGetOperation)
            {
                if (_currentGetOperationWebRequest != null)
                    yield break; // Another get operation is already in progress
                
#if UNITY_2020_1_OR_NEWER
                if (BorritSettings.Instance.Get<bool>(BorritSettings.Keys.DatabaseRefreshBackgroundProgress, SettingsScope.User))
                {
                    if (Progress.Exists(_progressId))
                        Progress.Remove(_progressId);
                    _progressId = Progress.Start("Refreshing Borrit Database", null, Progress.Options.Managed);
                }
#endif
            }
            else
            {
                AbortGetOperationWebRequest();
                EditorUtility.DisplayProgressBar("Please Wait", $"Validating {data.Operation} operation...", 1f);
            }
            
            UnityWebRequest request = new UnityWebRequest(url);
            request.method = UnityWebRequest.kHttpVerbPOST;
            request.useHttpContinue = false;
            request.redirectLimit = 10;
            request.timeout = 60;
            request.downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = new UploadHandlerRaw(data.Serialize());
            request.SetRequestHeader("Content-Type", "application/json");
            if (isGetOperation)
                _currentGetOperationWebRequest = request;
            yield return request.SendWebRequest();
            
            bool hasError = false;
#if UNITY_2020_1_OR_NEWER
            if (request.result == UnityWebRequest.Result.ConnectionError)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                hasError = true;
                bool isAborted = request.error == "Request aborted";
                bool hasLostConnection = request.error == "Cannot connect to destination host" || request.error == "Cannot resolve destination host";
                if (isAborted == false && hasLostConnection == false)
                {
                    Debug.LogError($"[Borrit] Error communicating with Google App Script: {request.error}");
                }
            }
            else
            {
                response(request.downloadHandler.text);
            }

            request.Dispose();
            
            if (isGetOperation)
            {
                _currentGetOperationWebRequest = null;
#if UNITY_2020_1_OR_NEWER
                if (Progress.Exists(_progressId))
                {
                    if (hasError)
                    {
                        Progress.Finish(_progressId, Progress.Status.Failed);
                    }
                    else
                    {
                        Progress.Remove(_progressId);
                    }
                }
#endif
            }
            else
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void AbortGetOperationWebRequest()
        {
            if (_currentGetOperationWebRequest != null)
            {
                _currentGetOperationWebRequest.Abort();
                _currentGetOperationWebRequest = null;
            }
        }
    }
}