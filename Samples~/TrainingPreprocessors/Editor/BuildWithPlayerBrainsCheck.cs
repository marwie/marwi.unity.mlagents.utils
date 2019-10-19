using marwi.mlagents;
using MLAgents;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Helper
{
    public class BuildWithPlayerBrainsCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
            var trainingSceneMarker = Object.FindObjectOfType<TrainingSceneMarker>();
            if (!trainingSceneMarker) return;
            var agents = Object.FindObjectsOfType<Agent>();
            foreach (var agent in agents)
            {
                if (!agent) continue;
                var brain = agent.brain;
                if (brain && brain is PlayerBrain) Debug.LogWarning("Build contains PlayerBrain:" + agent.name + ", " + brain.name, agent);
            }
        }
    }
}