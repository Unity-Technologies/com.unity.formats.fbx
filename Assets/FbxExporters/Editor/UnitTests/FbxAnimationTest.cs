// NOTE: uncomment the next line to leave temporary FBX files on disk
// and create a imported object in the scene.
#define DEBUG_UNITTEST

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

    public enum RotationCurveType {
        kEuler=3,
        kQuaternion=4
    };

    public class AnimationTestDataClass
    {
        // TODO: remove items that become supported by exporter
        public static IEnumerable<System.Type> m_exceptionTypes = new List<System.Type> ()
        {
            typeof(MeshFilter),
            typeof(SkinnedMeshRenderer),
            typeof(Camera), // TODO: uncomment this to add unit tests
            typeof(Transform),  // NOTE: has it's own special tests
        };

        public static IEnumerable<System.Type> m_componentTypes = 
            typeof (Component).Assembly.GetTypes ().
            Where (t => typeof (Component).IsAssignableFrom (t) && 
                   ModelExporter.MapsToFbxObject.ContainsKey(t)).Except(m_exceptionTypes);

        public static string [] m_quaternionRotationNames = new string [4] { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
        public static string [] m_eulerRotationNames = new string [3] { "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z" };
        public static string [] m_translationNames = new string [3] { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z"};

        public static float [] m_keytimes1 = new float [3] { 1f, 2f, 3f };
        public static float [] m_keyFloatValues1 = new float [3] { 0f, 100f, 0f };
        public static float [] m_keyvalues2 = new float [3] { 1f, 100f, 1f };
        public static Vector3 [] m_keyEulerValues3 = new Vector3 [3] { new Vector3 (0f, 80f, 0f), new Vector3 (80f, 0f, 0f), new Vector3 (0f, 0f, 80f) };
        public static Vector3 [] m_keyEulerValues4 = new Vector3 [3] { new Vector3 (90f, 0f, 0f), new Vector3 (90f, 30f, 0f), new Vector3 (90f, 0f, 30f) };

        public static float [] m_keytimes5 = new float [5] { 0f, 30f, 60f, 90f, 120f };
        public static Vector3 [] m_keyPosValues5 = new Vector3 [5] { new Vector3 (5.078195f, 0.000915527f, 4.29761f), new Vector3 (0.81f, 0.000915527f, 10.59f), new Vector3 (-3.65f, 0.000915527f, 4.29761f), new Vector3 (0.81f, 0.000915527f, -3.37f), new Vector3 (5.078195f, 0.000915527f, 4.29761f) };
        public static Vector3 [] m_keyRotValues5 = new Vector3 [5] { new Vector3 (0f, 0f, 0f), new Vector3 (0f, -90f, 0f), new Vector3 (0f, -180f, 0f), new Vector3 (0f, -270f, 0f), new Vector3 (0f, -360f, 0f) };

        public static IEnumerable TestCases1 {
            get {
                yield return new TestCaseData (m_keytimes1, m_keyvalues2, typeof (Transform), "m_LocalScale.x").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues2, typeof (Transform), "m_LocalScale.y").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyvalues2, typeof (Transform), "m_LocalScale.z").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyFloatValues1, typeof (Transform), "m_LocalPosition.x").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyFloatValues1, typeof (Transform), "m_LocalPosition.y").Returns (1);
                yield return new TestCaseData (m_keytimes1, m_keyFloatValues1, typeof (Transform), "m_LocalPosition.z").Returns (1);
            }
        }
        public static IEnumerable TestCases2 {
            get {
                yield return new TestCaseData (m_keytimes1, m_keyEulerValues3, typeof (Transform), m_quaternionRotationNames ).Returns (3);
            }
        }
        // specify gimbal conditions for rotation
        public static IEnumerable TestCases3 {
            get {
                yield return new TestCaseData (m_keytimes1, m_keyEulerValues4, typeof (Transform), m_quaternionRotationNames ).Returns (3);
            }
        }
        // specify one of each component type
        public static IEnumerable TestCases4 { 
            get {
                foreach (var cType in m_componentTypes)
                    yield return new TestCaseData (cType).Returns(1);
            }
        }
        // specify continuous rotations
        public static IEnumerable ContinuousRotationTestCases {
            get {
                yield return new TestCaseData (RotationCurveType.kEuler /*use euler curve*/, m_keytimes5, m_keyPosValues5, m_keyRotValues5).Returns (6);
                // Uni-35616 doesn't work with quaternions; rotations don't exceed 180
                yield return new TestCaseData (RotationCurveType.kQuaternion /*use quaternion curve*/, m_keytimes5, m_keyPosValues5, m_keyRotValues5).Returns (6);
            }
        }

        // test key tangents
        public static IEnumerable KeyTangentsTestCases {
            get {
                yield return new TestCaseData (new float [3] { 0f, 4f, 5f }, new Vector3 [3] { new Vector3 (-100, 100, 0), new Vector3 (0f, 0.0f, 0f), new Vector3 (25f, 0f, 0f) }, new Vector3 [3] { new Vector3 (0, 0, 0), new Vector3 (0f, 0f, 16.9f), new Vector3 (0f, 0f, 0f) }).Returns (6);
            }
        }

        public static IEnumerable SkinnedMeshTestCases {
            get {
                yield return "Models/DefaultMale/Male_DyingHitFromBack_Blend_T3_Cut01_James.fbx";
            }
        }

        public static IEnumerable BlendShapeTestCases {
            get {
                yield return "FbxExporters/Editor/UnitTests/Models/blendshape.fbx";
                yield return "FbxExporters/Editor/UnitTests/Models/blendshape_with_skinning.fbx";
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
            #if (!DEBUG_UNITTEST)
            base.Term ();
            #endif
        }

       public class KeyData
        {
            public float [] keyTimesInSeconds;
            public System.Type componentType;
            public GameObject targetObject;

            public virtual int NumKeys { get { return 0; } }
            public virtual int NumProperties { get { return 0; } }
            public virtual float[] GetKeyValues(int id) { return null; }
            public virtual float [] GetAltKeyValues (int id) { return GetKeyValues(id); }
            public virtual string GetPropertyName (int id) { return null; }
            public virtual int GetIndexOf (string name) { return -1; }

        }

        public class ComponentKeyData : KeyData
        {
            public string propertyName;
            public float [] keyFloatValues;

            public override int     NumKeys { get { return Mathf.Min (keyTimesInSeconds.Length, keyFloatValues.Length); } }
            public override int     NumProperties { get { return 1; } }
            public override float[] GetKeyValues (int id) { return keyFloatValues; }
            public override string  GetPropertyName (int id) { return propertyName; }
            public override int     GetIndexOf (string name) { return (name == propertyName) ? 0 : -1; }
        }

        public class SingleKeyData : KeyData
        {
            public string[] propertyNames;
            public System.Single [] keyFloatValues;

            public override int NumKeys { get { return Mathf.Min (keyTimesInSeconds.Length, keyFloatValues.Length); } }
            public override int NumProperties { get { return propertyNames.Length; } }
            public override float [] GetKeyValues (int id) { return keyFloatValues; }
            public override string GetPropertyName (int id) { return propertyNames[id]; }
            public override int GetIndexOf (string name) { return System.Array.IndexOf (propertyNames, name); }
        }

        public class QuaternionKeyData : KeyData
        {
            public string[] propertyNames;
            public Vector3 [] keyEulerValues;

            public override int NumKeys { get { return Mathf.Min (keyTimesInSeconds.Length, keyEulerValues.Length); } }
            public override int NumProperties { get { return propertyNames.Length; } }
            public override float [] GetKeyValues (int id)
            {
                return (from e in keyEulerValues select Quaternion.Euler(e)[id]).ToArray ();
            }
            public override float [] GetAltKeyValues (int id)
            {
                return (from e in keyEulerValues select e[id]).ToArray ();
            }

            public override string GetPropertyName (int id) { return propertyNames[id]; }
            public override int GetIndexOf (string name)
            {
                return System.Array.IndexOf (propertyNames, name);
            }
        }

        public class TransformKeyData : KeyData
        {
            public RotationCurveType RotationType = RotationCurveType.kEuler;

            public string[] propertyNames;
            public Vector3 [] keyPosValues; 
            public Vector3 [] keyEulerValues; 
            private Quaternion [] keyQuatValues;

            public bool IsRotation(int id) { return id < (int)RotationType; }

            public override int NumKeys { get { return keyTimesInSeconds.Length; } }
            public override int NumProperties { get { return propertyNames.Length; } }
            public override float [] GetKeyValues (int id)
            {
                if (RotationType==RotationCurveType.kEuler) 
                    return GetAltKeyValues(id);

                // compute continous rotations
                if (keyQuatValues==null)
                {
                    keyQuatValues = new Quaternion[NumKeys];

                    for (int idx=0;  idx < NumKeys; idx++)
                    {
                        keyQuatValues[idx] = Quaternion.Euler(keyEulerValues[idx]);
                    }
                }

                float[] result = new float[NumKeys];

                for (int idx=0;  idx < NumKeys; idx++)
                {
                    if (IsRotation(id))
                    {
                        switch (RotationType)
                        {
                        case (RotationCurveType.kEuler):
                            result[idx] = keyEulerValues[idx][id];
                            break;
                        case (RotationCurveType.kQuaternion):
                            result[idx] = keyQuatValues[idx][id];
                            break;
                        }
                    }
                    else
                    {
                        result[idx] = keyPosValues[idx][id-(int)RotationType];
                    }
                }

                return result;
            }
            public override float [] GetAltKeyValues (int id)
            {
                float[] result = new float[NumKeys];

                for (int idx=0;  idx < NumKeys; idx++)
                {
                    // kQuaternion
                    //     0..3 quarternion XYZW
                    //     4..6 position XYZ
                    // kEuler
                    //     0..2 euler XYZ
                    //     3..5 position XYZ

                    if (IsRotation(id))
                    {
                        result[idx] = keyEulerValues[idx][id];
                    }
                    else
                    {
                        result[idx] = keyPosValues[idx][id-(int)RotationType];
                    }
                }

                return result;
            }

            public override string GetPropertyName (int id) { return propertyNames[id]; }
            public override int GetIndexOf (string name)
            {
                return System.Array.IndexOf (propertyNames, name);
            }
        }

        public class BasicKeyComparer : IComparer<Keyframe>
        {
            const float Epsilon = 0.00001f;

            public int CompareKeyValue(Keyframe a, Keyframe b)
            {
                bool result = true;

                result &= a.time.Equals(b.time);
                Debug.Log(string.Format("{2} a.time: {0}, b.time: {1}", a.time, b.time,result));

                result &= (Mathf.Abs(a.value - b.value) <= Epsilon);
                Debug.Log(string.Format("{2} a.value: {0}, b.value: {1}", a.value, b.value,result));

                return  result ? 0 : 1;
            }

            public virtual int Compare(Keyframe a, Keyframe b)
            {
                return CompareKeyValue(a,b);
            }
        }

        public class KeyTangentComparer : IComparer<Keyframe>
        {
            public int CompareKeyTangents(Keyframe a, Keyframe b)
            {
                bool result = true;

                result &= a.time.Equals(b.time);
                Debug.Log(string.Format("{2} a.time: {0}, b.time: {1}", a.time, b.time,result));

                // TODO : use AnimationUtility.GetLeftTangentMode 
                // requires reference to AnimationCurve and keyindex
                result &= (a.tangentMode == b.tangentMode);
                Debug.Log(string.Format("{2} a.tangentMode={0} b.tangentMode={1}", 
                    ((AnimationUtility.TangentMode)a.tangentMode).ToString(),
                    ((AnimationUtility.TangentMode)b.tangentMode).ToString(),result));

                return result ? 0 : 1;
            }

            public int Compare(Keyframe a, Keyframe b)
            {
                return CompareKeyTangents(a,b);
            }
        }

        class AnimTester
        {
            public FbxAnimationTest.KeyData keyData;
            public string testName;
            public string path;
            public IComparer<Keyframe> keyComparer;

            public int DoIt ()
            {
                return Main(keyData, testName, path);
            }

            public static GameObject CreateTargetObject (string name, System.Type componentType)
            {
                GameObject goModel = new GameObject ();
                goModel.name = "model_" + name;

                // check for component and add if missing
                var goComponent = goModel.GetComponent (componentType);
                if (!goComponent)
                    goModel.AddComponent (componentType);

                return goModel;
            }

            public int Main (KeyData keyData, string testName, string path)
            {
                if (!keyData.targetObject)
                    keyData.targetObject = CreateTargetObject (testName, keyData.componentType);

                Animation animOrig = keyData.targetObject.AddComponent (typeof (Animation)) as Animation;

                AnimationClip animClipOriginal = new AnimationClip ();
                var animCurvesOriginal = new AnimationCurve[keyData.NumProperties];
                    
                animClipOriginal.legacy = true;
                animClipOriginal.name = "anim_" + testName;

                for (int id = 0; id < keyData.NumProperties; id++) {
                    // initialize keys
                    Keyframe [] keys = new Keyframe [keyData.NumKeys];

                    for (int idx = 0; idx < keyData.NumKeys; idx++) {
                        keys [idx].time = keyData.keyTimesInSeconds [idx];
                        keys [idx].value = keyData.GetKeyValues (id) [idx];
                    }
                    animCurvesOriginal[id] = new AnimationCurve (keys);

                    animClipOriginal.SetCurve ("", keyData.componentType, keyData.GetPropertyName (id), animCurvesOriginal[id]);
                }

                animOrig.AddClip (animClipOriginal, animClipOriginal.name);
                animOrig.clip = animClipOriginal;

                // NOTE: when we first cached the curves the tangents wheren't set.
                foreach (EditorCurveBinding curveBinding in AnimationUtility.GetCurveBindings (animOrig.clip))
                {
                    int id = keyData.GetIndexOf (curveBinding.propertyName);
                    if (id==-1) continue;

                    animCurvesOriginal[id] = AnimationUtility.GetEditorCurve (animOrig.clip, curveBinding);
                }

                // TODO: add extra parent so that we can test export/import of transforms
                var goRoot = new GameObject ();
                goRoot.name = "Root_" + testName;
                keyData.targetObject.transform.parent = goRoot.transform;

                //export the object
                var exportedFilePath = ModelExporter.ExportObject (path, goRoot);
                Assert.That (exportedFilePath, Is.EqualTo (path));

                // TODO: Uni-34492 change importer settings of (newly exported model) 
                // so that it's not resampled and it is legacy animation
                {
                    ModelImporter modelImporter = AssetImporter.GetAtPath (path) as ModelImporter;
                    modelImporter.resampleCurves = false;
                    AssetDatabase.ImportAsset (path);
                    modelImporter.animationType = ModelImporterAnimationType.Legacy;
                    AssetDatabase.ImportAsset (path);
                }

                // create a scene GO so we can compare.
                #if DEBUG_UNITTEST
                GameObject prefabGO = AssetDatabase.LoadMainAssetAtPath (path) as GameObject;
                GameObject sceneGO = Object.Instantiate(prefabGO, keyData.targetObject.transform.localPosition, keyData.targetObject.transform.localRotation);
                sceneGO.name = "Imported_" + testName;
                #endif 

                //acquire imported object from exported file
                AnimationClip animClipImported = GetClipFromFbx (path);

                ClipPropertyTest (animClipOriginal, animClipImported);

                // check animCurve & keys
                int result = 0;

                // keyed localEulerAnglesRaw.z m_LocalRotation.z -1
                foreach (EditorCurveBinding curveBinding in AnimationUtility.GetCurveBindings (animClipImported)) {
                    AnimationCurve animCurveImported = AnimationUtility.GetEditorCurve (animClipImported, curveBinding);
                    Assert.That (animCurveImported, Is.Not.Null);

                    string altBinding;

                    MapAltPropertyName.TryGetValue (curveBinding.propertyName, out altBinding);

                    bool hasAltBinding = !string.IsNullOrEmpty (altBinding);

                    if (!hasAltBinding)
                        altBinding = curveBinding.propertyName;

                    int id = keyData.GetIndexOf (curveBinding.propertyName);

                    if (id == -1)
                        id = keyData.GetIndexOf (altBinding);

                    #if DEBUG_UNITTEST
                    Debug.Log(string.Format("animtest binding={0} altBinding={1} id={2}", curveBinding.propertyName, altBinding, id));
                    #endif

                    if (id != -1) {
                        // test against original data
                        KeysTest (keyData.keyTimesInSeconds, hasAltBinding ? keyData.GetAltKeyValues (id) : keyData.GetKeyValues (id), animCurveImported, curveBinding.propertyName);

                        // test against origin curve keys
                        KeysTest (animCurvesOriginal[id], animCurveImported, curveBinding.propertyName, keyComparer);
                        result++;
                    }
                }

                return result;
            }

            public static void ClipPropertyTest (AnimationClip animClipExpected, AnimationClip animClipActual)
            {
                // TODO: figure out why we get __preview__ on Windows
                Assert.That (animClipActual.name, Is.EqualTo (animClipExpected.name).Or.EqualTo("__preview__" + animClipExpected.name));
                Assert.That (animClipActual.legacy, Is.EqualTo (animClipExpected.legacy));
                Assert.That (animClipActual.isLooping, Is.EqualTo (animClipExpected.isLooping));
                Assert.That (animClipActual.wrapMode, Is.EqualTo (animClipExpected.wrapMode));

                // TODO: Uni-34489
                Assert.That (animClipActual.length, Is.EqualTo (animClipExpected.length).Within (Mathf.Epsilon), "animClip length doesn't match");
            }

            public static void KeysTest (AnimationCurve expectedAnimCurve, AnimationCurve actualAnimCurve, string message, IComparer<Keyframe> keyComparer = null)
            {
                if (keyComparer==null)
                    keyComparer = new BasicKeyComparer();
                
                Assert.That (actualAnimCurve.length, Is.EqualTo(expectedAnimCurve.length), "animcurve number of keys doesn't match");

                Assert.That(actualAnimCurve.keys, Is.EqualTo(expectedAnimCurve.keys).Using<Keyframe>(keyComparer), string.Format("{0} key doesn't match", message));
            }

            public static void KeysTest (float [] keyTimesExpected, float [] keyValuesExpected, AnimationCurve actualAnimCurve, string message, IComparer<Keyframe> keyComparer=null)
            {
                if (keyComparer==null)
                    keyComparer = new BasicKeyComparer();
                
                int numKeysExpected = keyTimesExpected.Length;

                // NOTE : Uni-34492 number of keys don't match
                Assert.That (actualAnimCurve.length, Is.EqualTo(numKeysExpected), "animcurve number of keys doesn't match");

                //check imported animation against original
                // NOTE: if I check the key values explicitly they match but when I compare using this ListMapper the float values
                // are off by 0.000005f; not sure why that happens.
                var keysExpected = new Keyframe[numKeysExpected];

                for (int idx = 0; idx < numKeysExpected; idx++)
                {
                    keysExpected[idx].time = keyTimesExpected[idx];
                    keysExpected[idx].value = keyValuesExpected[idx];
                }

                Assert.That(actualAnimCurve.keys, Is.EqualTo(keysExpected).Using<Keyframe>(keyComparer), string.Format("{0} key doesn't match", message));

                return ;
            }

            public static void CurveTest(AnimationCurve animCurveImported, AnimationCurve animCurveActual, string message)
            {
                // TODO : Uni-34492 number of keys don't match
                //Assert.That (animCurveActual.length, Is.EqualTo (animCurveImported.length), "animcurve number of keys doesn't match");

                var actualTimeKeys = new ListMapper (animCurveActual.keys).Property ("time");
                var actualValueKeys = new ListMapper (animCurveActual.keys).Property ("value");

                var importedTimeKeys = new ListMapper (animCurveImported.keys).Property ("time");
                var importedValueKeys = new ListMapper (animCurveImported.keys).Property ("value");

                //check imported animation against original
                Assert.That(actualTimeKeys, Is.EqualTo(importedTimeKeys), string.Format("{0} key time doesn't match",message));
                Assert.That(actualValueKeys, Is.EqualTo(importedValueKeys), string.Format("{0} key value doesn't match", message));
            }

            public static AnimationClip GetClipFromFbx(string path){
                //acquire imported object from exported file
                Object [] goAssetImported = AssetDatabase.LoadAllAssetsAtPath (path);
                Assert.That (goAssetImported, Is.Not.Null);

                // TODO : configure object so that it imports w Legacy Animation

                AnimationClip animClipImported = null;
                foreach (Object o in goAssetImported) {
                    animClipImported = o as AnimationClip;
                    if (animClipImported) break;
                }
                Assert.That (animClipImported, Is.Not.Null, "expected imported clip");

                // TODO : configure import settings so we don't need to force legacy
                animClipImported.legacy = true;

                return animClipImported;
            }
        }

        [Ignore("Uni-34804 gimbal conditions, and Uni-34492 number of keys don't match")]
        [Test, TestCaseSource (typeof (AnimationTestDataClass), "SkinnedMeshTestCases")]
        public void LegacySkinnedMeshAnimTest (string fbxPath)
        {
            fbxPath = FindPathInUnitTests (fbxPath);
            Assert.That (fbxPath, Is.Not.Null);

            // add fbx to scene
            GameObject originalFbxObj = AssetDatabase.LoadMainAssetAtPath("Assets/" + fbxPath) as GameObject;
            Assert.IsNotNull (originalFbxObj);
            GameObject originalGO = GameObject.Instantiate (originalFbxObj);
            Assert.IsTrue (originalGO);

            // get clip
            AnimationClip animClipOriginal = originalGO.GetComponentInChildren<Animation>().clip;
            Assert.That (animClipOriginal, Is.Not.Null);

            // export fbx
            // get GameObject
            string filename = GetRandomFbxFilePath();
            var exportedFilePath = ModelExporter.ExportObject (filename, originalGO);
            Assert.That (exportedFilePath, Is.EqualTo (filename));

            // TODO: Uni-34492 change importer settings of (newly exported model) 
            // so that it's not resampled and it is legacy animation
            {
                ModelImporter modelImporter = AssetImporter.GetAtPath (filename) as ModelImporter;
                modelImporter.resampleCurves = false;
                AssetDatabase.ImportAsset (filename);
                modelImporter.animationType = ModelImporterAnimationType.Legacy;
                AssetDatabase.ImportAsset (filename);
            }

            var animClipImported = AnimTester.GetClipFromFbx (filename);

            // check clip properties match
            AnimTester.ClipPropertyTest (animClipOriginal, animClipImported);

            foreach (EditorCurveBinding curveBinding in AnimationUtility.GetCurveBindings (animClipOriginal)) {
                foreach(EditorCurveBinding impCurveBinding in AnimationUtility.GetCurveBindings (animClipImported)) {

                    // only compare if the path and property names match
                    if (curveBinding.path != impCurveBinding.path || curveBinding.propertyName != impCurveBinding.propertyName) {
                        continue;
                    }

                    AnimationCurve animCurveOrig = AnimationUtility.GetEditorCurve (animClipOriginal, curveBinding);
                    Assert.That (animCurveOrig, Is.Not.Null);

                    AnimationCurve animCurveImported = AnimationUtility.GetEditorCurve (animClipImported, impCurveBinding);
                    Assert.That (animCurveImported, Is.Not.Null);

                    AnimTester.CurveTest(animCurveImported, animCurveOrig, curveBinding.propertyName);
                }
            }


        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases1")]
        public int SimplePropertyAnimTest (float [] keyTimesInSeconds, float [] keyValues, System.Type componentType, string componentName)
        {
            KeyData keyData = new ComponentKeyData { propertyName = componentName, componentType = componentType, keyTimesInSeconds = keyTimesInSeconds, keyFloatValues = keyValues };

            var tester = new AnimTester {keyData=keyData, testName=componentName, path=GetRandomFbxFilePath ()};
            return tester.DoIt();
        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases2")]
        public int QuaternionPropertyAnimTest (float [] keyTimesInSeconds, Vector3 [] keyValues, System.Type componentType, string[] componentNames)
        {
            KeyData keyData = new QuaternionKeyData { propertyNames = componentNames, componentType = componentType, keyTimesInSeconds = keyTimesInSeconds, keyEulerValues = keyValues };

            var tester = new AnimTester {keyData=keyData, testName=(componentType.ToString() + "_Quaternion"), path=GetRandomFbxFilePath ()};
            return tester.DoIt();
        }

        [Ignore("Uni-34804 gimbal conditions")]
        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases3")]
        public int GimbalConditionsAnimTest (float [] keyTimesInSeconds, Vector3 [] keyValues, System.Type componentType, string [] componentNames)
        {
            KeyData keyData = new QuaternionKeyData { propertyNames = componentNames, componentType = componentType, keyTimesInSeconds = keyTimesInSeconds, keyEulerValues = keyValues };

            var tester = new AnimTester {keyData=keyData, testName=componentType.ToString () + "_Gimbal", path=GetRandomFbxFilePath ()};
            return tester.DoIt();
        }

        [Description("Uni-35616 continuous rotations")]
        [Test, TestCaseSource (typeof (AnimationTestDataClass), "ContinuousRotationTestCases")]
        public int ContinuousRotationAnimTest (RotationCurveType rotCurveType, float [] keyTimesInSeconds, Vector3 [] keyPosValues, Vector3 [] keyEulerValues)
        {
            System.Type componentType = typeof(Transform);

            string[] propertyNames = null;
            string testName = componentType.ToString () + "_ContinuousRotations";
                
            switch (rotCurveType)
            {
            case RotationCurveType.kEuler:
                testName += "_Euler";
                propertyNames=AnimationTestDataClass.m_eulerRotationNames.Concat(AnimationTestDataClass.m_translationNames).ToArray();
                break;
            case RotationCurveType.kQuaternion:
                testName += "_Quaternion";
                propertyNames=AnimationTestDataClass.m_quaternionRotationNames.Concat(AnimationTestDataClass.m_translationNames).ToArray();
                break;
            }

            KeyData keyData = new TransformKeyData { RotationType = rotCurveType, propertyNames = propertyNames, componentType = componentType, keyTimesInSeconds = keyTimesInSeconds, keyPosValues = keyPosValues, keyEulerValues = keyEulerValues };

            var tester = new AnimTester {keyData=keyData, testName=testName, path=GetRandomFbxFilePath ()};
            return tester.DoIt();
        }

        [Description("Uni-35935 key tangents")]
        [Test, TestCaseSource (typeof (AnimationTestDataClass), "KeyTangentsTestCases")]
        public int KeyTangentsAnimTest (float [] keyTimesInSeconds, Vector3 [] keyPosValues, Vector3 [] keyRotValues)
        {
            System.Type componentType = typeof(Transform);

            if (keyRotValues == null)
            {
                keyRotValues = new Vector3[keyPosValues.Length];
            }
                
            string[] propertyNames = null;
            string testName = componentType.ToString () + "_KeyTangents";
            RotationCurveType rotCurveType = RotationCurveType.kEuler;
                
            testName += "_Euler";
            propertyNames = AnimationTestDataClass.m_eulerRotationNames.Concat(AnimationTestDataClass.m_translationNames).ToArray();

            KeyData keyData = new TransformKeyData { RotationType = rotCurveType, propertyNames = propertyNames, componentType = componentType, keyTimesInSeconds = keyTimesInSeconds, keyPosValues = keyPosValues, keyEulerValues = keyRotValues };

            var tester = new AnimTester {keyData=keyData, testName=testName, path=GetRandomFbxFilePath (), keyComparer=new KeyTangentComparer()};
            return tester.DoIt();
        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases4")]
        public int ComponentSingleAnimTest (System.Type componentType)
        {
            Debug.Log (string.Format ("ComponentAnimTest {0}", componentType.ToString()));

            if (!ModelExporter.MapsToFbxObject.ContainsKey(componentType))
            {
                Debug.Log (string.Format ("skipping {0}; fbx export not supported", componentType.ToString()));
                return 1;                
            }

            string testName = "ComponentSingleAnimTest_" + componentType.ToString ();
            GameObject targetObject = AnimTester.CreateTargetObject (testName, componentType);

            string [] propertyNames = 
                (from b in AnimationUtility.GetAnimatableBindings (targetObject, targetObject) 
                 where b.type==componentType select b.propertyName).ToArray();

            if (propertyNames.Length == 0)
            {
                Debug.Log (string.Format ("skipping {0}; no animatable Single properties found", componentType.ToString()));
                return 1;                
            }

            float [] keyTimesInSeconds = AnimationTestDataClass.m_keytimes1;
            float [] keyValues = AnimationTestDataClass.m_keyFloatValues1;

            KeyData keyData = new SingleKeyData { propertyNames = propertyNames, componentType = componentType, keyTimesInSeconds = keyTimesInSeconds, keyFloatValues = keyValues, targetObject = targetObject };

            var tester = new AnimTester {keyData=keyData, testName=testName, path=GetRandomFbxFilePath ()};
            return tester.DoIt() <= propertyNames.Length ? 1 : 0;
        }
    }
}
