using System;
using System.Collections.Generic;
using marwi.mlagents;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Helper
{
    public class MultiAgentTrainingProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
            foreach (var env in EachTrainingsEnvironmentMarker())
            {
                Debug.Log($"Create {env.Count} Environment Copies of {env.name}", env);
                env.CreateCopies();
            }
        }

        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChange;
        }

        private static void OnPlayModeChange(PlayModeStateChange mode)
        {
            switch (mode)
            {
                case PlayModeStateChange.ExitingEditMode:
                    foreach (var env in EachTrainingsEnvironmentMarker())
                    {
                        if (env.DestroyCopiesInPlayMode)
                            env.DestroyCopies();
                    }

                    break;
            }
        }

        private static IEnumerable<MultiAgentTrainingsEnvironmentMarker> EachTrainingsEnvironmentMarker()
        {
            var trainingScene = Object.FindObjectOfType<TrainingSceneMarker>();
            if (!trainingScene || !trainingScene.enabled)
            {
//                Debug.Log("Not a training scene");
                yield break;
            }

            var copyGridMarkers = Object.FindObjectsOfType<MultiAgentTrainingsEnvironmentMarker>();
            foreach (var copy in copyGridMarkers)
            {
                if (copy && copy.enabled)
                    yield return copy;
            }
        }
    }
}