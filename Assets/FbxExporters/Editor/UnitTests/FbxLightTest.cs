using UnityEngine;
using UnityEditor;
using NUnit.Framework;

namespace FbxExporters.UnitTests
{
    public class FbxLightTest : ExporterTestBase
    {
        [Test]
        public void AnimationWithLightTest()
        {
            string filename = GetRandomFbxFilePath();
            GameObject go = new GameObject();
            go.name = "original";
            Light light = go.AddComponent(typeof(Light)) as Light;
            Animation anim = go.AddComponent(typeof(Animation)) as Animation;

            Keyframe[] keys = new Keyframe[3];
            keys[0] = new Keyframe(0.0f, 0.25f);
            keys[1] = new Keyframe(1.0f, 0.5f);
            keys[2] = new Keyframe(2.0f, 1.0f);

            AnimationCurve curve = new AnimationCurve(keys);

            AnimationClip clip = new AnimationClip();

            clip.legacy = true;

            clip.SetCurve("", typeof(Light), "m_Intensity", curve);

            anim.AddClip(clip, "test");
            
            //export the object
            var exported = FbxExporters.Editor.ModelExporter.ExportObject(filename, go);

            Assert.That(exported, Is.EqualTo(filename));

            // TODO: Uni-34492 change importer settings of (newly exported model) 
            // so that it's not resampled and it is legacy animation
            {
                ModelImporter modelImporter = AssetImporter.GetAtPath(filename) as ModelImporter;
                Assert.That(modelImporter, Is.Not.Null);
                modelImporter.resampleCurves = false;
                AssetDatabase.ImportAsset(filename);
                modelImporter.animationType = ModelImporterAnimationType.Legacy;
                AssetDatabase.ImportAsset(filename);
            }

            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(filename);

            AnimationClip exportedClip = null;
            foreach (Object o in objects)
            {
                exportedClip = o as AnimationClip;
                if (exportedClip != null) break;
            }

            exportedClip.legacy = true;

            Assert.IsNotNull(exportedClip);
            EditorCurveBinding exportedEditorCurveBinding = AnimationUtility.GetCurveBindings(exportedClip)[0];

            AnimationCurve exportedCurve = AnimationUtility.GetEditorCurve(exportedClip, exportedEditorCurveBinding);

            Assert.That(exportedCurve.keys.Length, Is.EqualTo(keys.Length));

            Debug.Log(AnimationUtility.GetEditorCurve(clip, AnimationUtility.GetCurveBindings(clip)[0]).keys[1].value);
            Debug.Log(AnimationUtility.GetEditorCurve(exportedClip, AnimationUtility.GetCurveBindings(exportedClip)[0]).keys[1].value);

            for (int i = 0; i < exportedCurve.keys.Length; i++)
            {
                Assert.That(exportedCurve.keys[i].time == keys[i].time);
                Assert.That(exportedCurve.keys[i].value == keys[i].value);
            }
        }
    }
}