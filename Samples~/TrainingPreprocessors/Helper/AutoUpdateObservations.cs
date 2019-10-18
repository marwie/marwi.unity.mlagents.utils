
using UnityEngine;
using System;
using System.Collections.Generic;
using MLAgents;

#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
#endif

namespace Helper
{
    public class AutoUpdateObservations : MonoBehaviour
    {
        public enum AutoTime
        {
            OnReload,
//            OnPreprocessBuild
        }
        
#if UNITY_EDITOR
        [SerializeField] private AutoTime UpdateTime = AutoTime.OnReload;
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
            if (!force && (time != UpdateTime || !enabled)) return false;
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
        
        
        
        [DidReloadScripts]
        public static void OnReloadScripts()
        {
            if (Application.isPlaying) return;
            if(Run(AutoTime.OnReload))
                AssetDatabase.SaveAssets();
        }

        private static readonly HashSet<Type> updated = new HashSet<Type>();
        private static bool Run(AutoTime time)
        {
            var markedAgents = FindObjectsOfType<AutoUpdateObservations>();
            if (markedAgents.Length <= 0) return false;
            updated.Clear();
            var anyChanged = false;
            foreach (var auto in markedAgents)
            {
                if (!auto) continue;
                var type = auto.GetType();
                if (updated.Contains(type)) continue;
                updated.Add(type);
                if (auto.UpdateObservations(time))
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