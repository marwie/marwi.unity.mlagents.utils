using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace marwi.mlagents
{
    public class AgentEnvironmentBounds : MonoBehaviour
    {
        public enum GizmoMode
        {
            Always,
            Selected,
            Never
        }

        public GizmoMode Mode = GizmoMode.Selected;
        public Color Tint = new Color(.5f, .5f, .5f, .2f);
        [Range(-.5f, .5f)] public float Plane = 0;
        

        private void OnDrawGizmos()
        {
            if (Mode == GizmoMode.Always)
                DrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (Mode == GizmoMode.Selected)
                DrawGizmos();
        }

        private void DrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            var col = Tint;
            col.a = .7f;
            Gizmos.color = col;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            var col2 = Tint;
            Gizmos.color = col2;
            Gizmos.DrawCube(new Vector3(0, Plane, 0), new Vector3(1, 0.000001f, 1f));
        }

        public Bounds Bounds => new Bounds(transform.position, transform.lossyScale);
        
        public Vector3 GetRandomPosition()
        {
            var rpl = new Vector4(Mathf.Lerp(-.5f, .5f, Random.value), Mathf.Lerp(-.5f, .5f, Random.value), Mathf.Lerp(-.5f, .5f, Random.value), 1);
            return transform.localToWorldMatrix * rpl;
        }

        public bool IsOutOfBounds(Vector3 position)
        {
            var normalizedPosition = NormalizePosition(position);
            return normalizedPosition.x < -.5f || normalizedPosition.x > .5f || normalizedPosition.y < -.5f || normalizedPosition.y > .5f ||
                   normalizedPosition.z < -.5f || normalizedPosition.z > .5f;
        }

        public bool IsInBounds(Vector3 position) => !IsOutOfBounds(position);

        public Vector3 NormalizePosition(Vector3 position)
        {
            var p = (Vector4) position;
            p.w = 1;
            return transform.worldToLocalMatrix * p;
        }

        public Vector3 Scale => transform.localToWorldMatrix * Vector3.one;
    }
}