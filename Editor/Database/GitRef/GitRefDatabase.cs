using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace BorritEditor.Database.GitRef
{
    public class GitRefDatabase : IDatabase
    {
        public bool IsInitialized { get; private set; }
        public IDatabaseSettings Settings => _settings;

        public event EventHandler<bool> OnInitialized;
        public event EventHandler OnUpdated;

        private readonly InternalDataBase _data = new InternalDataBase();
        private GitRefSettings _settings;
        private GitRefStore _store;
        private string _borrowerName;
        private int _generation;
        private bool _isGitRunning;
        private bool _isUpdating;
        private string _lastLoggedError;
        private int _progressId = -1;

        public void Initialize(string borrowerName, string projectName)
        {
            _settings = new GitRefSettings();
            _borrowerName = borrowerName;
            string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
            GitRefStore store = new GitRefStore(projectDirectory, _settings.Remote, _settings.RefName);
            _store = store;

            _generation++;
            StartGit(() => store.CheckSetup(), (error, exception) =>
            {
                if (exception != null)
                    error = exception.Message;

                _settings.Error = error;
                IsInitialized = error == null;
                if (IsInitialized == false)
                    Debug.LogError($"[Borrit] Failed to initialize Git Ref database: {error}");

                OnInitialized?.Invoke(this, IsInitialized);
            });
        }

        public void Reset()
        {
            _generation++;
            _store = null;
            _settings = null;
            _isGitRunning = false;
            _isUpdating = false;
            IsInitialized = false;
            _data.Clear();
        }

        public void BorrowAssets(string[] guids, string borrowerName)
        {
            if (IsInitialized == false)
                return;

            if (_isUpdating)
            {
                Debug.LogError("[Borrit] Failed to borrow! Another request is in progress. Wait for it to end and try again.");
                return;
            }

            long time = DateTime.UtcNow.ToBinary();
            List<InternalDataBase.EntryDTO> alreadyBorrowedEntries = new List<InternalDataBase.EntryDTO>();
            Func<List<InternalDataBase.EntryDTO>, bool> applyChanges = entries =>
            {
                alreadyBorrowedEntries.Clear();
                bool hasChanged = false;
                foreach (string guid in guids)
                {
                    InternalDataBase.EntryDTO existingEntry = entries.Find(entry => entry.Guid == guid);
                    if (existingEntry != null && existingEntry.User != borrowerName)
                    {
                        alreadyBorrowedEntries.Add(existingEntry);
                        continue;
                    }

                    entries.Remove(existingEntry);
                    entries.Add(new InternalDataBase.EntryDTO
                    {
                        Guid = guid,
                        User = borrowerName,
                        UtcDateTime = time
                    });
                    hasChanged = true;
                }

                return hasChanged;
            };

            StartUpdate("borrow", applyChanges, borrowerName, () =>
            {
                foreach (InternalDataBase.EntryDTO entry in alreadyBorrowedEntries)
                {
                    Debug.LogWarning($"[Borrit] Could not borrow {AssetDatabase.GUIDToAssetPath(entry.Guid)}, it is already borrowed by {entry.User}");
                }
            });
        }

        public void ReturnAssets(string[] guids)
        {
            if (IsInitialized == false)
                return;

            if (_isUpdating)
            {
                Debug.LogError("[Borrit] Failed to return! Another request is in progress. Wait for it to end and try again.");
                return;
            }

            HashSet<string> returnedGuids = new HashSet<string>(guids);
            Func<List<InternalDataBase.EntryDTO>, bool> applyChanges = entries => entries.RemoveAll(entry => returnedGuids.Contains(entry.Guid)) > 0;
            StartUpdate("return", applyChanges, _borrowerName, null);
        }

        public void Refresh()
        {
            if (IsInitialized == false || _isGitRunning || _isUpdating)
                return;

#if UNITY_2020_1_OR_NEWER
            if (BorritSettings.Instance.Get<bool>(BorritSettings.Keys.DatabaseRefreshBackgroundProgress, SettingsScope.User))
            {
                if (Progress.Exists(_progressId))
                    Progress.Remove(_progressId);
                _progressId = Progress.Start("Refreshing Borrit Database", null, Progress.Options.Managed);
            }
#endif

            GitRefStore store = _store;
            StartGit(() => store.Fetch(), (snapshot, exception) =>
            {
                OnSnapshotReceived(snapshot, exception);

#if UNITY_2020_1_OR_NEWER
                if (Progress.Exists(_progressId))
                {
                    if (exception != null)
                    {
                        Progress.Report(_progressId, 1f, exception.Message);
                        Progress.Finish(_progressId, Progress.Status.Failed);
                    }
                    else
                    {
                        Progress.Remove(_progressId);
                    }
                }
#endif
            });
        }

        public DatabaseRow GetBorrowedAssetData(string guid)
        {
            return _data.TryGetRow(guid, out DatabaseRow row) ? row : DatabaseRow.Empty;
        }

        public IReadOnlyList<DatabaseRow> GetBorrowedAssetsData()
        {
            return _data.Rows;
        }

        public bool IsAssetBorrowed(string guid)
        {
            return _data.TryGetRow(guid, out DatabaseRow _);
        }

        private void StartUpdate(string operation, Func<List<InternalDataBase.EntryDTO>, bool> applyChanges, string authorName, Action onSuccess)
        {
            _isUpdating = true;
            EditorUtility.DisplayProgressBar("Please Wait", $"Validating {operation} operation...", 1f);
            EditorCoroutineUtility.StartCoroutineOwnerless(UpdateCoroutine(_generation, applyChanges, authorName, onSuccess));
        }

        private IEnumerator UpdateCoroutine(int generation, Func<List<InternalDataBase.EntryDTO>, bool> applyChanges, string authorName, Action onSuccess)
        {
            while (_isGitRunning && generation == _generation)
            {
                yield return null;
            }

            if (generation != _generation)
            {
                EditorUtility.ClearProgressBar();
                yield break;
            }

            GitRefStore store = _store;
            _isGitRunning = true;
            yield return RunGitCoroutine(generation, () => store.Update(applyChanges, authorName), (snapshot, exception) =>
            {
                OnSnapshotReceived(snapshot, exception);
                if (exception == null)
                    onSuccess?.Invoke();
            });

            if (generation == _generation)
                _isUpdating = false;
            EditorUtility.ClearProgressBar();
        }

        private void OnSnapshotReceived(GitRefSnapshot snapshot, Exception exception)
        {
            if (exception != null)
            {
                if (exception.Message != _lastLoggedError)
                {
                    _lastLoggedError = exception.Message;
                    Debug.LogError($"[Borrit] {exception.Message}");
                }
                _settings.Error = exception.Message;
                return;
            }

            _lastLoggedError = null;
            _settings.Error = null;
            if (snapshot.HasChanged == false)
                return;

            _data.Clear();
            _data.Borrow(snapshot.Entries);
            _data.Commit();

            OnUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void StartGit<T>(Func<T> work, Action<T, Exception> onCompleted)
        {
            _isGitRunning = true;
            EditorCoroutineUtility.StartCoroutineOwnerless(RunGitCoroutine(_generation, work, onCompleted));
        }

        private IEnumerator RunGitCoroutine<T>(int generation, Func<T> work, Action<T, Exception> onCompleted)
        {
            Task<T> task = Task.Run(work);
            while (task.IsCompleted == false)
            {
                yield return null;
            }

            if (generation != _generation)
                yield break;

            _isGitRunning = false;
            onCompleted(task.IsFaulted ? default : task.Result, task.IsFaulted ? task.Exception.GetBaseException() : null);
        }
    }
}
