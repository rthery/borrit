﻿using UnityEditor;
using UnityEditor.SettingsManagement;

namespace BorritEditor.Database.GoogleAppScript
{
    public class GoogleAppScriptSettings : IDatabaseSettings
    {
        public bool HasBeenModified { get; private set; } = false;
        
        private UserSetting<string> _url = new UserSetting<string>(BorritSettings.Instance, Keys.ScriptUrl, string.Empty, SettingsScope.Project);
        public void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("Google Sheets", EditorStyles.boldLabel);
            _url.value = SettingsGUILayout.SettingsTextField("Script URL", _url, searchContext);
            
            HasBeenModified = EditorGUI.EndChangeCheck();
        }

        public static class Keys
        {
            public const string ScriptUrl = "Borrit.GoogleAppScript.Url";
        }
    }
}