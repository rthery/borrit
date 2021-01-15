using System;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace BorritEditor.Database.GoogleSheets
{
    public class GoogleSheetsSettings : IDatabaseSettings
    {
        public bool HasBeenModified { get; private set; }

        private UserSetting<string> _spreadsheetId = new UserSetting<string>(BorritSettings.Instance, Keys.SpreadsheetId, string.Empty, SettingsScope.Project);
        private UserSetting<string> _credentials = new UserSetting<string>(BorritSettings.Instance, Keys.Credentials, string.Empty, SettingsScope.Project);
        
        public void OnGUI(string searchContext)
        {
            GoogleSheetsDatabase database = Borrit.Database as GoogleSheetsDatabase;

            HasBeenModified = false; 

            EditorGUILayout.LabelField("Google Sheets", EditorStyles.boldLabel);
            
            // Workaround because modifying a setting value via BorritSettings.Instance.Set will not actually update the UserSetting value!
            if (BorritSettings.Instance.Get<string>(Keys.Credentials) == String.Empty)
                _credentials.value = string.Empty;
            
            GUILayout.BeginHorizontal();
            _credentials.value = SettingsGUILayout.SettingsTextField("Credentials", _credentials, searchContext);
            GUI.SetNextControlName("BrowseBtn");
            if (GUILayout.Button("Browse"))
            {
                string absolutePath = EditorUtility.OpenFilePanelWithFilters("Select your Google API OAuth 2.0 Client ID JSON file", EditorApplication.applicationPath, new string[] {"JSON files", "json"});
                if (string.IsNullOrEmpty(absolutePath) == false)
                {
                    string projectPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1);
                    _credentials.value = absolutePath.Replace(projectPath, string.Empty);
                    
                    // Force focus back to the button, so that the SettingsTextField is updated...
                    GUI.FocusControl("BrowseBtn");
                    HasBeenModified = true;
                }
            }
            GUILayout.EndHorizontal();

            // Check if we need a token
            bool hasCredentials = File.Exists(_credentials.value);
            if (database.HasAuthToken)
            {
                GUI.enabled = false;
                EditorGUILayout.TextField("Authentication token", database.AuthTokenUserFilePath);
                GUI.enabled = true;
            }
            else
            {
                bool hasUsername = string.IsNullOrEmpty(BorritSettings.Instance.Get<string>(BorritSettings.Keys.Username, SettingsScope.User)) == false;
                if (hasUsername == false)
                {
                    EditorGUILayout.HelpBox("Authentication requires a unique username within your team", MessageType.Warning);
                }
                else if (hasCredentials)
                {
                    EditorGUILayout.HelpBox("Authentication required before you can use Google Sheets", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Browse for a valid credentials JSON file before you can authenticate", MessageType.Info);
                }

                GUI.enabled = hasCredentials && hasUsername;
                if (database.IsAuthenticating == false)
                {
                    GUI.SetNextControlName("AuthenticateBtn");
                    if (GUILayout.Button("Authenticate"))
                    {
                        GUI.FocusControl("AuthenticateBtn");
                        Task<UserCredential> authTokenCreationTask = database.CreateAuthToken();
                    }
                }
                else
                {
                    if (GUILayout.Button("Cancel Authentication"))
                    {
                        database.CancelAuthTokenCreation();
                    }
                }
                GUI.enabled = true;
            }
            
            EditorGUI.BeginChangeCheck();
            _spreadsheetId.value = SettingsGUILayout.SettingsTextField("SpreadsheetId", _spreadsheetId, searchContext);
            if (EditorGUI.EndChangeCheck())
                HasBeenModified = true;

            EditorGUILayout.BeginHorizontal();
            var previousColor = GUI.contentColor;
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            if (database.IsInitialized == false && database.HasAuthToken)
            {
                GUI.contentColor = Color.red;
                EditorGUIUtility.labelWidth = 16;
                GUILayout.Label("\u2716");
                EditorGUIUtility.labelWidth = previousLabelWidth;
                GUI.contentColor = previousColor;
                GUILayout.Label("Not connected to Google Sheets");
                
                GUILayout.FlexibleSpace();
                // Display a button to connect to Google Sheets!
                if (GUILayout.Button("Connect", GUILayout.Width(300)))
                {
                    Borrit.Initialize();
                }
            }
            else if (database.IsInitialized)
            {
                GUI.contentColor = Color.green;
                EditorGUIUtility.labelWidth = 16;
                GUILayout.Label("\u2714");
                EditorGUIUtility.labelWidth = previousLabelWidth;
                GUI.contentColor = previousColor;
                GUILayout.Label("Connected to Google Sheets");
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        public static class Keys
        {
            public const string SpreadsheetId = "GoogleSheetSpreadsheetId";
            public const string Credentials = "GoogleSheetCredentials";
        }
    }
}