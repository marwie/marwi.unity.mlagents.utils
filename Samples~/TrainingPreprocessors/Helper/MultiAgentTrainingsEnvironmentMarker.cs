using marwi.mlagents;
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
        public enum CopyPattern
        {
            X = 0,
            XZ = 1
        }
        
#if UNITY_EDITOR
#pragma warning disable CS0649
        [SerializeField] private Transform Environment;
        [SerializeField] private uint CopyCount = 0;
        [SerializeField] private CopyPattern Pattern = CopyPattern.X;
        [SerializeField, Range(0, 1)] private float BoundsOverlap = 0f;
#pragma warning restore CS0649

        public enum EnterPlayModeBehaviour
        {
            KeepCopies = 0,
            DestroyCopies = 1
        }

        public EnterPlayModeBehaviour OnPlayMode = EnterPlayModeBehaviour.DestroyCopies;

        public bool DestroyCopiesInPlayMode => OnPlayMode == EnterPlayModeBehaviour.DestroyCopies;

        [Header("Allowed Components")] public bool Camera = false;

        [HideInInspector, SerializeField] private Transform copyTarget;
        [HideInInspector, SerializeField] private GameObject environmentTemplate;

        public int Count => (int)(CopyCount);

        [ContextMenu(nameof(CreateCopies))]
        public void CreateCopiesAndShow()
        {
            CreateCopies();
            SetVisibility(true);
        }

        public void CreateCopies()
        {
            if (!Environment) return;
            if (copyTarget == Environment) return;
            var totalBounds = new Bounds(Vector3.zero, Vector3.zero);
            var environmentRenderer = Environment.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in environmentRenderer)
                totalBounds.Encapsulate(rend.bounds);
            var rendererBoundsSize = totalBounds.extents;
            var agentBounds = Environment.GetComponentsInChildren<AgentEnvironmentBounds>();
            foreach (var ab in agentBounds)
                totalBounds.Encapsulate(ab.Bounds);
            totalBounds.extents = Vector3.Lerp(totalBounds.extents, rendererBoundsSize, BoundsOverlap);


            DestroyCopies();

            if (!copyTarget) copyTarget = new GameObject(Environment.name + "-Copies").transform;

            // clean environment of any scripts we dont want in our copies
            environmentTemplate.SafeDestroy();
            environmentTemplate = Instantiate(Environment.gameObject);
            environmentTemplate.name = "ENVIRONMENT_Template";
            environmentTemplate.CollectComponents(
                typeof(MultiAgentTrainingsEnvironmentMarker),
                typeof(AutoUpdateObservations),
                typeof(AudioListener),
                Camera ? typeof(Camera) : null
            ).SafeDestroy();

            // create copies
            InstantiateCopies(totalBounds, environmentTemplate);

            // cleanup
            environmentTemplate.SafeDestroy();
            HideAndDisablePickingOfCopiedEnvironments();
            if (copyTarget)
                Debug.Log("Created " + copyTarget.childCount + " Training Copies of " + Environment.name, this);
        }

        private void InstantiateCopies(Bounds totalBounds, GameObject template)
        {
            switch (Pattern)
            {
                default:
                case CopyPattern.X:
                    for (var i = 0; i < CopyCount; i++)
                    {
                        var instance = Instantiate(template, copyTarget, false);
                        if (!instance || instance == null)
                        {
                            Debug.LogWarning("Failed to create Environment Instance for " + Environment.name, this);
                            break;
                        }

                        instance.transform.position = Environment.transform.position + new Vector3(totalBounds.size.x * (i + 1), 0, 0);
                    }
                    break;
                case CopyPattern.XZ:
                    var count = Mathf.CeilToInt(Mathf.Sqrt(CopyCount));
                    var instances = 0;
                    var failed = false;
                    for (var z = 0; z < count; z++)
                    {
                        if (failed) break;
                        if (instances >= CopyCount) break;
                        for (var x = 0; x < count; x++)
                        {
                            if (instances >= CopyCount) break;
                            var instance = Instantiate(template, copyTarget, false);
                            if (!instance || instance == null)
                            {
                                Debug.LogWarning("Failed to create Environment Instance for " + Environment.name, this);
                                failed = true;
                                break;
                            }

                            var offset = new Vector3(totalBounds.size.x * x, 0, totalBounds.size.z * z);
                            instance.transform.position = Environment.transform.position + offset;
                            ++instances;
                        }
                    }
                    break;
            }
            
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
            environmentTemplate.SafeDestroy();
        }


        public void HideAndDisablePickingOfCopiedEnvironments()
        {
            if (copyTarget == null)
                return;
            SetVisibility(false);
            SceneVisibilityManager.instance.DisablePicking(copyTarget.gameObject, true);
        }

        private void SetVisibility(bool visible)
        {
            if (copyTarget == null)
                return;
            if (!visible)
                SceneVisibilityManager.instance.Hide(copyTarget.gameObject, true);
            else 
                SceneVisibilityManager.instance.Show(copyTarget.gameObject, true);
        }

        private void OnEnable()
        {
            HideAndDisablePickingOfCopiedEnvironments();
            environmentTemplate.SafeDestroy();
//            DestroyCopies();
        }
#endif
    }
}