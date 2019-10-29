using System.Collections.Generic;
using System.Linq;
using AgentUtils.Editor;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace marwi.mlagents.editor
{
    public static class TrainingsUtility
    {
        public static bool OpenPlayScenesAdditive()
        {
            return ToggleScenes(trainingScenes: false);
        }

        public static bool OpenTrainingScenesAdditive()
        {
            return ToggleScenes(trainingScenes: true);
        }

        public static BuildReport MakeTrainingsBuild(MLAgentsSettings settings)
        {
            var scenesToBuild = new List<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (DetermineIsTrainingsScene(scene, true))
                {
                    Debug.Log($"Adding {scene.path} to TrainingsBuild");
                    scenesToBuild.Add(scene.path);
                }
            }

            return BuildPipeline.BuildPlayer(
                scenesToBuild.ToArray(),
                settings.ActiveConfiguration.AbsolutePathToExecuteable,
                BuildTarget.StandaloneWindows,
                BuildOptions.None
            );
        }


        private static bool ToggleScenes(bool trainingScenes)
        {
            var scenesWeWant = new List<int>();


            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid()) continue;
                if (DetermineIsTrainingsScene(scene, true) == trainingScenes)
                {
                    scenesWeWant.Add(i);
                    EditorSceneManager.OpenScene(string.IsNullOrEmpty(scene.path) ? "" : scene.path, OpenSceneMode.Additive);
                }
            }

            if (!trainingScenes && scenesWeWant.Count <= 0)
            {
                Debug.LogWarning("No Play Scene found");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (scenesWeWant.Contains(i)) continue;
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid()) continue;
                if (SceneManager.sceneCount > 1)
                    EditorSceneManager.CloseScene(scene, false);
            }

            return scenesWeWant.Count > 0;
        }

        private static readonly List<GameObject> roots = new List<GameObject>();

        private static bool DetermineIsTrainingsScene(Scene scene, bool loadAdditiveIfNecessary = false)
        {
            if (!scene.IsValid()) return false;

            var result = false;
            var wasLoaded = scene.isLoaded;
            if (!scene.isLoaded && loadAdditiveIfNecessary && !string.IsNullOrEmpty(scene.path))
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);

            if (scene.isLoaded)
            {
                roots.Clear();
                scene.GetRootGameObjects(roots);
                result = roots.Any(root => root.GetComponentInChildren<TrainingSceneMarker>());
//                    Debug.Log("is training scene " + scene.path + ": " + result);
            }

            if (!wasLoaded && loadAdditiveIfNecessary) EditorSceneManager.CloseScene(scene, false);


            return result;
        }
    }
}