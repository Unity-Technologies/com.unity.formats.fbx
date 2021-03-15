#if ENABLE_FBX_RECORDER
using UnityEngine;
using NUnit.Framework;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace FbxExporter.UnitTests
{
    public class FbxRecorderTest : ExporterTestBase
    {
        public static string[] RecorderTestCases = new string[]
        {
            "Models/Player/Player.prefab"
        };

        [Test, TestCaseSource(typeof(FbxRecorderTest), "RecorderTestCases")]
    	public void TransferAnimationTest(string prefabPath) 
        {
            prefabPath = FindPathInUnitTests(prefabPath);
            Assert.That(prefabPath, Is.Not.Null);

            var go = AddAssetToScene(prefabPath);
            Assert.That(go);

            AnimationInputSettings animationInputSettings = new AnimationInputSettings();
            animationInputSettings.gameObject = go;

            FbxRecorderSettings settings = ScriptableObject.CreateInstance(typeof(FbxRecorderSettings)) as FbxRecorderSettings;
            settings.animationInputSettings = animationInputSettings;

            settings.TransferAnimationSource = go.transform;

            Transform skeletonNode = go.transform.GetChild(2);
            settings.TransferAnimationDest = skeletonNode;

            Assert.That(settings.TransferAnimationSource, Is.Not.Null);
            Assert.That(settings.TransferAnimationDest, Is.Not.Null);
        }
    }
}
#endif // ENABLE_FBX_RECORDER