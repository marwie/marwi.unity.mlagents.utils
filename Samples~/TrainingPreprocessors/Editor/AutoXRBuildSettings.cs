using System;
using MLAgents;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace Helper
{
    public class AutoXRBuildSettings : MonoBehaviour
    {
        public enum XRBuildSetting
        {
            DisableXRInBuild
        }

        public XRBuildSetting Setting;
        
        
        
//        [ContextMenu(nameof(Start))]
//        void Start()
//        {
//            PlayerSettings.virtualRealitySupported = false;
//        }
//
//        // Update is called once per frame
//        void Update()
//        {
//        
//        }

        private void Start()
        {
            
        }
    }
}
