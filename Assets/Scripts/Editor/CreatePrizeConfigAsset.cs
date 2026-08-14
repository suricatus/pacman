using System;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class CreatePrizeConfig
    {
        /// <summary>
        /// Creates a PrizeConfig ScriptableObject asset at Assets/ScriptableObjects/PrizeManagerConfig.asset.
        /// </summary>
        [MenuItem("Tools/Create PrizeConfig")]
        public static void CreatePrizeConfigAsset()
        {
            Type prizeConfigType = Type.GetType("Tools.PrizeManager.Models.PrizeConfig, Assembly-CSharp");

            if (prizeConfigType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    prizeConfigType = assembly.GetType("Tools.PrizeManager.Models.PrizeConfig");
                    if (prizeConfigType != null)
                        break;
                }
            }

            if (prizeConfigType == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Could not find PrizeConfig type. Make sure the PrizeManager package is installed.",
                    "OK");
                return;
            }

            var config = ScriptableObject.CreateInstance(prizeConfigType);

            const string path = "Assets/ScriptableObjects/PrizeManagerConfig.asset";

            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;

            Debug.Log($"Created PrizeConfig at {path}");
        }
    }
}
