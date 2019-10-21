﻿
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
        private static AutoXRSupport instance;

        private void OnEnable()
        {
            if (instance && instance != this)
            {
                Debug.Log("Overwrite previously assigned: " + nameof(AutoXRSupport), instance);
//                this.SafeDestroy();
            }
            instance = this;
        }

        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!instance) return;
            
            if (instance.OnBuild == XRSupport.DoNothing) return;

            if (instance.OnBuild == XRSupport.Disable)
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
            if (!instance) return;
            
            if (obj == PlayModeStateChange.ExitingEditMode)
            {
                if (instance.OnPlay == XRSupport.Enable) PlayerSettings.virtualRealitySupported = true;
                else if(instance.OnPlay == XRSupport.Disable) PlayerSettings.virtualRealitySupported = false;
            }
        }
#endif
    }
}