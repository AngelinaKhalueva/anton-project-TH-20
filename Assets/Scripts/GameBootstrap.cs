using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntonDoors
{
    public static class GameBootstrap
    {
        private static bool subscribed;
        private static int lastBuildFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!subscribed)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                subscribed = true;
            }
            BuildWorld();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BuildWorld();
        }

        private static void BuildWorld()
        {
            if (Application.isPlaying && Time.frameCount == lastBuildFrame) return;

            GameObject oldRoot = GameObject.Find("GameRoot");
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

            // В SampleScene по умолчанию есть камера и свет — убираем их, чтобы не было дублей.
            foreach (Camera cam in UnityEngine.Object.FindObjectsOfType<Camera>())
            {
                if (!IsUnderRoot(cam.transform)) UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }
            foreach (AudioListener listener in UnityEngine.Object.FindObjectsOfType<AudioListener>())
            {
                if (!IsUnderRoot(listener.transform)) UnityEngine.Object.DestroyImmediate(listener.gameObject);
            }
            foreach (Light light in UnityEngine.Object.FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional && !IsUnderRoot(light.transform))
                    UnityEngine.Object.DestroyImmediate(light.gameObject);
            }

            GameManager gm = WorldBuilder.CreateWorld(0);
            Debug.Log("[AntonDoors] Мир готов. Монстров: " + gm.MonsterSpawns.Count);
            if (Application.isPlaying) lastBuildFrame = Time.frameCount;
        }

        private static bool IsUnderRoot(Transform t)
        {
            while (t != null)
            {
                if (t.name == "GameRoot") return true;
                t = t.parent;
            }
            return false;
        }
    }
}
