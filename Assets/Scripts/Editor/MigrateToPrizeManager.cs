using System.Collections.Generic;
using System.Reflection;
using Rewards;
using Tools.PrizeManager.Models;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class MigrateToPrizeManager : EditorWindow
    {
        private RewardConfig _oldConfig;
        private PrizeConfig _newConfig;

        /// <summary>
        /// Opens the Prize Migration editor window.
        /// </summary>
        [MenuItem("Tools/Migrate RewardService to PrizeManager")]
        public static void ShowWindow()
        {
            GetWindow<MigrateToPrizeManager>("Prize Migration");
        }

        private void OnGUI()
        {
            GUILayout.Label("Migrate Old Reward System", EditorStyles.boldLabel);

            _oldConfig = (RewardConfig)EditorGUILayout.ObjectField("Old RewardConfig", _oldConfig, typeof(RewardConfig), false);
            _newConfig = (PrizeConfig)EditorGUILayout.ObjectField("New PrizeConfig", _newConfig, typeof(PrizeConfig), false);

            if (GUILayout.Button("Migrate Data"))
            {
                MigrateData();
            }
        }

        private void MigrateData()
        {
            if (_oldConfig == null || _newConfig == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both configs", "OK");
                return;
            }

            var prizes = new List<Prize>();

            foreach (var category in _oldConfig.categories)
            {
                if (category?.items == null) continue;

                foreach (var item in category.items)
                {
                    if (item == null || string.IsNullOrEmpty(item.id)) continue;

                    var prize = new Prize(
                        item.id,
                        item.name,
                        item.initialDailyStock,
                        $"Category: {category.name}",
                        item.sprite
                    );

                    prizes.Add(prize);
                }
            }

            var prizesField = typeof(PrizeConfig).GetField(
                "prizes",
                BindingFlags.NonPublic | BindingFlags.Instance);

            prizesField?.SetValue(_newConfig, prizes);

            EditorUtility.SetDirty(_newConfig);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success", $"Migrated {prizes.Count} prizes to PrizeConfig", "OK");
        }
    }
}
