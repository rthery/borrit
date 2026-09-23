using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace BorritEditor.Database.GitRef
{
    public class GitRefSettings : IDatabaseSettings
    {
        public const string DefaultRemote = "origin";
        public const string DefaultRefName = "refs/borrit/locks";

        public bool HasBeenModified { get; private set; } = false;
        public string Remote => _remote.value;
        public string RefName => _refName.value;
        public string Error { get; set; }

        private UserSetting<string> _remote = new UserSetting<string>(BorritSettings.Instance, Keys.Remote, DefaultRemote, SettingsScope.Project);
        private UserSetting<string> _refName = new UserSetting<string>(BorritSettings.Instance, Keys.RefName, DefaultRefName, SettingsScope.Project);

        public void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("Git Ref", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string remote = EditorGUILayout.DelayedTextField(new GUIContent("Remote", "Name of the git remote storing the borrowed assets"), _remote.value);
            string refName = EditorGUILayout.DelayedTextField(new GUIContent("Ref Name", "Git ref storing the borrowed assets"), _refName.value);
            HasBeenModified = EditorGUI.EndChangeCheck();

            if (HasBeenModified)
            {
                _remote.SetValue(remote.Trim());
                _refName.SetValue(refName.Trim());
                BorritSettings.Instance.Save();
            }

            if (string.IsNullOrEmpty(Error) == false)
                EditorGUILayout.HelpBox(Error, MessageType.Error);

            if (GUILayout.Button("Check Setup") || HasBeenModified)
                Borrit.Initialize();
        }

        public static class Keys
        {
            public const string Remote = "Borrit.GitRef.Remote";
            public const string RefName = "Borrit.GitRef.RefName";
        }
    }
}
