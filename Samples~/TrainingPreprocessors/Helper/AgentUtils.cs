using System;
using System.Collections.Generic;
using System.Reflection;
using MLAgents;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace Helper
{
    public static class AgentUtils
    {
        private static FieldInfo m_Info;

        public static Type GetBaseTypeBeforeMonoBehaviour(Type type)
        {
            if (type.BaseType == null || type.BaseType.Name == nameof(MonoBehaviour))
                return type;
            return GetBaseTypeBeforeMonoBehaviour(type.BaseType);
        }

        /// <summary>
        /// get vector observation count
        /// </summary>
        public static int GetVectorObservationCount(Agent agent)
        {
            if (agent == null) return -1;
            if (m_Info == null)
            {
                var type = GetBaseTypeBeforeMonoBehaviour(agent.GetType());
                m_Info = type.GetField("m_Info", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (m_Info == null)
            {
                Debug.LogWarning("Failed to get FieldInfo", agent);
                return -1;
            }
            var info = (AgentInfo) m_Info.GetValue(agent);
            info.vectorObservation = new List<float>();
            m_Info.SetValue(agent, info);
            agent.InitializeAgent();
            agent.CollectObservations();
            return info.vectorObservation.Count;
        }

        public static bool CanInitialize(this Agent agent)
        {
            return PrefabStageUtility.GetPrefabStage(agent.gameObject) == null;
        }
    }
}