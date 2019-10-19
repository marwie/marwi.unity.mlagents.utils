
using UnityEngine;
using System;
using MLAgents;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
#endif

namespace Helper
{
    public class AutoUpdateObservations : MonoBehaviour
    {
        public enum AutoTime
        {
            OnSceneSave = 0,
            OnReload = 1
        }
        
#if UNITY_EDITOR
        [FormerlySerializedAs("UpdateTime")] [SerializeField] private AutoTime AutoUpdate = AutoTime.OnSceneSave;
        [SerializeField] private Agent Agent;

        private void OnValidate()
        {
            if (Agent == null)
                Agent = this.GetComponent<Agent>();
        }

        [ContextMenu(nameof(AutoUpdateNow))]
        private void AutoUpdateNow()
        {
            UpdateObservations(AutoTime.OnReload, true);
        }

        private bool UpdateObservations(AutoTime time, bool force = false)
        {
            try
            {
                if (!force && (time != AutoUpdate || !enabled)) return false;
                var agent = Agent;
                if (!agent) return false;
                if (!agent.brain) return false;
                var vecObs = AgentUtils.GetVectorObservationCount(agent);
                if (vecObs < 0) return false;
                if (agent.brain.brainParameters.vectorObservationSize == vecObs) return false;
                agent.brain.brainParameters.vectorObservationSize = vecObs;
                EditorUtility.SetDirty(agent.brain);
                Debug.Log("UPDATED " + agent.brain.name + ": " + vecObs, agent.brain);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e, Agent);
                return false;
            }
        }


        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorApplication.playModeStateChanged += OnPlayModeChange;
        }

        private static void OnPlayModeChange(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.ExitingEditMode:
                    if(Run(AutoTime.OnReload, true))
                        AssetDatabase.SaveAssets();
                    break;
            }
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if(Run(AutoTime.OnSceneSave))
                AssetDatabase.SaveAssets();
        }

        [DidReloadScripts]
        private static void OnReloadScripts()
        {
            if (Application.isPlaying) return;
            if(Run(AutoTime.OnReload))
                AssetDatabase.SaveAssets();
        }

        private static bool Run(AutoTime time, bool force = false)
        {
            var markedAgents = FindObjectsOfType<AutoUpdateObservations>();
            if (markedAgents.Length <= 0) return false;
            var anyChanged = false;
            foreach (var auto in markedAgents)
            {
                if (!auto) continue; 
                if (auto.UpdateObservations(time, force))
                    anyChanged = true;
            }

            return anyChanged;
        }

//        private class UpdateAgentsOnPreprocessBuildHelper : IPreprocessBuildWithReport
//        {
//            public int callbackOrder { get; }
//            public void OnPreprocessBuild(BuildReport report)
//            {
//                Run(AutoTime.OnPreprocessBuild);
//            }
//        }
#endif
    }
}