using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using FbxExporters.Editor;

namespace FbxExporters.UnitTests
{
    public class RotationCurveTest : ExporterTestBase {

    	[Test]
    	public void TestBasics() {
            // Test get euler index
            var eulerCurve = new EulerCurve();
            Assert.That(EulerCurve.GetEulerIndex ("localEulerAnglesRaw.y"), Is.EqualTo(1));
            Assert.That(EulerCurve.GetEulerIndex ("localEulerAnglesRaw."), Is.EqualTo(-1));
            Assert.That(EulerCurve.GetEulerIndex ("m_LocalRotation.x"), Is.EqualTo(-1));

            // Test get quaternion index
            var quaternionCurve = new QuaternionCurve();
            Assert.That(QuaternionCurve.GetQuaternionIndex ("m_LocalRotation.w"), Is.EqualTo(3));
            Assert.That(QuaternionCurve.GetQuaternionIndex ("m_LocalRotation"), Is.EqualTo(-1));
            Assert.That(QuaternionCurve.GetQuaternionIndex ("localEulerAnglesRaw.y"), Is.EqualTo(-1));

            // Test SetCurve
            var animCurve = new AnimationCurve();

            eulerCurve.SetCurve (2, animCurve);
            Assert.That (eulerCurve.m_curves [2], Is.EqualTo (animCurve));

            Assert.That(() => eulerCurve.SetCurve (-1, animCurve), Throws.Exception.TypeOf<System.IndexOutOfRangeException>());
            Assert.That(() => eulerCurve.SetCurve (3, animCurve), Throws.Exception.TypeOf<System.IndexOutOfRangeException>());

            quaternionCurve.SetCurve (3, animCurve);
            Assert.That (quaternionCurve.m_curves [3], Is.EqualTo (animCurve));

            Assert.That(() => quaternionCurve.SetCurve (-5, animCurve), Throws.Exception.TypeOf<System.IndexOutOfRangeException>());
            Assert.That(() => quaternionCurve.SetCurve (4, animCurve), Throws.Exception.TypeOf<System.IndexOutOfRangeException>());
    	}

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "RotationCurveTestCases")]
        public void TestEulerCurveExport (string prefabPath) {
            prefabPath = FindPathInUnitTests (prefabPath);
            Assert.That (prefabPath, Is.Not.Null);

            // add fbx to scene
            GameObject originalGO = AddAssetToScene(prefabPath);

            // get clips
            var animator = originalGO.GetComponentInChildren<Animator> ();
            var animClips = FbxAnimationTest.GetClipsFromAnimator (animator);

            // export fbx
            string filename = ExportToFbx(originalGO);

            var fbxAnimClips = FbxAnimationTest.AnimTester.GetClipsFromFbx (filename);

            Assert.That (fbxAnimClips.Count, Is.EqualTo (animClips.Length));
            FbxAnimationTest.AnimTester.MultiClipTest (animClips, fbxAnimClips);
        }
    }
}