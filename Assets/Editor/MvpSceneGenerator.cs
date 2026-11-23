#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntonDoors.Editor
{
    public static class MvpSceneGenerator
    {
        private const string ScenePath = "Assets/Scenes/Game.unity";
        private const string VersionPath = "Assets/AntonDoors/scene-version.txt";
        private const string VersionPref = "AntonDoors.SceneVersion";
        private static bool scheduled;

        [InitializeOnLoadMethod]
        private static void AutoGenerateOnce()
        {
            if (scheduled || File.Exists(ScenePath)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().isDirty) return;

            scheduled = true;
            EditorApplication.delayCall += GenerateScene;
        }

        // Позволяет пересобрать уже существующую сцену после изменений генератора.
        [InitializeOnLoadMethod]
        private static void AutoRegenerateOnVersionChange()
        {
            if (scheduled || !File.Exists(VersionPath)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().isDirty) return;

            string version = File.ReadAllText(VersionPath).Trim();
            if (string.IsNullOrEmpty(version) || EditorPrefs.GetString(VersionPref, string.Empty) == version) return;

            EditorPrefs.SetString(VersionPref, version);
            scheduled = true;
            EditorApplication.delayCall += GenerateScene;
        }

        [InitializeOnLoadMethod]
        private static void EnsureBuildSettings()
        {
            if (File.Exists(ScenePath)) AddSceneToBuildSettings();
        }

        [MenuItem("Tools/AntonDoors/Generate MVP Scene")]
        public static void GenerateScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            WorldBuilder.CreateWorld(0);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AddSceneToBuildSettings();

            Debug.Log("[AntonDoors] MVP-сцена создана: " + ScenePath);
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (EditorBuildSettingsScene s in scenes)
            {
                if (s.path == ScenePath) { found = true; break; }
            }
            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
