
using System;
using marwi.mlagents;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

#endif

namespace Helper
{
    [ExecuteInEditMode]
    public class AutoXRSupport : MonoBehaviour
#if UNITY_EDITOR
        , IPreprocessBuildWithReport
#endif
    {
        public enum XRSupport
        {
            DoNothing = -1,
            Disable = 0,
            Enable = 1
        }

        public XRSupport OnBuild = XRSupport.Disable;
        public XRSupport OnPlay = XRSupport.DoNothing;

        
#if UNITY_EDITOR
        
        public int callbackOrder => 500;

        public void OnPreprocessBuild(BuildReport report)
        {
            var autoXR = FindObjectOfType<AutoXRSupport>();
            
            if (!autoXR) return;
            
            if (autoXR.OnBuild == XRSupport.DoNothing) return;

            if (autoXR.OnBuild == XRSupport.Disable)
            {
                var virtualRealityWasEnabled = PlayerSettings.virtualRealitySupported;
                if (!virtualRealityWasEnabled) return;
                PlayerSettings.virtualRealitySupported = false;
                Debug.Log("Building without XR Support", this);
            }
        }

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChange;
        }

        private static void OnPlayModeChange(PlayModeStateChange obj)
        {
            var autoXR = FindObjectOfType<AutoXRSupport>();
            
            if (!autoXR) return;
            
            if (obj == PlayModeStateChange.ExitingEditMode)
            {
                if (autoXR.OnPlay == XRSupport.Enable)
                {
                    Debug.Log("Enable ´Virtual Reality Support", autoXR);
                    PlayerSettings.virtualRealitySupported = true;
                }
                else if (autoXR.OnPlay == XRSupport.Disable)
                {
                    Debug.Log("Disable Virtual Reality Support", autoXR);
                    PlayerSettings.virtualRealitySupported = false;
                }
            }
        }
#endif
    }
}