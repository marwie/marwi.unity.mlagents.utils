using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

namespace marwi.mlagents.Visualizer
{
    public abstract class BaseValueDisplayProvider : MonoBehaviour
    {
        public uint Decimals = 2;

        private string decimalsStr;

        private void Awake()
        {
            decimalsStr = $"N{Decimals}";
        }

        private void OnValidate()
        {
            decimalsStr = $"N{Decimals}";
        }

        private readonly Dictionary<string, List<float>> values = new Dictionary<string, List<float>>();
        private readonly Dictionary<string, IDisplayInstance> instances = new Dictionary<string, IDisplayInstance>();

        public virtual void Clear()
        {
            values?.Clear();
        }

        public void Append(string key, float value)
        {
            if (!values.TryGetValue(key, out var list))
            {
                list = new List<float>();
                values.Add(key, list);
            }

            list.Add(value);
        }

        public void UpdateLayout(RectTransform panel)
        {
            foreach (var kvp in values)
            {
                var key = kvp.Key;
                if (!instances.TryGetValue(key, out var instance))
                {
                    instance = GetInstance(panel);
                    if (instance != null)
                        instances.Add(key, instance);
                }

//                if (instance == null)
//                {
//                    if (instances.ContainsKey(key))
//                        instances.Remove(key);
//                    continue;
//                }

                if (!instance.enabled) continue;

                instance.decimalString = this.decimalsStr;
                instance.OnDisplay(key, kvp.Value);
            }
        }

        public abstract Visualizsation Type { get; }
        protected abstract IDisplayInstance GetInstance(RectTransform panel);
    }
}