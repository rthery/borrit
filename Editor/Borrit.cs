using System;
using System.Collections;
using System.IO;
using BorritEditor.Database;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace BorritEditor
{
    [InitializeOnLoad]
    public static class Borrit
    {
        private const string AssetsGuid = "00000000000000001000000000000000";
        
        private static IDatabase _database;
        
        private static bool IsInitialized
        {
            get
            {
                if (_database == null || _database.IsInitialized == false)
                    return false;

                string username = BorritSettings.Instance.Get<string>(BorritSettings.Keys.Username, SettingsScope.User);
                if (string.IsNullOrEmpty(username))
                    return false;

                return true;
            }
        }

        public static IDatabase Database => _database;

        static Borrit()
        {
            if (Application.isBatchMode)
                return; // No point to use Borrit in batch mode!
            
            Initialize();
        }

        public static void Initialize()
        {
            Reset();
            
            BorritSettings.InitDefaultValues();
            string selectedDatabase = BorritSettings.Instance.Get<string>(BorritSettings.Keys.SelectedDatabase);
            if (string.IsNullOrEmpty(selectedDatabase) == false)
            {
                selectedDatabase = selectedDatabase.Replace(" ", string.Empty);
                string databaseClassFullName = $"BorritEditor.Database.{selectedDatabase}.{selectedDatabase}Database";
                Type t = Type.GetType(databaseClassFullName);
                if (t == null)
                {
                    Debug.LogError($"[Borrit] Failed to initialize, could not find {databaseClassFullName}");
                    return;
                }
                _database = Activator.CreateInstance(t) as IDatabase;
                if (_database == null)
                {
                    Debug.LogError($"[Borrit] Failed to initialize, could not instantiate {databaseClassFullName}");
                    return;
                }
                _database.OnInitialized += OnDatabaseInitialized;
                
                string username = BorritSettings.Instance.Get<string>(BorritSettings.Keys.Username, SettingsScope.User);
                _database.Initialize(username, Application.productName);
            }
        }

        public static void Reset()
        {
            if (_database != null)
            {
                _database.OnInitialized -= OnDatabaseInitialized;
                _database.OnUpdated -= OnDatabaseUpdated;
                _database.Reset();
                _database = null;
            }
        }

        private static void OnDatabaseInitialized(object sender, bool success)
        {
            if (success)
            {
                _database.OnInitialized -= OnDatabaseInitialized;
                _database.OnUpdated += OnDatabaseUpdated;
                EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;

                EditorCoroutineUtility.StartCoroutineOwnerless(RefreshDatabaseCoroutine());
            }
        }

        private static void OnDatabaseUpdated(object sender, EventArgs e)
        {
            // We have to do it twice, otherwise it doesn't always trigger EditorApplication.projectWindowItemOnGUI...
            EditorApplication.RepaintProjectWindow();
            EditorApplication.RepaintProjectWindow();
        }
        
        private static IEnumerator RefreshDatabaseCoroutine()
        {
            while (true)
            {
                try
                {
                    _database.Refresh();
                }
                catch (Exception)
                {
                    // ignored
                    // TODO Investigate and handle error when waking up computer
                }

                float refreshInterval = BorritSettings.Instance.Get<int>(BorritSettings.Keys.DatabaseRefreshInterval, SettingsScope.User);
                yield return new EditorWaitForSeconds(refreshInterval);
            }
        }

        [MenuItem("Assets/Borrit/Borrow #&b", priority = 1984)]
        private static void BorrowAsset()
        {
            string username = BorritSettings.Instance.Get<string>(BorritSettings.Keys.Username, SettingsScope.User);
            _database.BorrowAssets(Selection.assetGUIDs, username);
        }
        
        [MenuItem("Assets/Borrit/Borrow #&b", true)]
        private static bool BorrowAssetValidate()
        {
            if (IsInitialized == false)
                return false;

            if (Selection.assetGUIDs.Length == 0)
                return false;
            
            foreach (string guid in Selection.assetGUIDs)
            {
                if (_database.IsAssetBorrowed(guid))
                    return false;
                
                // TODO Centralize that code with the similar part in OnProjectWindowItemOnGUI
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.Length == 0 || assetPath.StartsWith("Packages"))
                    return false;
                
                // Check if any parent directory has been borrowed recursively
                DirectoryInfo assetParentDirectory = Directory.GetParent(assetPath);
                while (assetParentDirectory != null)
                {
                    string assetParentPath = assetParentDirectory.ToString();
                    string assetParentGuid = AssetDatabase.AssetPathToGUID(assetParentPath);
                    if (assetParentGuid == AssetsGuid)
                        break;

                    if (_database.IsAssetBorrowed(assetParentGuid))
                        return false;
                    
                    assetParentDirectory = Directory.GetParent(assetParentPath);
                }
            }

            return true;
        }
        
        [MenuItem("Assets/Borrit/Return #&r", priority = 1985)]
        private static void ReturnAsset()
        {
            _database.ReturnAssets(Selection.assetGUIDs);
        }

        [MenuItem("Assets/Borrit/Return #&r", true)]
        private static bool ReturnAssetValidate()
        {
            if (IsInitialized == false)
                return false;
            
            string username = BorritSettings.Instance.Get<string>(BorritSettings.Keys.Username, SettingsScope.User);
            foreach (string guid in Selection.assetGUIDs)
            {
                DatabaseRow assetData = _database.GetBorrowedAssetData(guid);
                if (assetData.BorrowerName == username)
                    return true;
            }

            return false;
        }
        
        [MenuItem("Assets/Borrit/Refresh", priority = 1996)]
        private static void RefreshAssets()
        {
            _database.Refresh();
        }

        [MenuItem("Assets/Borrit/Refresh", true)]
        private static bool RefreshAssetsValidate()
        {
            return IsInitialized;
        }

        private static void OnProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            if (_database == null)
                return;
            
            if (string.IsNullOrEmpty(guid) || guid == AssetsGuid)
                return;
            
            if (_database.IsAssetBorrowed(guid))
            {
                DrawBorrowedIcon(selectionRect, guid);
            }
            else
            {
                if (string.IsNullOrEmpty(guid) || guid == AssetsGuid)
                    return;
                
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.Length == 0 || assetPath.StartsWith("Packages"))
                    return;
                
                // Check if any parent directory has been borrowed recursively
                DirectoryInfo assetParentDirectory = Directory.GetParent(assetPath);
                while (assetParentDirectory != null)
                {
                    string assetParentPath = assetParentDirectory.ToString();
                    string assetParentGuid = AssetDatabase.AssetPathToGUID(assetParentPath);
                    if (assetParentGuid == AssetsGuid)
                        break;
                    
                    if (_database.IsAssetBorrowed(assetParentGuid))
                    {
                        DrawBorrowedIcon(selectionRect, assetParentGuid, assetParentGuid != guid);
                    }
                    
                    assetParentDirectory = Directory.GetParent(assetParentPath);
                }
            }
        }

        private static void DrawBorrowedIcon(Rect selectionRect, string guid, bool isBorrowedIndirectly = false)
        {
            DatabaseRow borrowedAssetData = _database.GetBorrowedAssetData(guid);
            if (borrowedAssetData.IsEmpty)
                return;
            
            Rect iconRect;
            bool isListView = selectionRect.height <= 32f;
            if (isListView)
            {
                iconRect = new Rect(selectionRect.xMax - selectionRect.height, selectionRect.y, selectionRect.height, selectionRect.height);
            }
            else
            {
                iconRect = new Rect(selectionRect.x + selectionRect.width - selectionRect.height / 3f, selectionRect.y + selectionRect.height * 0.80f - selectionRect.height / 3f, selectionRect.height / 3f, selectionRect.height / 3f);
            }

            Texture2D icon;
            string username = BorritSettings.Instance.Get<string>(BorritSettings.Keys.Username, SettingsScope.User);
            if (borrowedAssetData.BorrowerName == username)
            {
                icon = Resources.Load<Texture2D>("Icons/borrowed-by-me");
            }
            else
            {
                icon = Resources.Load<Texture2D>("Icons/borrowed-by-others");
            }

            if (isBorrowedIndirectly)
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.DrawTexture(iconRect, icon);
            GUI.color = Color.white;
                
            Color previousColor = GUI.color;
            GUI.color = Color.clear;
            string borrowerName = borrowedAssetData.BorrowerName;
            DateTime borrowedDateTime = DateTime.FromBinary(borrowedAssetData.BorrowBinaryUtcDateTime).ToLocalTime();
            GUI.Button(iconRect, new GUIContent(string.Empty, $"Borrowed by {borrowerName} on {borrowedDateTime:yyyy-MM-dd HH:mm:ss}"));
            GUI.color = previousColor;
        }
    }
}