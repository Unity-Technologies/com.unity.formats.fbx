// NOTE: uncomment the next line to leave temporary FBX files on disk
// and create a imported object in the scene.
//#define DEBUG_UNITTEST

using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FbxExporter.UnitTests
{
    public class LightAnimationTestDataClass
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData(new float[] {0f, 1f, 2f}, new float[] {1f, 2f, 3f}, "m_SpotAngle").Returns(1);
                yield return new TestCaseData(new float[] {0f, 1f, 2f}, new float[] {1f, 2f, 3f}, "m_Intensity").Returns(1);
            }
        }
    }

    public class FbxLightTest : ExporterTestBase
    {
        [TearDown]
        public override void Term()
        {
            #if (!DEBUG_UNITTEST)
            base.Term();
            #endif
        }

        [Test, TestCaseSource(typeof(LightAnimationTestDataClass), "TestCases")]
        public int AnimationWithLightTest(float[] keyTimes, float[] keyValues, string propertyName)
        {
            var keyData = new FbxAnimationTest.PropertyKeyData
            {
                componentType = typeof(Light),
                propertyName = propertyName,
                keyTimes = keyTimes,
                keyFloatValues = keyValues
            };
            var tester = new FbxAnimationTest.AnimTester {keyData = keyData, testName = "LightAnim_" + propertyName, path = GetRandomFbxFilePath()};

            return tester.DoIt();
        }

        [Test]
        public void AnimationWithLightColorTest()
        {
            var keyTimes = new float[] {0f, 1f, 2f};
            var propertyNames = new string[] {"m_Color.r", "m_Color.g", "m_Color.b"};

            var ran = new System.Random();
            float[] keyValues = Enumerable.Range(1, keyTimes.Length).Select(x => (float)ran.NextDouble()).ToArray();

            foreach (var v in keyValues)
                Debug.Log("random value: " + v);

            var keyData = new FbxAnimationTest.MultiPropertyKeyData
            {
                componentType = typeof(Light),
                propertyNames = propertyNames,
                keyTimes = keyTimes,
                keyValues = keyValues
            };
            var tester = new FbxAnimationTest.AnimTester {keyData = keyData, testName = "LightAnim_Color", path = GetRandomFbxFilePath()};

            tester.DoIt();
        }
    }
}
