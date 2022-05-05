using System;
using BorritEditor.Database;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BorritEditor
{
    public class BorritEditorWindow : EditorWindow
    {
        [MenuItem("Window/Borrit/Borrowed Assets")]
        private static void Init()
        {
            BorritEditorWindow window = (BorritEditorWindow)GetWindow(typeof(BorritEditorWindow));
            window.Show();
        }
        
        private void OnGUI()
        {
            if (Borrit.IsInitialized == false)
            {
                EditorGUILayout.HelpBox("Borrit is not initialized! Please setup Borrit in your Project Settings.", MessageType.Error);
                if (GUILayout.Button("Open Project Settings"))
                {
                    SettingsService.OpenProjectSettings("Project/Borrit");
                }
                return;
            }
            
            string username = BorritSettings.Instance.Get<string>(BorritSettings.Keys.Username, SettingsScope.User);

            EditorGUILayout.LabelField("Borrowed Assets", EditorStyles.boldLabel);
            var borrowedAssetsData = Borrit.Database.GetBorrowedAssetsData();
            foreach (DatabaseRow databaseRow in borrowedAssetsData)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(databaseRow.BorrowedAssetGuid);
                Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = false;
                EditorGUILayout.ObjectField(asset, typeof(Object), false);
                GUI.enabled = true;

                if (databaseRow.BorrowerName == username)
                {
                    if (GUILayout.Button("Return"))
                    {
                        Borrit.Database.ReturnAssets(new[] { databaseRow.BorrowedAssetGuid }); // TODO Add a ReturnAsset method to IDatabase
                    }
                }
                else
                {
                    DateTime borrowedDateTime = DateTime.FromBinary(databaseRow.BorrowBinaryUtcDateTime).ToLocalTime();
                    string iconTooltip = $"Borrowed by {databaseRow.BorrowerName} on {borrowedDateTime:yyyy-MM-dd HH:mm:ss}";
                    GUIContent icon = new GUIContent(Resources.Load<Texture2D>("Icons/borrowed-by-others"), iconTooltip);
                    EditorGUILayout.LabelField(icon, GUILayout.MaxWidth(20));
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}