using System;
using System.Linq;
using System.Security.Cryptography;
using marwi.mlagents;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Helper
{
    /// <summary>
    /// add to the root of your environment
    /// </summary>
    [ExecuteInEditMode]
    public class MultiAgentTrainingsEnvironmentMarker : MonoBehaviour
    {
#if UNITY_EDITOR
#pragma warning disable CS0649
        [SerializeField] private Transform Environment;
        [SerializeField] private int CopyCount = 0;
#pragma warning restore CS0649

        public enum EnterPlayModeBehaviour
        {
            KeepCopies = 0,
            DestroyCopies = 1
        }

        public EnterPlayModeBehaviour OnPlayMode = EnterPlayModeBehaviour.DestroyCopies;

        public bool DestroyCopiesInPlayMode => OnPlayMode == EnterPlayModeBehaviour.DestroyCopies;

        [Header("Allowed Components")] public bool Camera = false;

        [HideInInspector] [SerializeField] private Transform copyTarget;
        [HideInInspector] [SerializeField] private GameObject environmentTemplate;


        public int Count => CopyCount;

        [ContextMenu(nameof(CreateCopies))]
        public void CreateCopies()
        {
            if (!Environment) return;
            if (copyTarget == Environment) return;
            var totalBounds = new Bounds(Vector3.zero, Vector3.zero);
            var environmentRenderer = Environment.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in environmentRenderer)
                totalBounds.Encapsulate(rend.bounds);

            if (!copyTarget) copyTarget = new GameObject(Environment.name + "-Copies").transform;
            DestroyCopies();

            // clean environment of any scripts we dont want in our copies
            environmentTemplate.SafeDestroy();
            environmentTemplate = Instantiate(Environment.gameObject);
            environmentTemplate.name = Environment.name + "-Template";
            environmentTemplate.CollectComponents(
                typeof(MultiAgentTrainingsEnvironmentMarker),
                typeof(AutoUpdateObservations),
                typeof(AudioListener),
                Camera ? typeof(Camera) : null
            ).SafeDestroy();

            // create copies
            for (var i = 0; i < CopyCount; i++)
            {
                var instance = Instantiate(environmentTemplate, copyTarget, false);
                if (!instance || instance == null)
                {
                    Debug.LogWarning("Failed to create Environment Instance for " + Environment.name, this);
                    break;
                }

                instance.transform.position = Environment.transform.position + new Vector3(totalBounds.size.x * 1.1f * (i + 1), 0, 0);
            }

            // cleanup
            environmentTemplate.SafeDestroy();
            HideAndDisablePickingOfCopiedEnvironments();
            if (copyTarget)
                Debug.Log("Created " + copyTarget.childCount + " Training Copies of " + Environment.name, this);
        }

        [ContextMenu(nameof(DestroyCopies))]
        public void DestroyCopies()
        {
            if (copyTarget == Environment) return;
            if (!copyTarget) return;
            var count = copyTarget.childCount;
            if (count <= 0) return;
            for (var i = copyTarget.childCount - 1; i >= 0; i--)
            {
                var child = copyTarget.GetChild(i);
                child.gameObject.SafeDestroy();
            }

            Debug.Log("Removed " + count + " Training Copies of " + Environment.name, this);

            copyTarget.gameObject.SafeDestroy();
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
            environmentTemplate.SafeDestroy();
        }
#endif
    }
}