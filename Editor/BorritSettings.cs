using System;
using System.Collections.Generic;
using BorritEditor.Database;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace BorritEditor
{
    public class BorritSettings
    {
        private static Settings _settings;

        public static Settings Instance
        {
            get
            {
                if (_settings == null)
                    _settings = new Settings("io.github.borrit");
                return _settings;
            }
        }

        private static Dictionary<string, Type> _databaseTypesByName = null;

        [SettingsProvider]
        private static SettingsProvider CreateProjectSettingsProvider()
        {
            TypeCache.TypeCollection databaseTypes = TypeCache.GetTypesDerivedFrom<IDatabase>();
            _databaseTypesByName = new Dictionary<string, Type>(databaseTypes.Count);
            foreach (Type databaseType in databaseTypes)
            {
                _databaseTypesByName.Add(ObjectNames.NicifyVariableName(databaseType.Name.Replace("Database", string.Empty)), databaseType);
            }

            UserSettingsProvider provider = new UserSettingsProvider("Project/Borrit",
                Instance,
                new [] { typeof(BorritSettings).Assembly },
                SettingsScope.Project);
            
            return provider;
        }
        
        [UserSetting("General", "Username")]
        private static UserSetting<string> _userName = new UserSetting<string>(BorritSettings.Instance, Keys.Username, string.Empty, SettingsScope.User);

        private static UserSetting<string> _database = new UserSetting<string>(BorritSettings.Instance, Keys.SelectedDatabase, "Google Sheets", SettingsScope.Project);
        private static UserSetting<int> _databaseRefreshInterval = new UserSetting<int>(BorritSettings.Instance, Keys.DatabaseRefreshInterval, 10, SettingsScope.User);
        
        [UserSettingBlock("General")]
        private static void GeneralGUI(string searchContext)
        {
            bool saveSettings = false;
            
            EditorGUI.BeginChangeCheck();
            
            var databaseNames = new List<string>(_databaseTypesByName.Keys);
            databaseNames.Sort();
            int selectedIndex = databaseNames.IndexOf(_database.value);
            if (selectedIndex == -1) selectedIndex = 0;
            selectedIndex = EditorGUILayout.Popup("Database", selectedIndex, databaseNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                saveSettings = true;
                _database.SetValue(databaseNames[selectedIndex]);
                Borrit.Initialize();
            }
            
            if (Borrit.Database == null)
                Borrit.Initialize();

            string tooltip = "Interval in seconds between each request to the database to refresh the state of borrowed assets in the Project window";
            _databaseRefreshInterval.value = SettingsGUILayout.SettingsIntField(new GUIContent("Database Refresh Interval", tooltip), _databaseRefreshInterval, searchContext);
            if (_databaseRefreshInterval.value < 0)
                _databaseRefreshInterval.value = 0;
            
            GUILayout.Space(16);

            // Display settings of the selected database
            if (Borrit.Database != null)
            {
                Borrit.Database.Settings.OnGUI(searchContext);
            }
            
            if (saveSettings)
                Instance.Save();
        }
        
        public static class Keys
        {
            public const string Username = "Username";
            public const string SelectedDatabase = "SelectedDatabase";
            public const string DatabaseRefreshInterval = "DatabaseRefreshInterval";
        }
    }
}