using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using marwi.mlagents.Visualizer;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace marwi.mlagents
{
    public enum Visualizsation
    {
        PlainValue = 0,
    }

    public class AgentMonitor : MonoBehaviour
    {
        public RectTransform CanvasTemplate;
        public RectTransform LayoutTemplate;

        private BaseValueDisplayProvider[] displayProviders;

        private readonly Dictionary<Visualizsation, BaseValueDisplayProvider> monitoring = new Dictionary<Visualizsation, BaseValueDisplayProvider>();

        public void Log(string key, float value, Visualizsation visual = Visualizsation.PlainValue)
        {
            InternalUpdateObservation(visual, key, value);
        }

        private void InternalUpdateObservation(Visualizsation viz, string name, float value)
        {
            if (!enabled) return;

            InternalCleanup();

            if (!monitoring.TryGetValue(viz, out var visualizer))
            {
                foreach (var provider in displayProviders)
                {
                    if (provider.Type != viz) continue;
                    visualizer = provider;
                    break;
                }

                if (visualizer)
                    monitoring.Add(viz, visualizer);
            }

            if (!visualizer || visualizer == null)
            {
                if (monitoring.ContainsKey(viz))
                    monitoring.Remove(viz);
                return;
            }

            visualizer.Append(name, value);
        }

        private int lastUpdateFrame = -1;

        /// <summary>
        /// cleanup values from previous frame
        /// </summary>
        private void InternalCleanup()
        {
            if (lastUpdateFrame == Time.frameCount) return;
            foreach (var visualizer in monitoring.Values)
                visualizer.Clear();
            lastUpdateFrame = Time.frameCount;
        }

        private RectTransform canvas, layout;

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!canvas) canvas = Instantiate(CanvasTemplate);
            if (!layout) layout = Instantiate(LayoutTemplate, canvas);
            canvas.name = this.name;
            canvas.rotation = Quaternion.identity;
            InitialCanvasPosition(canvas);
            SetupDisplayProviders();
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetupDisplayProviders();
        }
#endif

        private void OnDestroy()
        {
            if (canvas)
                Destroy(canvas);
        }

#if UNITY_EDITOR
        private void LateUpdate()
        {
            foreach (var monitored in monitoring.Values) monitored.UpdateLayout(layout);
        }
#endif

        private void SetupDisplayProviders()
        {
            this.displayProviders = this.GetComponents<BaseValueDisplayProvider>();
#if UNITY_EDITOR
            if (this.displayProviders.Length <= 0) this.displayProviders = new[] {this.gameObject.AddComponent<ValueDisplayProvider>()};
#endif
        }

        private void InitialCanvasPosition(RectTransform canvas)
        {
            var meshRenderer = this.GetComponentsInChildren<MeshRenderer>();
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (var rend in meshRenderer)
                bounds.Encapsulate(rend.bounds);
            canvas.transform.position = bounds.center + .6f * bounds.size.y * Vector3.up;
            // ReSharper disable once Unity.InefficientPropertyAccess
            canvas.transform.localScale = .01f * bounds.size.magnitude * Vector3.one;
        }
    }
}