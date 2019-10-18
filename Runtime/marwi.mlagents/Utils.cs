using System.Security;
using UnityEngine;

namespace marwi.mlagents
{
    public static class Utils
    {
        public static Vector3 RandomBetween(Vector3 min, Vector3 max) => new Vector3(Mathf.Lerp(min.x, max.x, Random.value),
            Mathf.Lerp(min.y, max.y, Random.value), Mathf.Lerp(min.z, max.z, Random.value));

        public static Vector2 XZ(this Vector3 v) => new Vector2(v.x, v.z);
        public static Vector2 VectorXZ(Vector3 to, Vector3 from) => to.XZ() - from.XZ();

        public static Vector3 Modulo(this Vector3 vec, Vector3 mod) => new Vector3(vec.x % mod.x, vec.y % mod.y, vec.z % mod.z);
        public static Vector3 Modulo(this Vector3 vec, float mod) => new Vector3(vec.x % mod, vec.y % mod, vec.z % mod);

        public static Vector3 CenterEulerAngleDifference(Vector3 vec)
        {
            vec += Vector3.one * 180;
            vec = vec.Modulo(360);
            if (vec.x < 0)
                vec.x += 360;
            if (vec.y < 0)
                vec.y += 360;
            if (vec.z < 0)
                vec.z += 360;
            vec -= Vector3.one * 180;
            return vec;
        }
    }
}