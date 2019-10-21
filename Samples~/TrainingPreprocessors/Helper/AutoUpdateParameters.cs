using System;
using marwi.mlagents;
using MLAgents;
using UnityEngine;

namespace Helper
{
    public class AutoUpdateParameters : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private Academy Academy;
        [SerializeField] private Curriculum Curriculum;
        [SerializeField] private int DefaultLevel = 0;

        private void OnValidate()
        {
            if (!Academy)
                Academy = FindObjectOfType<Academy>();
            UpdateParameters();
        }

        private void OnEnable()
        {
            UpdateParameters();
        }

        [ContextMenu(nameof(UpdateParameters))]
        public void UpdateParameters()
        {
            if (!Curriculum || !Academy || Curriculum.Parameters == null) return;
            Academy.resetParameters.Clear();
            foreach (var param in Curriculum.Parameters)
            {
                if (param.Values != null && param.Values.Length > 0)
                {
                    var levelIndex = Mathf.Max(0, Mathf.Min(DefaultLevel, param.Values.Length - 1));
                    Academy.resetParameters.Add(param.Name, param.Values[levelIndex]);
                }
            }
        }
#endif
    }
}