using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections;
using FbxExporters.Editor;

namespace FbxExporters.UnitTests
{
    public class AnimationComponentTestDataClass
    {
        static float[] m_keydata1 = new float [3 * 2] {1.0f, 0f, 2.0f, 100.0f, 3.0f, 0.0f};

    	public static IEnumerable TestCases {
    		get {
                yield return new TestCaseData (m_keydata1, typeof(Transform), "m_LocalPosition.x").Returns (1);
    		}
    	}
    }

    [TestFixture]
    public class FbxAnimationTest : ExporterTestBase
    {
        protected static void DebugLogAnimCurve (AnimationCurve animCurve)
        {
        	int idx = 0;
        	foreach (var key in animCurve.keys) {
        		Debug.Log (string.Format ("key[{0}] {1} {2}", idx++, key.time, key.value));
        	}
        }

        [Test, TestCaseSource (typeof (AnimationComponentTestDataClass), "TestCases")]
        public int SinglePropertyLegacyAnimTest (float [] keydata, System.Type propertyType, string propertyName )
        {
        	string filePath = GetRandomFbxFilePath ();
        	GameObject go = new GameObject ();
            go.name = "orig_"+propertyName;
        	Animation animOrig = go.AddComponent (typeof (Animation)) as Animation;

            int expectedNumKeys = keydata.Length / 2;

            // initialize keys
        	Keyframe [] keys = new Keyframe [expectedNumKeys];

            for (int idx = 0; idx < expectedNumKeys; idx++)
            {
                keys[idx].time  = keydata [(idx*2)+0];
                keys[idx].value = keydata [(idx*2)+1];
            }

        	AnimationCurve animCurveOrig = new AnimationCurve (keys);

        	AnimationClip animClipOrig = new AnimationClip ();

        	animClipOrig.legacy = true;

        	animClipOrig.SetCurve ("", propertyType, propertyName, animCurveOrig);

        	animOrig.AddClip (animClipOrig, "anim_" + propertyName );

        	//export the object
        	var exportedFilePath = ModelExporter.ExportObject (filePath, go);
            Assert.AreEqual (exportedFilePath, filePath);

        	//acquire imported object from exported file
        	Object[] goAssetImported = AssetDatabase.LoadAllAssetsAtPath (filePath);
            Assert.IsNotNull (goAssetImported);

            // TODO : configure object so that it imports w Legacy Animation

            AnimationClip animClipImported = null;
            foreach (Object o in goAssetImported)
            {
                animClipImported = o as AnimationClip;
                if (animClipImported) break;
            }
            Assert.IsNotNull (animClipImported);

            // TODO : configure import settings so we don't need to force legacy
            animClipImported.legacy = true;
            Assert.AreEqual (animClipImported.legacy, true);

            {
                var go2 = Object.Instantiate (goAssetImported [0]) as GameObject;
                Assert.IsNotNull (go2);
                go2.name = "imported_" + propertyName;
                Animation anim2 = go2.AddComponent (typeof (Animation)) as Animation;
                anim2.AddClip (animClipImported, "anim2_" + propertyName );
            }


            // TODO : check clip properties match

            // check animCurve & keys
            int result = 0;

            foreach (EditorCurveBinding curveBinding in AnimationUtility.GetCurveBindings (animClipImported)) 
            {
                AnimationCurve animCurveImported = AnimationUtility.GetEditorCurve (animClipImported, curveBinding);
                Assert.IsNotNull (animCurveImported);

                DebugLogAnimCurve (animCurveImported);

                Assert.AreEqual (expectedNumKeys, animCurveImported.length);

                //check imported animation against original
                int idx = 0;
                foreach (var key in animCurveImported.keys) 
                {
                    Assert.AreEqual (key.time, keydata [(idx * 2) + 0]);
                    Assert.AreEqual (key.value, keydata [(idx * 2) + 1]);

                    idx++;
                }

                result++;
            }

            return result;
        }
    }
}
