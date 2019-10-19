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

        private void OnValidate()
        {
            if (!Academy)
                Academy = FindObjectOfType<Academy>();
        }

        private void OnEnable()
        {
            // just to show the enabled checkbox
        }

        [ContextMenu(nameof(UpdateParameters))]
        public void UpdateParameters()
        {
            if (!Curriculum || !Academy || Curriculum.Parameters == null) return;
            Academy.resetParameters.Clear();
            foreach (var param in Curriculum.Parameters)
            {
                if (param.Values != null && param.Values.Length > 0)
                    Academy.resetParameters.Add(param.Name, param.Values[0]);
            }
        }
#endif
    }
}