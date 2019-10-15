using UnityEngine;

namespace marwi.mlagents
{
    public static class Utils
    {
        public static Vector3 RandomBetween(Vector3 min, Vector3 max) => new Vector3(Mathf.Lerp(min.x, max.x, Random.value), Mathf.Lerp(min.y, max.y, Random.value), Mathf.Lerp(min.z, max.z, Random.value));
        
        public static Vector2 XZ(this Vector3 v) => new Vector2(v.x, v.z);
    }
}