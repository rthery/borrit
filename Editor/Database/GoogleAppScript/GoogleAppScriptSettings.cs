using UnityEditor;
using UnityEditor.SettingsManagement;

namespace BorritEditor.Database.GoogleAppScript
{
    public class GoogleAppScriptSettings : IDatabaseSettings
    {
        public bool HasBeenModified { get; private set; } = false;
        
        private UserSetting<string> _url = new UserSetting<string>(BorritSettings.Instance, Keys.ScriptUrl, string.Empty, SettingsScope.Project);
        public void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("Google App Script", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _url.value = SettingsGUILayout.SettingsTextField("Script URL", _url, searchContext);
            
            HasBeenModified = EditorGUI.EndChangeCheck();

            if (HasBeenModified)
            {
                if (string.IsNullOrEmpty(_url.value) == false)
                {
                    if (_url.value.ToLowerInvariant().StartsWith("https://script.google.com/macros/") == false)
                    {
                        _url.value = null;
                        EditorUtility.DisplayDialog("Error", "Please paste a valid URL to the Google App Script web app", "Ok");
                    }
                }
            }
        }

        public static class Keys
        {
            public const string ScriptUrl = "Borrit.GoogleAppScript.Url";
        }
    }
}