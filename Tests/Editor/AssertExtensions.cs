using UnityEngine;
using UnityEngine.Assertions;

namespace mlagentsUtilsTests
{
    public static class AssertExtensions
    {
        public static void AreApproximatelyEqual(Vector3 expected, Vector3 actual, float threshold)
        {
            Assert.AreApproximatelyEqual(expected.x, actual.x, threshold);
            Assert.AreApproximatelyEqual(expected.y, actual.y, threshold);
            Assert.AreApproximatelyEqual(expected.z, actual.z, threshold);
        }
    }
}