using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections;
using FbxExporters.Editor;

namespace FbxExporters.UnitTests
{
    public class AnimationComponentTestDataClass
    {
        static float [] m_keytimes1 = new float [3] {1f, 2f, 3f};
        static float [] m_keyvalues1 = new float [3] {0f, 100f, 0f};
        static float [] m_keyvalues2 = new float [3] {1f, 100f, 1f};

        public static IEnumerable TestCases {
            get {
                yield return new TestCaseData (m_keytimes1, m_keyvalues2, typeof(Transform), "m_LocalScale.x").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues2, typeof(Transform), "m_LocalScale.y").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues2, typeof(Transform), "m_LocalScale.z").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues1, typeof(Transform), "m_LocalRotation.x").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues1, typeof(Transform), "m_LocalRotation.y").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues1, typeof(Transform), "m_LocalRotation.z").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues1, typeof(Transform), "m_LocalPosition.x").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues1, typeof(Transform), "m_LocalPosition.y").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues1, typeof(Transform), "m_LocalPosition.z").Returns (1);
            }
        }
    }

    [TestFixture]
    public class FbxAnimationTest : ExporterTestBase
    {
        protected void AnimClipTest (AnimationClip animClipExpected, AnimationClip animClipActual)
        {
            Assert.That (animClipActual.name, Is.EqualTo(animClipExpected.name));
            Assert.That (animClipActual.legacy, Is.EqualTo(animClipExpected.legacy));
            Assert.That (animClipActual.isLooping, Is.EqualTo(animClipExpected.isLooping));
            Assert.That (animClipActual.wrapMode, Is.EqualTo(animClipExpected.wrapMode));

            // TODO: Uni-34489
            Assert.That (animClipActual.length, Is.EqualTo(animClipExpected.length).Within (Mathf.Epsilon), "animClip length doesn't match");
        }

        protected void AnimCurveTest (float [] keyTimesExpected, float [] keyValuesExpected, AnimationCurve animCurveActual)
        {
            int numKeysExpected = keyTimesExpected.Length;

            // TODO : Uni-34492 number of keys don't match
            Assert.That (animCurveActual.length, Is.EqualTo(numKeysExpected), "animcurve number of keys doesn't match");

            //check imported animation against original
            Assert.That(new ListMapper(animCurveActual.keys).Property ("time"), Is.EqualTo(keyTimesExpected), "key time doesn't match");
            Assert.That(new ListMapper(animCurveActual.keys).Property ("value"), Is.EqualTo(keyValuesExpected), "key value doesn't match");

            return ;
        }

        [TearDown]
        public override void Term ()
        {
            return;
        }

        [Test, TestCaseSource (typeof (AnimationComponentTestDataClass), "TestCases")]
        public int SinglePropertyLegacyAnimTest (float [] keytimes, float [] keyvalues, System.Type propertyType, string propertyName )
        {
            string path = GetRandomFbxFilePath ();

            // TODO: add extra parent so that we can test export/import of transforms
            GameObject goRoot = new GameObject ();
            goRoot.name = "root_"+propertyName;

            GameObject goModel = new GameObject ();
            goModel.name = "model_"+propertyName;
            goModel.transform.parent = goRoot.transform;

            Animation animOrig = goModel.AddComponent (typeof (Animation)) as Animation;

            int numKeys = Mathf.Min(keytimes.Length, keyvalues.Length);

            // initialize keys
            Keyframe [] keys = new Keyframe [numKeys];

            for (int idx = 0; idx < numKeys; idx++)
            {
                keys[idx].time  = keytimes [idx];
                keys[idx].value = keyvalues [idx];
            }

            AnimationCurve animCurveOrig = new AnimationCurve (keys);

            AnimationClip animClipOriginal = new AnimationClip ();

            animClipOriginal.legacy = true;
            animClipOriginal.name = "anim_" + propertyName;
                
            animClipOriginal.SetCurve ("", propertyType, propertyName, animCurveOrig);

            animOrig.AddClip (animClipOriginal, "anim_" + propertyName );

            //export the object
            var exportedFilePath = ModelExporter.ExportObject (path, goRoot);
            Assert.That (exportedFilePath, Is.EqualTo(path));

            // TODO: Uni-34492 change importer settings of (newly exported model) 
            // so that it's not resampled and it is legacy animation
            {
                ModelImporter modelImporter = AssetImporter.GetAtPath (path) as ModelImporter;
                modelImporter.resampleCurves = false;
                AssetDatabase.ImportAsset (path);
                modelImporter.animationType = ModelImporterAnimationType.Legacy;
                AssetDatabase.ImportAsset (path);
            }

            //acquire imported object from exported file
            Object[] goAssetImported = AssetDatabase.LoadAllAssetsAtPath (path);
            Assert.That(goAssetImported, Is.Not.Null);

            // TODO : configure object so that it imports w Legacy Animation

            AnimationClip animClipImported = null;
            foreach (Object o in goAssetImported)
            {
                animClipImported = o as AnimationClip;
                if (animClipImported) break;
            }
            Assert.That (animClipImported, Is.Not.Null);

            // TODO : configure import settings so we don't need to force legacy
            animClipImported.legacy = true;

            // check clip properties match
            AnimClipTest (animClipOriginal, animClipImported);
                            
            // check animCurve & keys
            int result = 0;

            foreach (EditorCurveBinding curveBinding in AnimationUtility.GetCurveBindings (animClipImported)) 
            {
                AnimationCurve animCurveImported = AnimationUtility.GetEditorCurve (animClipImported, curveBinding);
                Assert.That(animCurveImported, Is.Not.Null);

                AnimCurveTest (keytimes, keyvalues, animCurveImported);

                result++;
            }

            return result;
        }
    }
}
