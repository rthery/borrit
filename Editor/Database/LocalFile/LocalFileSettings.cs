using UnityEditor;

namespace BorritEditor.Database.LocalFile
{
    public class LocalFileSettings : IDatabaseSettings
    {
        public bool HasBeenModified { get; }
        
        public void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("LocalFileSettings", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox("NOT IMPLEMENTED YET!", MessageType.Warning);
        }
    }
}