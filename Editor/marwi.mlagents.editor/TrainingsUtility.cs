using System.Collections.Generic;
using System.Linq;
using AgentUtils.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace marwi.mlagents.editor
{
    public static class TrainingsUtility
    {
        public static bool LoadTrainingScenesAdditive()
        {
            var trainingSceneFound = false;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (DetermineIsTrainingsScene(i, scene))
                {
                    SceneManager.LoadSceneAsync(i, LoadSceneMode.Additive);
                    trainingSceneFound = true;
                }
            }

            return trainingSceneFound;
        }

        public static bool UnloadTrainingScenes()
        {
            var trainingSceneUnloaded = false;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (DetermineIsTrainingsScene(i, scene))
                {
                    EditorSceneManager.CloseScene(scene, false);
                    trainingSceneUnloaded = true;
                }
            }

            return trainingSceneUnloaded;
        }

        public static BuildReport MakeTrainingsBuild(MLAgentsSettings settings)
        {
            var scenesToBuild = new List<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (DetermineIsTrainingsScene(i, scene))
                    scenesToBuild.Add(scene.path);
            }

            return BuildPipeline.BuildPlayer(
                scenesToBuild.ToArray(),
                settings.ActiveConfiguration.AbsolutePathToExecuteable,
                BuildTarget.StandaloneWindows,
                BuildOptions.None
            );
        }


        private static readonly List<GameObject> roots = new List<GameObject>();

        private static bool DetermineIsTrainingsScene(int index, Scene scene, bool loadAdditiveIsNecessary = false)
        {
            if (scene.IsValid())
            {
                var wasLoaded = scene.isLoaded;
                if (!scene.isLoaded && loadAdditiveIsNecessary)
                {
                    SceneManager.LoadScene(index, LoadSceneMode.Additive);
                }
                
                if (scene.isLoaded)
                {
                    roots.Clear();
                    scene.GetRootGameObjects(roots);
                    return roots.Any(root => root.GetComponentInChildren<TrainingSceneMarker>());
                }

                if (!wasLoaded && loadAdditiveIsNecessary)
                {
                    EditorSceneManager.CloseScene(scene, false);
                }
            }

            
            return false;
        }
    }
}