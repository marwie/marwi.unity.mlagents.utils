
using System;
#if UNITY_EDITOR
using marwi.mlagents;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Helper
{
    [Obsolete("TODO")]
    public class OnTrainingsBuild : MonoBehaviour, IPreprocessBuildWithReport
    {
        public enum Mode
        {
            DoNothing = 0,
            ActivateGO = 1,
            DeactivateGO = 2,
            DestroyGO = 3
        }

        [SerializeField]
        public Mode OnBuild;

        public int callbackOrder => 500;
        public void OnPreprocessBuild(BuildReport report)
        {
//            Debug.Log("HELLO " + OnBuild);
//            switch (OnBuild) 
//            {
//                case Mode.DoNothing:
//                    break;
//                
//                case Mode.ActivateGO:
//                    Debug.Log("Activate " + name);
//                    this.gameObject.SetActive(true);
//                    break;
//                case Mode.DeactivateGO:
//                    Debug.Log("Deactivate " + name);
//                    this.gameObject.SetActive(false);
//                    break;
//                case Mode.DestroyGO:
//                    Debug.Log("Destroy " + name);
//                    this.gameObject.SafeDestroy();
//                    break;
//            }
        }
    }
}

#endif