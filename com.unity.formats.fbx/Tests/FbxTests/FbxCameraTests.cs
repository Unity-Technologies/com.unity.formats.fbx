// NOTE: uncomment the next line to leave temporary FBX files on disk
// and create a imported object in the scene.
//#define DEBUG_UNITTEST

using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEditor.Formats.Fbx.Exporter.CustomExtensions;
using UnityEditor.Formats.Fbx.Exporter;

namespace FbxExporter.UnitTests
{
    public class FbxCameraTest : ExporterTestBase
    {
        const float EPSILON = 0.00001f;

        [TearDown]
        public override void Term()
        {
            #if (!DEBUG_UNITTEST)
            base.Term();
            #endif
        }

        [Test]
        public void AnimationWithCameraFOVTest()
        {
            var keyData = new FbxAnimationTest.PropertyKeyData
            {
                componentType = typeof(Camera),
                propertyName = "field of view",
                keyTimes = new float[] {0f, 1f, 2f},
                keyFloatValues = new float[] {1f, 2f, 3f}
            };
            var tester = new FbxAnimationTest.AnimTester {keyData = keyData, testName = "CameraFOV", path = GetRandomFbxFilePath()};

            tester.DoIt();
        }

        [Test]
        public void GameCameraTest()
        {
            var filename = GetRandomFileNamePath();

            var original = FbxAnimationTest.AnimTester.CreateTargetObject("GameCamera", typeof(Camera));

            var origCam = original.GetComponent<Camera>();

            // Configure Game Camera
            origCam.fieldOfView = 59;

            // FBX Value range is [0.001, 600000.0] centimeters
            // Unity Property Inspector range is [0.01, MAX_FLT] meters
            // Unity Importer range is [0.3, MAX_FLT] meters
            origCam.nearClipPlane = 30f.Centimeters().ToMeters(); // minumum
            origCam.farClipPlane = 6000f;

            // Convert it to FBX. The asset file will be deleted automatically
            // on termination.
            var fbxAsset = ModelExporter.ExportObject(
                filename, original);

            // refresh the assetdata base so that we can query for the model
            AssetDatabase.Refresh();

            var source = AssetDatabase.LoadMainAssetAtPath(fbxAsset) as GameObject;
            var srcCam = source.GetComponent<Camera>();

            Assert.That(srcCam.aspect, Is.EqualTo(origCam.aspect));
            Assert.That(srcCam.fieldOfView, Is.EqualTo(origCam.fieldOfView));
            Assert.That(srcCam.farClipPlane, Is.EqualTo(origCam.farClipPlane));
            Assert.That(srcCam.nearClipPlane, Is.EqualTo(origCam.nearClipPlane));
        }

        [Test]
        public void PhysicalCameraTest()
        {
            var filename = GetRandomFileNamePath();

            var original = FbxAnimationTest.AnimTester.CreateTargetObject("GameCamera", typeof(Camera));

            var origCam = original.GetComponent<Camera>();

            // Configure FilmBack Super 8mm, 5.79f x 4.01mm
            origCam.usePhysicalProperties = true;
            origCam.focalLength = 50f.Centimeters().ToMillimeters();
            origCam.sensorSize = new Vector2(5.79f, 4.01f);

            // Lens Shift ( Film Offset ) as a percentage between 0 and 1
            origCam.lensShift = new Vector2(1f, 1f);

            origCam.nearClipPlane = 30f.Centimeters().ToMeters();
            origCam.farClipPlane = 600000f.Centimeters().ToMeters();

#if UNITY_2022_2_OR_NEWER
            origCam.focusDistance = 500f.Centimeters().ToMeters();
#endif

            // Convert it to FBX. The asset file will be deleted automatically
            // on termination.
            var fbxAsset = ModelExporter.ExportObject(
                filename, original);

            var source = AssetDatabase.LoadMainAssetAtPath(fbxAsset) as GameObject;
            var srcCam = source.GetComponent<Camera>();

            Assert.That(srcCam.fieldOfView, Is.EqualTo(origCam.fieldOfView).Within(EPSILON));
            Assert.That(srcCam.focalLength, Is.EqualTo(origCam.focalLength));
            Assert.That(srcCam.aspect, Is.EqualTo(origCam.aspect));
            Assert.That(srcCam.sensorSize.x, Is.EqualTo(origCam.sensorSize.x).Within(EPSILON));
            Assert.That(srcCam.sensorSize.y, Is.EqualTo(origCam.sensorSize.y).Within(EPSILON));
            Assert.That(srcCam.usePhysicalProperties, Is.EqualTo(origCam.usePhysicalProperties));
            Assert.That(srcCam.lensShift.x, Is.EqualTo(origCam.lensShift.x).Within(EPSILON));
            Assert.That(srcCam.lensShift.y, Is.EqualTo(origCam.lensShift.y).Within(EPSILON));
            Assert.That(srcCam.nearClipPlane, Is.EqualTo(origCam.nearClipPlane));
            Assert.That(srcCam.farClipPlane, Is.EqualTo(origCam.farClipPlane));
#if UNITY_2022_2_OR_NEWER
            Assert.That(srcCam.focusDistance, Is.EqualTo(origCam.focusDistance));
#endif
        }
    }
}
