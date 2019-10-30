using marwi.mlagents;
using NUnit.Framework;
using UnityEngine;
using static UnityEngine.Assertions.Assert;

namespace mlagentsUtilsTests
{
    public class UtilsTests
    {
        private const float FLOAT_THRESHOLD = 0.00001f;


        [Test]
        public void ModuloTests()
        {
            AreEqual(new Vector3(0, 0, 0), new Vector3(0, 0, 0).Modulo(1));
            AssertExtensions.AreApproximatelyEqual(new Vector3(0, .3f, -.3f), new Vector3(4, 12.3f, -3.3f).Modulo(1), FLOAT_THRESHOLD);
            AssertExtensions.AreApproximatelyEqual(new Vector3(0, 0, 1.3f), new Vector3(0, 2f, 3.3f).Modulo(2), FLOAT_THRESHOLD);
        }

        [Test]
        public void ThresholdActionTests()
        {
            Assert.AreEqual(1, 0.3f.ThresholdAction(.1f), FLOAT_THRESHOLD);
            Assert.AreEqual(-1, -0.3f.ThresholdAction(.1f), FLOAT_THRESHOLD);
            Assert.AreEqual(1, 0.3f.ThresholdAction(.3f), FLOAT_THRESHOLD);
            Assert.AreEqual(0, 0.3f.ThresholdAction(.5f), FLOAT_THRESHOLD);
            Assert.AreEqual(0, -0.3f.ThresholdAction(.5f), FLOAT_THRESHOLD);
            Assert.AreEqual(-1, -0.5f.ThresholdAction(.1f), FLOAT_THRESHOLD);
            Assert.AreEqual(-1, -2f.ThresholdAction(.5f), FLOAT_THRESHOLD);
            Assert.AreEqual(0, 0.0001f.ThresholdAction(.01f), FLOAT_THRESHOLD);
            Assert.AreEqual(0, 0.99999f.ThresholdAction(1f), FLOAT_THRESHOLD);
            Assert.AreEqual(0, -0.99999f.ThresholdAction(1f), FLOAT_THRESHOLD);
            Assert.AreEqual(1, 3f.ThresholdAction(1f), FLOAT_THRESHOLD);
            Assert.AreEqual(1, 0.1f.ThresholdAction(0f), FLOAT_THRESHOLD);
            Assert.AreEqual(-1, -0.1f.ThresholdAction(0f), FLOAT_THRESHOLD);
            Assert.AreEqual(1, 0f.ThresholdAction(0f), FLOAT_THRESHOLD);
            Assert.AreEqual(-1, -.000001f.ThresholdAction(0f), FLOAT_THRESHOLD);
        }

        [Test]
        public void StepActionTests()
        {
            Assert.AreEqual(1f, 1f.StepAction(1), FLOAT_THRESHOLD);
            Assert.AreEqual(-1f, -1f.StepAction(1), FLOAT_THRESHOLD);
            Assert.AreEqual(0.5f, 0.5f.StepAction(1), FLOAT_THRESHOLD);
            Assert.AreEqual(-0.5f, -0.5f.StepAction(1), FLOAT_THRESHOLD);
            Assert.AreEqual(.5f, 0.5f.StepAction(2), FLOAT_THRESHOLD);
            Assert.AreEqual(.5f, 0.55f.StepAction(1), FLOAT_THRESHOLD);
            Assert.AreEqual(.5f, 0.57f.StepAction(1), FLOAT_THRESHOLD);
            Assert.AreEqual(.4f, 0.49f.StepAction(1), FLOAT_THRESHOLD);
            Assert.AreEqual(-.5f, -0.53f.StepAction(1), FLOAT_THRESHOLD);
            Assert.AreEqual(-.53f, -0.5365f.StepAction(2), FLOAT_THRESHOLD);
            Assert.AreEqual(.555f, 0.555222f.StepAction(3), FLOAT_THRESHOLD);
            Assert.AreEqual(1.555f, 1.555222f.StepAction(3), FLOAT_THRESHOLD);
            Assert.AreEqual(1, 1.3213543f.StepAction(0), FLOAT_THRESHOLD);

            var vec = new Vector2(1.2f, -1);
            var res = vec.StepAction(0);
            Assert.AreEqual(1f, res.x, FLOAT_THRESHOLD);
            Assert.AreEqual(-1f, res.y, FLOAT_THRESHOLD);
        }

        [Test]
        public void CenterEulerAngleDifference()
        {
            var angle = new Vector3(0, 20, 180).CenterEulerAngleDifference();
            AreEqual(new Vector3(0, 20, -180), angle);

            angle = new Vector3(179, 181, 180.1f).CenterEulerAngleDifference();
            AreEqual(new Vector3(179, -179f, -179.9f), angle);

            angle = new Vector3(90, -10, -150).CenterEulerAngleDifference();
            AreEqual(new Vector3(90, -10, -150), angle);

            angle = new Vector3(-20, 350, -350).CenterEulerAngleDifference();
            AreEqual(new Vector3(-20, -10, 10), angle);

            angle = new Vector3(120, 190, 200).CenterEulerAngleDifference();
            AreEqual(new Vector3(120, -170, -160), angle);
        }
    }
}