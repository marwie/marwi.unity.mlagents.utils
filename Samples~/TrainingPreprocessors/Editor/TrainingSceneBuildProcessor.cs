using System;
using UnityEditor;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;
using UnityEditor.Build.Reporting;
using MLAgents;
using UnityEditor.Build;
using UnityEngine;

namespace marwi.mlagents.utils
{
    // this can be moded to ml-agents/Editor and removed as as sample as soon as mlagents is available as a package
    // because we need access to the academy
    [InitializeOnLoad]
    public class TrainingSceneBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        static TrainingSceneBuildProcessor()
        {
            EditorApplication.playModeStateChanged += PlayModeChange;
        }

        private static void PlayModeChange(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.ExitingEditMode:
                    SetAcademyBrainsControlled(false);
                    break;
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            SetAcademyBrainsControlled(true);
        }

        private static void SetAcademyBrainsControlled(bool controlled)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                TrainingSceneMarker marker = null;
                Academy academy = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (!marker || !marker.enabled)
                        marker = root.GetComponentInChildren<TrainingSceneMarker>();
                    if (!academy)
                        academy = root.GetComponentInChildren<Academy>();
                    if (marker && academy)
                        break;
                }

                if (!marker || !marker.enabled || !academy || academy == null) continue;
                foreach (var hub in academy.broadcastHub.broadcastingBrains)
                {
                    Debug.Log("Set Controlled: " + hub.name + ", " + controlled, academy);
                    academy.broadcastHub.SetControlled(hub, controlled);
                }
            }
        }
    }
}
