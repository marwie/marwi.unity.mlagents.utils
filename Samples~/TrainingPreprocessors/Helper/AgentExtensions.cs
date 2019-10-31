using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using marwi.mlagents;
using MLAgents;
using UnityEngine;

namespace Helper
{
    public static class AgentExtensions
    {
        private static readonly Dictionary<Component, AgentMonitor> monitors = new Dictionary<Component, AgentMonitor>();

        public static AgentMonitor GetOrCreateMonitor(Component agent)
        {
            if (!monitors.ContainsKey(agent))
                monitors.Add(agent, agent.GetComponent<AgentMonitor>() ?? agent.gameObject.AddComponent<AgentMonitor>());
            return monitors[agent];
        }

        public static float Visualize(this float value, string name, Component owner, Visualizsation viz = Visualizsation.PlainValue)
        {
            return InternalVisualize(viz, owner, name, value);
        }

        public static int Visualize(this int value, string name, Component owner, Visualizsation viz = Visualizsation.PlainValue)
        {
#if UNITY_EDITOR
            return (int) InternalVisualize(viz, owner, name, value);
#else
            return value;
#endif
        }

        public static Vector3 Visualize(this Vector3 value, string name, Component owner, Visualizsation viz = Visualizsation.PlainValue)
        {
            InternalVisualize(viz, owner, name, value.x);
            InternalVisualize(viz, owner, name, value.y);
            InternalVisualize(viz, owner, name, value.z);
            return value;
        }

        private static float InternalVisualize(Visualizsation viz, Component owner, string name, float value)
        {
#if UNITY_EDITOR
            var monitor = GetOrCreateMonitor(owner);
            if (!monitor) return value;
            monitor.Log(name, value, viz);
            return value;
#else
            return value;
#endif
        }
    }
}