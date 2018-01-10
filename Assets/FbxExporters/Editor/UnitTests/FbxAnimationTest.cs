using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using FbxExporters.Editor;
using System.Linq;

namespace FbxExporters.UnitTests
{
    using CustomExtensions;

    public class AnimationTestDataClass
    {
        static float [] m_keytimes1 = new float [3] { 1f, 2f, 3f };
        static float [] m_keyvalues1 = new float [3] { 0f, 100f, 0f };
        static float [] m_keyvalues2 = new float [3] { 1f, 100f, 1f };
        static Vector3 [] m_keyEulerValues3 = new Vector3 [3] { new Vector3 (0f, 80f, 0f), new Vector3 (80f, 0f, 0f), new Vector3 (0f, 0f, 80f) };
        static Vector3 [] m_keyEulerValues4 = new Vector3 [3] { new Vector3 (0f, 270f, 0f), new Vector3 (270f, 0f, 0f), new Vector3 (0f, 0f, 270f) };

        public static IEnumerable TestCases1 {
            get {
                yield return new TestCaseData (m_keytimes1, m_keyvalues2, typeof (Transform), "m_LocalScale.x").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues2, typeof (Transform), "m_LocalScale.y").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues2, typeof (Transform), "m_LocalScale.z").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues1, typeof (Transform), "m_LocalPosition.x").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues1, typeof (Transform), "m_LocalPosition.y").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues1, typeof (Transform), "m_LocalPosition.z").Returns (1);
            }
        }
        public static IEnumerable TestCases2 {
            get {
                yield return new TestCaseData (m_keytimes1, m_keyEulerValues3, typeof (Transform), new string [4] { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" } ).Returns (3);
            }
        }
        // specify gimbal conditions for rotation
        public static IEnumerable TestCases3 {
            get {
                yield return new TestCaseData (m_keytimes1, m_keyEulerValues4, typeof (Transform), new string [4] { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" } ).Returns (3);
            }
        }
    }

    [TestFixture]
    public class FbxAnimationTest : ExporterTestBase
    {
        // map imported clips with alt property name to property name used for instantiated prefabs
        protected static Dictionary<string, string> MapAltPropertyName = new Dictionary<string, string> {
            { "localEulerAnglesRaw.x", "m_LocalRotation.x" },
            { "localEulerAnglesRaw.y", "m_LocalRotation.y" },
            { "localEulerAnglesRaw.z", "m_LocalRotation.z" },
        };

        [TearDown]
        public override void Term ()
        {
            // NOTE: comment out the next line to leave temporary FBX files on disk
            base.Term ();
        }

        protected void AnimClipTest (AnimationClip animClipExpected, AnimationClip animClipActual)
        {
            Assert.That (animClipActual.name, Is.EqualTo (animClipExpected.name));
            Assert.That (animClipActual.legacy, Is.EqualTo (animClipExpected.legacy));
            Assert.That (animClipActual.isLooping, Is.EqualTo (animClipExpected.isLooping));
            Assert.That (animClipActual.wrapMode, Is.EqualTo (animClipExpected.wrapMode));

            // TODO: Uni-34489
            Assert.That (animClipActual.length, Is.EqualTo (animClipExpected.length).Within (Mathf.Epsilon), "animClip length doesn't match");
        }

        protected void AnimCurveTest (float [] keyTimesExpected, float [] keyValuesExpected, AnimationCurve animCurveActual, string message)
        {
            int numKeysExpected = keyTimesExpected.Length;

            // TODO : Uni-34492 number of keys don't match
            Assert.That (animCurveActual.length, Is.EqualTo(numKeysExpected), "animcurve number of keys doesn't match");

            //check imported animation against original
            Assert.That(new ListMapper(animCurveActual.keys).Property ("time"), Is.EqualTo(keyTimesExpected), string.Format("{0} key time doesn't match",message));
            Assert.That(new ListMapper(animCurveActual.keys).Property ("value"), Is.EqualTo(keyValuesExpected), string.Format("{0} key value doesn't match", message));

            return ;
        }

        public class KeyData
        {
            public float [] keyTimesInSeconds;
            public System.Type propertyType;

            public virtual int NumKeys { get { return 0; } }
            public virtual int NumComponents { get { return 0; } }
            public virtual float[] GetKeyValues(int id) { return null; }
            public virtual float [] GetAltKeyValues (int id) { return GetKeyValues(id); }
            public virtual string GetComponentName (int id) { return null; }
            public virtual int FindComponent (string name) { return -1; }

        }

        public class ComponentKeyData : KeyData
        {
            public string propertyName;
            public float [] keyFloatValues;

            public override int     NumKeys { get { return Mathf.Min (keyTimesInSeconds.Length, keyFloatValues.Length); } }
            public override int     NumComponents { get { return 1; } }
            public override float[] GetKeyValues (int id) { return keyFloatValues; }
            public override string  GetComponentName (int id) { return propertyName; }
            public override int     FindComponent (string name) { return (name == propertyName) ? 0 : -1; }
        }

        public class QuaternionKeyData : KeyData
        {
            public string[] propertyNames;
            public Vector3 [] keyEulerValues;

            public override int NumKeys { get { return Mathf.Min (keyTimesInSeconds.Length, keyEulerValues.Length); } }
            public override int NumComponents { get { return propertyNames.Length; } }
            public override float [] GetKeyValues (int id)
            {
                return (from e in keyEulerValues select Quaternion.Euler(e)[id]).ToArray ();
            }
            public override float [] GetAltKeyValues (int id)
            {
                return (from e in keyEulerValues select e[id]).ToArray ();
            }

            public override string GetComponentName (int id) { return propertyNames[id]; }
            public override int FindComponent (string name)
            {
                return System.Array.IndexOf (propertyNames, name);
            }
        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases1")]
        public int SimplePropertyAnimTest (float [] keyTimesInSeconds, float [] keyValues, System.Type propertyType, string componentName)
        {
            KeyData keyData = new ComponentKeyData { propertyName = componentName, propertyType = propertyType, keyTimesInSeconds = keyTimesInSeconds, keyFloatValues = keyValues };

            return AnimTest (keyData, componentName);
        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases2")]
        public int QuaternionPropertyAnimTest (float [] keyTimesInSeconds, Vector3 [] keyValues, System.Type propertyType, string[] componentNames)
        {
            KeyData keyData = new QuaternionKeyData { propertyNames = componentNames, propertyType = propertyType, keyTimesInSeconds = keyTimesInSeconds, keyEulerValues = keyValues };

            return AnimTest (keyData, propertyType.ToString() + "_Quaternion");
        }

        [Ignore("Uni-34804 gimbal conditions")]
        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases3")]
        public int GimbalConditionsAnimTest (float [] keyTimesInSeconds, Vector3 [] keyValues, System.Type propertyType, string [] componentNames)
        {
            KeyData keyData = new QuaternionKeyData { propertyNames = componentNames, propertyType = propertyType, keyTimesInSeconds = keyTimesInSeconds, keyEulerValues = keyValues };

            return AnimTest (keyData, propertyType.ToString () + "_Quaternion");
        }

        public int AnimTest (KeyData keyData, string name)
        {
            string path = GetRandomFbxFilePath ();

            // TODO: add extra parent so that we can test export/import of transforms
            GameObject goRoot = new GameObject ();
            goRoot.name = "root_"+name;

            GameObject goModel = new GameObject ();
            goModel.name = "model_"+name;
            goModel.transform.parent = goRoot.transform;

            Animation animOrig = goModel.AddComponent (typeof (Animation)) as Animation;

            AnimationClip animClipOriginal = new AnimationClip ();

            animClipOriginal.legacy = true;
            animClipOriginal.name = "anim_" + name;

            for (int id = 0; id < keyData.NumComponents; id++)
            {
                // initialize keys
                Keyframe [] keys = new Keyframe [keyData.NumKeys];

                for (int idx = 0; idx < keyData.NumKeys; idx++) {
                    keys [idx].time = keyData.keyTimesInSeconds [idx];
                    keys [idx].value = keyData.GetKeyValues (id) [idx];
                }
                AnimationCurve animCurveOriginal = new AnimationCurve (keys);

                animClipOriginal.SetCurve ("", keyData.propertyType, keyData.GetComponentName(id), animCurveOriginal);
            }

            animOrig.AddClip (animClipOriginal, animClipOriginal.name);
            animOrig.clip = animClipOriginal;

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

                string altPropertyName;

                MapAltPropertyName.TryGetValue (curveBinding.propertyName, out altPropertyName);

                bool hasAltPropertyName = !string.IsNullOrEmpty (altPropertyName);

                if (!hasAltPropertyName)
                    altPropertyName = curveBinding.propertyName;

                int id = keyData.FindComponent (altPropertyName);

                if (id!=-1)
                {
                    AnimCurveTest (keyData.keyTimesInSeconds, hasAltPropertyName ? keyData.GetAltKeyValues(id) : keyData.GetKeyValues(id), animCurveImported, curveBinding.propertyName);
                    result++;
                }
            }

            return result;
        }
    }
}
