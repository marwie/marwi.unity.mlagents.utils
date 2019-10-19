using System;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Helper
{
    /// <summary>
    /// add to the root of your environment
    /// </summary>
    [ExecuteInEditMode]
    public class MultiAgentTrainingsEnvironmentMarker : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private Transform Environment, CopyTarget;
        [SerializeField] private int CopyCount = 0;

        [HideInInspector]
        [SerializeField]
        private Transform previousCopyTarget;

        public int Count => CopyCount;

        [ContextMenu(nameof(CreateCopies))]
        public void CreateCopies()
        {
            if (Environment == null) return;
            var totalBounds = new Bounds(Vector3.zero, Vector3.zero);
            var environmentRenderer = Environment.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in environmentRenderer)
                totalBounds.Encapsulate(rend.bounds);

            if (CopyTarget == null)
                CopyTarget = new GameObject(Environment.name + "-Copies").transform;
            previousCopyTarget = CopyTarget;
            DestroyCopies();

//            var isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Environment.gameObject);
            for (var i = 0; i < CopyCount; i++)
            {
                var instance = Instantiate(Environment, CopyTarget, false);
                if (!instance || instance == null)
                {
                    Debug.LogWarning("Failed to create Environment Instance for " + Environment.name, this);
                    break;
                }

                instance.transform.position = Environment.transform.position + new Vector3(totalBounds.size.x * 1.1f * (i + 1), 0, 0);

            }
            
            HideAndDisablePickingOfCopiedEnvironments();
            Debug.Log("Created " + CopyTarget.childCount + " Training Copies of " + Environment.name, this);
        }

        [ContextMenu(nameof(DestroyCopies))]
        public void DestroyCopies()
        {
            if (previousCopyTarget == Environment) return;
            if (previousCopyTarget == null) return;
            var count = previousCopyTarget.childCount;
            if (count <= 0) return;
            for (var i = previousCopyTarget.childCount - 1; i >= 0; i--)
            {
                var child = previousCopyTarget.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            Debug.Log("Removed " + count + " Training Copies of " + Environment.name, this);
        }

        public void HideAndDisablePickingOfCopiedEnvironments()
        {
            if (CopyTarget == null)
            {
                Debug.Log("No copy Target");
                return;
            }
            SceneVisibilityManager.instance.Hide(CopyTarget.gameObject, true);
            SceneVisibilityManager.instance.DisablePicking(CopyTarget.gameObject, true);
        }

        // safety checks:
        private void OnValidate()
        {
            if (previousCopyTarget != null && CopyTarget != previousCopyTarget)
            {
                DestroyCopies();
            }

            if (CopyTarget != null && Environment == CopyTarget)
            {
                Debug.LogWarning("Environment and CopyTarget are not allowed to be the same", this);
                CopyTarget = null;
            }
        }

        private void OnEnable()
        {
            HideAndDisablePickingOfCopiedEnvironments();
        }
#endif
    }
}