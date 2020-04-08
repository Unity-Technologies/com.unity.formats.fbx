// NOTE: uncomment the next line to leave temporary FBX files on disk
// and create a imported object in the scene.
//#define DEBUG_UNITTEST

using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEditor.Formats.Fbx.Exporter.CustomExtensions;

namespace FbxExporter.UnitTests
{
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
            typeof(Transform),              // NOTE: has it's own special tests
        };

        public static IEnumerable<System.Type> m_componentTypes = 
            typeof (Component).Assembly.GetTypes ().
            Where (t => typeof (Component).IsAssignableFrom (t) && 
                   ModelExporter.MapsToFbxObject.ContainsKey(t)).Except(m_exceptionTypes);

        public static string [] m_rotationQuaternionNames = new string [4] { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
        public static string [] m_rotationEulerNames = new string [3] { "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z" };
        public static string [] m_translationNames = new string [3] { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z"};

        public static IEnumerable TransformIndependantComponentTestCases {
            get {
                yield return new TestCaseData (new float [3] { 1f, 2f, 3f }, new float [3] { 1f, 100f, 1f }, typeof (Transform), "m_LocalScale.x").Returns (1);
                yield return new TestCaseData (new float [3] { 1f, 2f, 3f }, new float [3] { 1f, 100f, 1f }, typeof (Transform), "m_LocalScale.y").Returns (1);
                yield return new TestCaseData (new float [3] { 1f, 2f, 3f }, new float [3] { 1f, 100f, 1f }, typeof (Transform), "m_LocalScale.z").Returns (1);
                yield return new TestCaseData (new float [3] { 1f, 2f, 3f }, new float [3] { 0f, 100f, 0f }, typeof (Transform), "m_LocalPosition.x").Returns (1);
                yield return new TestCaseData (new float [3] { 1f, 2f, 3f }, new float [3] { 0f, 100f, 0f }, typeof (Transform), "m_LocalPosition.y").Returns (1);
                yield return new TestCaseData (new float [3] { 1f, 2f, 3f }, new float [3] { 0f, 100f, 0f }, typeof (Transform), "m_LocalPosition.z").Returns (1);
            }
        }
        public static IEnumerable QuaternionTestCases {
            get {
                yield return new TestCaseData (new float [3] { 1f, 2f, 3f }, new Vector3 [3]{new Vector3 (0f, 80f, 0f), new Vector3 (80f, 0f, 0f),new Vector3 (0f, 0f, 80f)}, typeof (Transform), m_rotationQuaternionNames ).Returns (3);
            }
        }
        // specify gimbal conditions for rotation
        public static IEnumerable GimbalTestCases {
            get {
                yield return new TestCaseData (new float [3] { 1f, 2f, 3f }, new Vector3 [3] { new Vector3 (90f, 0f, 0f), new Vector3 (90f, 30f, 0f), new Vector3 (90f, 0f, 30f) }, typeof (Transform), m_rotationQuaternionNames ).Returns (3);
            }
        }
        // specify one of each component type
        public static IEnumerable ComponentTestCases { 
            get {
                foreach (var cType in m_componentTypes)
                    yield return new TestCaseData (cType).Returns(1);
            }
        }
        // specify continuous rotations
        public static IEnumerable ContinuousRotationTestCases {
            get {
                yield return new TestCaseData (RotationCurveType.kEuler, new float [5] { 0f, 30f, 60f, 90f, 120f }, new Vector3 [5]{new Vector3 (0f, 0f, 0f), new Vector3 (0f, -90f, 0f), new Vector3 (0f, -180f, 0f), new Vector3 (0f, -270f, 0f), new Vector3 (0f, -360f, 0f)}).Returns (3);
                yield return new TestCaseData (RotationCurveType.kQuaternion, new float [5] { 0f, 30f, 60f, 90f, 120f }, new Vector3 [5]{new Vector3 (0f, 0f, 0f), new Vector3 (0f, -90f, 0f), new Vector3 (0f, -180f, 0f), new Vector3 (0f, -270f, 0f), new Vector3 (0f, -360f, 0f)}).Returns (3);
            }
        }

        public static IEnumerable SkinnedMeshTestCases {
            get {
                yield return "Models/DefaultMale/Male_DyingHitFromBack_Blend_T3_Cut01_James.fbx";
            }
        }

        public static IEnumerable BlendShapeTestCases {
            get {
                yield return "Models/blendshape.fbx";
                yield return "Models/blendshape_with_skinning.fbx";
            }
        }

        public static IEnumerable AnimOnlyTestCases {
            get {
                yield return new TestCaseData ("Models/DefaultMale/DefaultMale.prefab");
            }
        }

        public static IEnumerable RotationCurveTestCases {
            get {
                yield return new TestCaseData ("Models/Player/Player.prefab");
            }
        }
    }

    [TestFixture]
    public class FbxAnimationTest : ExporterTestBase
    {
        // map imported clips with alt property name to property name used for instantiated prefabs
        protected static Dictionary<string, string> MapEulerToQuaternionPropertyName = new Dictionary<string, string> {
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
            public float [] keyTimes;
            public System.Type componentType;
            public GameObject targetObject;
            public object importSettings;
            public bool compareOriginalKeys = false;    // should we compare against the original data or evaluate the 
                                                        // actual curve against the original curve?

            public virtual int NumKeys { get { return 0; } }
            public virtual int NumProperties { get { return 0; } }
            public virtual float[] GetKeyValues(int id) { return null; }
            public virtual float [] GetAltKeyValues (int id) { return GetKeyValues(id); }
            public virtual string GetPropertyName (int id) { return null; }
            public virtual int GetIndexOf (string name) { return -1; }

        }

        public class PropertyKeyData : KeyData
        {
            public string propertyName;
            public float [] keyFloatValues;

            public override int     NumKeys { get { return Mathf.Min (keyTimes.Length, keyFloatValues.Length); } }
            public override int     NumProperties { get { return 1; } }
            public override float[] GetKeyValues (int id) { return keyFloatValues; }
            public override string  GetPropertyName (int id) { return propertyName; }
            public override int     GetIndexOf (string name) { return (name == propertyName) ? 0 : -1; }
        }

        public class MultiPropertyKeyData : KeyData
        {
            public string[] propertyNames;
            public System.Single [] keyValues;

            public override int NumKeys { get { return Mathf.Min (keyTimes.Length, keyValues.Length); } }
            public override int NumProperties { get { return propertyNames.Length; } }
            public override float [] GetKeyValues (int id) { return keyValues; }
            public override string GetPropertyName (int id) { return propertyNames[id]; }
            public override int GetIndexOf (string name) { return System.Array.IndexOf (propertyNames, name); }
        }

        public class QuaternionKeyData : KeyData
        {
            public string[] propertyNames;
            public Vector3 [] keyEulerValues;

            public override int NumKeys { get { return Mathf.Min (keyTimes.Length, keyEulerValues.Length); } }
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

            public override int NumKeys { get { return keyTimes.Length; } }
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
                #if DEBUG_UNITTEST
                Debug.Log(string.Format("{2} a.time: {0}, b.time: {1}", a.time, b.time,result));
                #endif

                result &= (Mathf.Abs(a.value - b.value) <= Epsilon);
                #if DEBUG_UNITTEST
                Debug.Log(string.Format("{2} a.value: {0}, b.value: {1}", a.value, b.value,result));
                #endif

                return  result ? 0 : 1;
            }

            public virtual int Compare(Keyframe a, Keyframe b)
            {
                return CompareKeyValue(a,b);
            }
        }

        public class AnimTester
        {
            public FbxAnimationTest.KeyData keyData;
            public string testName;
            public string path;
            public IComparer<Keyframe> keyComparer;

            public int DoIt ()
            {
                return Main(keyData, testName, path);
            }

            public static void ConfigureImportSettings(string filename, object customSettings = null)
            {
                if (customSettings==null)
                {
                    customSettings = new {
                        resampleCurves = false,
                        animationType = ModelImporterAnimationType.Legacy,
                        animationCompression = ModelImporterAnimationCompression.Off,
                        importConstraints = true
                    };                 
                }

                ModelImporter modelImporter = AssetImporter.GetAtPath (filename) as ModelImporter;
                modelImporter.resampleCurves = (bool)customSettings.GetType().GetProperty("resampleCurves").GetValue(customSettings,null);
                AssetDatabase.ImportAsset (filename);
                modelImporter.animationType = (ModelImporterAnimationType)customSettings.GetType().GetProperty("animationType").GetValue(customSettings,null);
                modelImporter.animationCompression = (ModelImporterAnimationCompression)customSettings.GetType().GetProperty("animationCompression").GetValue(customSettings,null);
                modelImporter.importConstraints = (bool)customSettings.GetType().GetProperty("importConstraints").GetValue(customSettings, null);
                AssetDatabase.ImportAsset (filename);
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
                        keys [idx].time = keyData.keyTimes [idx];
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
                AnimTester.ConfigureImportSettings(path, keyData.importSettings);

                // create a scene GO so we can compare.
                #if DEBUG_UNITTEST
                GameObject prefabGO = AssetDatabase.LoadMainAssetAtPath (path) as GameObject;
                GameObject sceneGO = Object.Instantiate(prefabGO, keyData.targetObject.transform.localPosition, keyData.targetObject.transform.localRotation);
                sceneGO.name = "Imported_" + testName;
                #endif 

                //acquire imported object from exported file
                AnimationClip animClipImported = GetClipFromFbx (path);

                ClipPropertyTest (animClipOriginal, animClipImported);

                int result = 0;

                foreach (EditorCurveBinding curveBinding in AnimationUtility.GetCurveBindings (animClipImported)) {
                    AnimationCurve animCurveImported = AnimationUtility.GetEditorCurve (animClipImported, curveBinding);
                    Assert.That (animCurveImported, Is.Not.Null);

                    string propertyBinding = curveBinding.propertyName;
                    int id = keyData.GetIndexOf (propertyBinding);

                    bool hasQuatBinding = 
                        MapEulerToQuaternionPropertyName.TryGetValue (propertyBinding, out propertyBinding);

                    bool isRotation = AnimationTestDataClass.m_rotationEulerNames.Contains(curveBinding.propertyName) ||
                        AnimationTestDataClass.m_rotationQuaternionNames.Contains(curveBinding.propertyName);

                    if (id==-1)
                        id = keyData.GetIndexOf (propertyBinding);

                    #if DEBUG_UNITTEST
                    Debug.Log(string.Format("propertyBinding={0} mappedBinding={1} id={2}", curveBinding.propertyName, propertyBinding, id));
                    #endif

                    if (id != -1) 
                    {
                        if (keyData.compareOriginalKeys)
                        {
                            // NOTE: we cannot compare the keys that exported quaternion but are imported as euler.
                            if (!hasQuatBinding)
                            {
                                // compare against original keydata
                                KeysTest (keyData.keyTimes, keyData.GetKeyValues (id), animCurveImported, curveBinding.propertyName);

                                // compare against original animCurve
                                KeysTest (animCurvesOriginal[id], animCurveImported, curveBinding.propertyName, keyComparer);
                            }
                            else
                            {
                                // compare by sampled keyvalues against original keydata
                                KeyValuesTest (keyData.keyTimes, keyData.GetAltKeyValues (id), animCurveImported, curveBinding.propertyName, isRotation);
                            }
                        }
                        else
                        {
                            // compare by sampled keyvalues against original animCurve
                            KeyValuesTest (animCurvesOriginal[id], animCurveImported, curveBinding.propertyName, isRotation);
                        }
                        result++;
                    }
                }

                return result;
            }

            public static void ClipPropertyTest (AnimationClip animClipExpected, AnimationClip animClipActual)
            {
                Assert.That (animClipActual.name, Is.EqualTo (animClipExpected.name).Or.EqualTo(animClipExpected.name));
                Assert.That (animClipActual.legacy, Is.EqualTo (animClipExpected.legacy));
                Assert.That (animClipActual.isLooping, Is.EqualTo (animClipExpected.isLooping));
                Assert.That (animClipActual.wrapMode, Is.EqualTo (animClipExpected.wrapMode));

                // TODO: Uni-34489
                Assert.That (animClipActual.length, Is.EqualTo (animClipExpected.length).Within (0.0001f), "animClip length doesn't match");
            }

            /// <summary>
            /// Compares the properties and curves of multiple animation clips
            /// </summary>
            /// <param name="animClipsOriginal">Animation clips original.</param>
            /// <param name="animClipsImported">Animation clips imported.</param>
            public static void MultiClipTest(AnimationClip[] animClipsOriginal, Dictionary<string, AnimationClip> animClipsImported){
                Assert.That (animClipsImported.Count, Is.EqualTo (animClipsOriginal.Length));

                foreach (var clip in animClipsOriginal) {
                    Assert.That (animClipsImported.ContainsKey (clip.name));
                    var fbxClip = animClipsImported [clip.name];
                    AnimTester.ClipTest (clip, fbxClip);
                }
            }

            /// <summary>
            /// Compares the properties and curves of two animation clips.
            /// </summary>
            /// <param name="animClipOriginal">Animation clip original.</param>
            /// <param name="animClipImported">Animation clip imported.</param>
            public static void ClipTest(AnimationClip animClipOriginal, AnimationClip animClipImported){
                // check clip properties match
                AnimTester.ClipPropertyTest (animClipOriginal, animClipImported);

                foreach (EditorCurveBinding curveBinding in AnimationUtility.GetCurveBindings (animClipOriginal)) {
                    foreach(EditorCurveBinding impCurveBinding in AnimationUtility.GetCurveBindings (animClipImported)) {

                        // only compare if the path and property names match
                        if (curveBinding.path != impCurveBinding.path || curveBinding.propertyName != impCurveBinding.propertyName) {
                            continue;
                        }

                        bool isRotation = AnimationTestDataClass.m_rotationEulerNames.Contains(curveBinding.propertyName) ||
                        AnimationTestDataClass.m_rotationQuaternionNames.Contains(curveBinding.propertyName);

                        AnimationCurve animCurveOrig = AnimationUtility.GetEditorCurve (animClipOriginal, curveBinding);
                        Assert.That (animCurveOrig, Is.Not.Null);

                        AnimationCurve animCurveImported = AnimationUtility.GetEditorCurve (animClipImported, impCurveBinding);
                        Assert.That (animCurveImported, Is.Not.Null);

                        AnimTester.KeyValuesTest(animCurveImported, animCurveOrig,
                            string.Format("path: {0}, property: {1}", curveBinding.path, curveBinding.propertyName),
                            isRotation);
                    }
                }
            }

            public static void KeysTest (AnimationCurve expectedAnimCurve, AnimationCurve actualAnimCurve, string message, IComparer<Keyframe> keyComparer = null)
            {
                if (keyComparer==null)
                    keyComparer = new BasicKeyComparer();
                
                Assert.That (actualAnimCurve.length, Is.EqualTo(expectedAnimCurve.length), string.Format("{0} number of keys doesn't match", message));

                Assert.That(actualAnimCurve.keys, Is.EqualTo(expectedAnimCurve.keys).Using<Keyframe>(keyComparer), string.Format("{0} key doesn't match", message));
            }

            public static void KeysTest (float [] expectedKeyTimes, float [] expectedKeyValues, AnimationCurve actualAnimCurve, string message, IComparer<Keyframe> keyComparer=null)
            {
                if (keyComparer==null)
                    keyComparer = new BasicKeyComparer();
                
                int numKeysExpected = expectedKeyTimes.Length;

                // NOTE : Uni-34492 number of keys don't match
                Assert.That (actualAnimCurve.length, Is.EqualTo(numKeysExpected), string.Format("{0} number of keys doesn't match",message));

                // check actual animation against expected
                // NOTE: if I check the key values explicitly they match but when I compare using this ListMapper the float values
                // are off by 0.000005f; not sure why that happens.
                var keysExpected = new Keyframe[numKeysExpected];

                for (int idx = 0; idx < numKeysExpected; idx++)
                {
                    keysExpected[idx].time = expectedKeyTimes[idx];
                    keysExpected[idx].value = expectedKeyValues[idx];
                }

                Assert.That(actualAnimCurve.keys, Is.EqualTo(keysExpected).Using<Keyframe>(keyComparer), string.Format("{0} key doesn't match", message));

                return ;
            }

            public static void KeyValuesTest (float [] expectedKeyTimes, float [] expectedKeyValues, AnimationCurve actualAnimCurve, string message, bool isRotationCurve = false)
            {
                for (var i=0; i < expectedKeyTimes.Length; ++i)
                {
                    float expectedKeyTime = expectedKeyTimes[i];
                    float expectedKeyValue = expectedKeyValues[i];

                    float actualKeyValue = actualAnimCurve.Evaluate(expectedKeyTime);

#if DEBUG_UNITTEST
                    Debug.Log(string.Format("key time={0} expected={1} actual={2} delta={3}", expectedKeyTime.ToString(), expectedKeyValue.ToString(), actualKeyValue.ToString(), Mathf.Abs(expectedKeyValue-actualKeyValue).ToString()));
#endif
                    // also handles values that are equal but different signs (i.e. -90 == 270)
                    if (isRotationCurve)
                    {
                        var expectedQuat = Quaternion.Euler(new Vector3(expectedKeyValue, 0, 0));
                        var actualQuat = Quaternion.Euler(new Vector3(actualKeyValue, 0, 0));
                        Assert.That(Quaternion.Angle(expectedQuat, actualQuat), Is.LessThan(0.1), string.Format("{0} key ({1}) doesn't match", message, expectedKeyTime));
                    }
                    else
                    {
                        Assert.That(expectedKeyValue, Is.EqualTo(actualKeyValue).Within(0.0001), string.Format("{0} key ({1}) doesn't match", message, expectedKeyTime));
                    }
                }
            }

            public static void KeyValuesTest (AnimationCurve expectedAnimCurve, AnimationCurve actualAnimCurve, string message, bool isRotationCurve = false)
            {
                foreach (var key in expectedAnimCurve.keys)
                {
                    float actualKeyValue = actualAnimCurve.Evaluate(key.time);

#if DEBUG_UNITTEST
                    Debug.Log(string.Format("key time={0} actual={1} expected={2} delta={3}", key.time.ToString(), key.value.ToString(), actualKeyValue.ToString(), Mathf.Abs(key.value-actualKeyValue).ToString()));
#endif
                    var expectedKeyValue = key.value;

                    // also handles values that are equal but different signs (i.e. -90 == 270)
                    if (isRotationCurve)
                    {
                        var expectedQuat = Quaternion.Euler(new Vector3(expectedKeyValue, 0, 0));
                        var actualQuat = Quaternion.Euler(new Vector3(actualKeyValue, 0, 0));
                        Assert.That(Quaternion.Angle(expectedQuat, actualQuat), Is.LessThan(0.1), string.Format("{0} key ({1}) doesn't match", message, key.time));
                    }
                    else
                    {
                        Assert.That(expectedKeyValue, Is.EqualTo(actualKeyValue).Within(0.1), string.Format("{0} key ({1}) doesn't match", message, key.time));
                    }
                }
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


            public static Dictionary<string, AnimationClip> GetClipsFromFbx(string path, bool setLegacy = false){
                //acquire imported object from exported file
                Object [] goAssetImported = AssetDatabase.LoadAllAssetsAtPath (path);
                Assert.That (goAssetImported, Is.Not.Null);

                // TODO : configure object so that it imports w Legacy Animation

                var animClips = new Dictionary<string, AnimationClip> ();
                foreach (Object o in goAssetImported) {
                    var animClipImported = o as AnimationClip;
                    if (animClipImported && !animClipImported.name.StartsWith("__preview__")) {
                        // TODO : configure import settings so we don't need to force legacy
                        animClipImported.legacy = setLegacy;
                        animClips.Add (animClipImported.name, animClipImported);
                    }
                }
                Assert.That (animClips, Is.Not.Empty, "expected imported clips");

                return animClips;
            }

            public static AnimationClip GetClipFromFbx(string path){
                //acquire imported object from exported file
                Object [] goAssetImported = AssetDatabase.LoadAllAssetsAtPath (path);
                Assert.That (goAssetImported, Is.Not.Null);

                // TODO : configure object so that it imports w Legacy Animation

                AnimationClip animClipImported = null;
                foreach (Object o in goAssetImported) {
                    animClipImported = o as AnimationClip;
                    if (animClipImported && !animClipImported.name.StartsWith("__preview__")) break;
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
            GameObject originalGO = AddAssetToScene(fbxPath);

            // get clip
            AnimationClip animClipOriginal = originalGO.GetComponentInChildren<Animation>().clip;
            Assert.That (animClipOriginal, Is.Not.Null);

            // export fbx
            // get GameObject
            string filename = AssetDatabase.GetAssetPath(ExportToFbx(originalGO));

            // TODO: Uni-34492 change importer settings of (newly exported model) 
            // so that it's not resampled and it is legacy animation
            AnimTester.ConfigureImportSettings(filename);

            var animClipImported = AnimTester.GetClipFromFbx (filename);

            AnimTester.ClipTest (animClipOriginal, animClipImported);
        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TransformIndependantComponentTestCases")]
        public int SimplePropertyAnimTest (float [] keyTimesInSeconds, float [] keyValues, System.Type componentType, string componentName)
        {
            KeyData keyData = new PropertyKeyData { propertyName = componentName, componentType = componentType, keyTimes = keyTimesInSeconds, keyFloatValues = keyValues };

            var tester = new AnimTester {keyData=keyData, testName=componentName, path=GetRandomFbxFilePath ()};
            return tester.DoIt();
        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "QuaternionTestCases")]
        public int QuaternionPropertyAnimTest (float [] keyTimesInSeconds, Vector3 [] keyValues, System.Type componentType, string[] componentNames)
        {
            KeyData keyData = new QuaternionKeyData { compareOriginalKeys=true, propertyNames = componentNames, componentType = componentType, keyTimes = keyTimesInSeconds, keyEulerValues = keyValues };

            var tester = new AnimTester {keyData=keyData, testName=(componentType.ToString() + "_Quaternion"), path=GetRandomFbxFilePath ()};
            return tester.DoIt();
        }

        [Ignore("Uni-34804 gimbal conditions")]
        [Test, TestCaseSource (typeof (AnimationTestDataClass), "GimbalTestCases")]
        public int GimbalConditionsAnimTest (float [] keyTimesInSeconds, Vector3 [] keyValues, System.Type componentType, string [] componentNames)
        {
            KeyData keyData = new QuaternionKeyData { propertyNames = componentNames, componentType = componentType, keyTimes = keyTimesInSeconds, keyEulerValues = keyValues };

            var tester = new AnimTester {keyData=keyData, testName=componentType.ToString () + "_Gimbal", path=GetRandomFbxFilePath ()};
            return tester.DoIt();
        }

        [Description("Uni-35616 continuous rotations")]
        [Test, TestCaseSource (typeof (AnimationTestDataClass), "ContinuousRotationTestCases")]
        public int ContinuousRotationAnimTest (RotationCurveType rotCurveType, float [] keyTimesInSeconds, Vector3 [] keyEulerValues)
        {
            System.Type componentType = typeof(Transform);

            string[] propertyNames = null;
            string testName = componentType.ToString () + "_ContinuousRotations";
            bool compareOriginalKeys = false;

            switch (rotCurveType)
            {
            case RotationCurveType.kEuler:
                testName += "_Euler";
                propertyNames=AnimationTestDataClass.m_rotationEulerNames.ToArray();
                break;
            case RotationCurveType.kQuaternion:
                compareOriginalKeys=true;
                testName += "_Quaternion";
                propertyNames=AnimationTestDataClass.m_rotationQuaternionNames.ToArray();
                break;
            }

            KeyData keyData = new TransformKeyData { 
                importSettings = new {resampleCurves = false, animationType= ModelImporterAnimationType.Legacy, animationCompression = ModelImporterAnimationCompression.Off, importConstraints = true},
                compareOriginalKeys=compareOriginalKeys, RotationType = rotCurveType, propertyNames = propertyNames, componentType = componentType, keyTimes = keyTimesInSeconds, keyEulerValues = keyEulerValues };

            var tester = new AnimTester {keyData=keyData, testName=testName, path=GetRandomFbxFilePath ()};
            return tester.DoIt();
        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "ComponentTestCases")]
        public int ComponentAnimTest (System.Type componentType)
        {
            #if DEBUG_UNITTEST
            Debug.Log (string.Format ("ComponentAnimTest {0}", componentType.ToString()));
            #endif 

            if (!ModelExporter.MapsToFbxObject.ContainsKey(componentType))
            {
                #if DEBUG_UNITTEST
                Debug.Log (string.Format ("skipping {0}; fbx export not supported", componentType.ToString()));
                #endif 
                return 1;                
            }

            string testName = "ComponentAnimTest_" + componentType.ToString ();
            GameObject targetObject = AnimTester.CreateTargetObject (testName, componentType);

            string [] propertyNames = 
                (from b in AnimationUtility.GetAnimatableBindings (targetObject, targetObject) 
                 where b.type==componentType select b.propertyName).ToArray();

            if (propertyNames.Length == 0)
            {
                #if DEBUG_UNITTEST
                Debug.Log (string.Format ("skipping {0}; no animatable Single properties found", componentType.ToString()));
                #endif 
                return 1;                
            }

            float [] keyTimesInSeconds = new float [3] { 1f, 2f, 3f };
            var ran = new System.Random();
            float [] keyValues = Enumerable.Range(1, keyTimesInSeconds.Length).Select(x =>(float)ran.NextDouble()).ToArray();

            KeyData keyData = new MultiPropertyKeyData { propertyNames = propertyNames, componentType = componentType, keyTimes = keyTimesInSeconds, keyValues = keyValues, targetObject = targetObject };

            var tester = new AnimTester {keyData=keyData, testName=testName, path=GetRandomFbxFilePath ()};
            return tester.DoIt() <= propertyNames.Length ? 1 : 0;
        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "AnimOnlyTestCases")]
        public void AnimOnlyExportTest(string prefabPath)
        {
            prefabPath = FindPathInUnitTests (prefabPath);
            Assert.That (prefabPath, Is.Not.Null);

            // add prefab to scene
            GameObject originalGO = AddAssetToScene(prefabPath);

            // get clips
            var animator = originalGO.GetComponentInChildren<Animator> ();
            var animClips = GetClipsFromAnimator (animator);

            // get the set of GameObject transforms to be exported with the clip
            var animatedObjects = GetAnimatedGameObjects (animClips, animator.gameObject);

            // export fbx
            // get GameObject
            GameObject fbxObj = ExportToFbx(originalGO, true);
            Assert.IsTrue (fbxObj);

            // compare hierarchy matches animated objects
            var s = new Stack<Transform> ();

            // don't check the root since it probably won't have the same name anyway
            foreach (Transform child in fbxObj.transform) {
                s.Push (child);
            }
            while (s.Count > 0) {
                var transform = s.Pop ();

                Assert.That (animatedObjects.Contains(transform.name));
                animatedObjects.Remove (transform.name);

                foreach (Transform child in transform) {
                    s.Push (child);
                }
            }

            // compare clips
            var fbxAnimClips = AnimTester.GetClipsFromFbx (AssetDatabase.GetAssetPath(fbxObj));
            AnimTester.MultiClipTest (animClips, fbxAnimClips);
        }

        public static AnimationClip[] GetClipsFromAnimator(Animator animator){
            Assert.That (animator, Is.Not.Null);

            var controller = animator.runtimeAnimatorController;
            Assert.That (controller, Is.Not.Null);

            var animClips = controller.animationClips;
            Assert.That (animClips, Is.Not.Null);

            return animClips;
        }

        private HashSet<string> GetAnimatedGameObjects(AnimationClip[] animClips, GameObject animatorObject){
            var animatedObjects = new HashSet<string>();
            foreach (var clip in animClips) {
                foreach (EditorCurveBinding uniCurveBinding in AnimationUtility.GetCurveBindings (clip)) {
                    Object uniObj = AnimationUtility.GetAnimatedObject (animatorObject, uniCurveBinding);
                    if (!uniObj) {
                        continue;
                    }

                    GameObject unityGo = ModelExporter.GetGameObject(uniObj);
                    if (!unityGo) {
                        continue;
                    }

                    // also it's parents up until but excluding the root (the root will have a different name)
                    var parent = unityGo.transform;
                    while (parent != null && parent.parent != null) {
                        animatedObjects.Add (parent.name);
                        parent = parent.parent;
                    }

                }
            }
            return animatedObjects;
        }
    }
}
