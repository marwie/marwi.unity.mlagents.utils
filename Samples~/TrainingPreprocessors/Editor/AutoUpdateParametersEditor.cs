using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Helper
{
    public class AutoUpdateParametersEditor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }
        public void OnPreprocessBuild(BuildReport report)
        {
            var autoUpdaters = Object.FindObjectsOfType<AutoUpdateParameters>();
            foreach (var autoUpdater in autoUpdaters)
            {
                if (autoUpdater && autoUpdater.enabled) 
                    autoUpdater.UpdateParameters();
            }
        }
    }
}