
using marwi.mlagents;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace Helper
{
    [ExecuteInEditMode]
    public class AutoXRSettings : MonoBehaviour
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
        public XRSupport InEditor = XRSupport.DoNothing;

        
#if UNITY_EDITOR
        private static AutoXRSettings instance;

        private void OnEnable()
        {
            if (instance && instance != this)
            {
                Debug.Log("Only one Component allowed: " + nameof(AutoXRSettings), instance);
                this.SafeDestroy();
            }
            else
                instance = this;
        }

        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (OnBuild == XRSupport.DoNothing) return;

            if (OnBuild == XRSupport.Disable)
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
            if (obj == PlayModeStateChange.ExitingEditMode)
            {
                if (instance.InEditor == XRSupport.Enable) PlayerSettings.virtualRealitySupported = true;
                else if(instance.InEditor == XRSupport.Disable) PlayerSettings.virtualRealitySupported = false;
            }
        }
#endif
    }
}