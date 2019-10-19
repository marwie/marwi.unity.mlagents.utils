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
        [SerializeField] private Transform Environment;
        [SerializeField] private int CopyCount = 0;

        [HideInInspector]
        [SerializeField]
        private Transform copyTarget;

        public int Count => CopyCount;

        [ContextMenu(nameof(CreateCopies))]
        public void CreateCopies()
        {
            if (Environment == null) return;
            var totalBounds = new Bounds(Vector3.zero, Vector3.zero);
            var environmentRenderer = Environment.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in environmentRenderer)
                totalBounds.Encapsulate(rend.bounds);

            if (copyTarget == null) copyTarget = new GameObject(Environment.name + "-Copies").transform;
            DestroyCopies();

//            var isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Environment.gameObject);
            for (var i = 0; i < CopyCount; i++)
            {
                var instance = Instantiate(Environment, copyTarget, false);
                if (!instance || instance == null)
                {
                    Debug.LogWarning("Failed to create Environment Instance for " + Environment.name, this);
                    break;
                }

                instance.transform.position = Environment.transform.position + new Vector3(totalBounds.size.x * 1.1f * (i + 1), 0, 0);

            }
            
            HideAndDisablePickingOfCopiedEnvironments();
            Debug.Log("Created " + copyTarget.childCount + " Training Copies of " + Environment.name, this);
        }

        [ContextMenu(nameof(DestroyCopies))]
        public void DestroyCopies()
        {
            if (copyTarget == Environment) return;
            if (copyTarget == null) return;
            var count = copyTarget.childCount;
            if (count <= 0) return;
            for (var i = copyTarget.childCount - 1; i >= 0; i--)
            {
                var child = copyTarget.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            Debug.Log("Removed " + count + " Training Copies of " + Environment.name, this);
            
            if (Application.isPlaying)
                Destroy(copyTarget.gameObject);
            else DestroyImmediate(copyTarget.gameObject);
        }

        public void HideAndDisablePickingOfCopiedEnvironments()
        {
            if (copyTarget == null)
                return;
            SceneVisibilityManager.instance.Hide(copyTarget.gameObject, true);
            SceneVisibilityManager.instance.DisablePicking(copyTarget.gameObject, true);
        }

        private void OnEnable()
        {
            HideAndDisablePickingOfCopiedEnvironments();
        }
#endif
    }
}