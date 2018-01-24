// NOTE: comment out the next line to leave temporary FBX files on disk
// #define DEBUG_UNITTEST

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

    public enum RotationInterpolation {
        kEuler=3,
        kMixed=(3+4),
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
        public static IEnumerable TestCases5 {
            get {
                yield return new TestCaseData (RotationInterpolation.kEuler /*use euler values*/, m_keytimes5, m_keyPosValues5, m_keyRotValues5).Returns (6);
                // Uni-35616 can't programmatically define a Euler (Quaternion) mix.
                // yield return new TestCaseData (RotationInterpolation.kMixed /*use euler+quaternion values*/, m_keytimes5, m_keyPosValues5, m_keyRotValues5).Returns (3);
                // Uni-35616 doesn't work with quaternions; rotations don't exceed 180
                yield return new TestCaseData (RotationInterpolation.kQuaternion /*use quaternion values*/, m_keytimes5, m_keyPosValues5, m_keyRotValues5).Returns (6);
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

        protected void AnimClipPropertyTest (AnimationClip animClipExpected, AnimationClip animClipActual)
        {
#if UNITY_EDITOR_WIN
            // TODO: figure out why we get __preview__ on Windows
            Assert.That (animClipActual.name, Is.EqualTo (animClipExpected.name).Or.EqualTo("__preview__" + animClipExpected.name));
#else
            Assert.That (animClipActual.name, Is.EqualTo (animClipExpected.name));
#endif
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
            // NOTE: if I check the key values explicitly they match but when I compare using this ListMapper the float values
            // are off by 0.000005f; not sure why that happens.
            Assert.That(new ListMapper(animCurveActual.keys).Property ("time"), Is.EqualTo(keyTimesExpected), string.Format("{0} key time doesn't match",message));
            Assert.That(new ListMapper(animCurveActual.keys).Property ("value"), Is.EqualTo(keyValuesExpected).Within(0.000005f), string.Format("{0} key value doesn't match", message));

            return ;
        }

        private void AnimCurveTest(AnimationCurve animCurveImported, AnimationCurve animCurveActual, string message){
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
            public RotationInterpolation RotationType = RotationInterpolation.kEuler;

            public string[] propertyNames;
            public Vector3 [] keyPosValues; 
            public Vector3 [] keyEulerValues; 
            private Quaternion [] keyQuatValues;

            public bool IsRotation(int id) { return id < (int)RotationType; }

            public override int NumKeys { get { return keyTimesInSeconds.Length; } }
            public override int NumProperties { get { return propertyNames.Length; } }
            public override float [] GetKeyValues (int id)
            {
                if (RotationType==RotationInterpolation.kEuler) 
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
                        case (RotationInterpolation.kEuler):
                            result[idx] = keyEulerValues[idx][id];
                            break;
                        case (RotationInterpolation.kMixed):
                            int NumEulerFields = (int)RotationInterpolation.kEuler;

                            result[idx] = (id<NumEulerFields) 
                                ? keyEulerValues[idx][id] : keyQuatValues[idx][id-NumEulerFields];

                            break;
                        case (RotationInterpolation.kQuaternion):
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
                    // kMixed
                    //     0..2 euler XYZ
                    //     3..6 quaternion XYZ
                    //     7..9 position XYZ
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

        GameObject CreateTargetObject (string name, System.Type componentType)
        {
            GameObject goModel = new GameObject ();
            goModel.name = "model_" + name;

            // check for component and add if missing
            var goComponent = goModel.GetComponent (componentType);
            if (!goComponent)
                goModel.AddComponent (componentType);

            return goModel;
        }

        public int AnimTest (KeyData keyData, string testName)
        {
            string path = GetRandomFbxFilePath ();

            if (!keyData.targetObject)
                keyData.targetObject = CreateTargetObject (testName, keyData.componentType);

            Animation animOrig = keyData.targetObject.AddComponent (typeof (Animation)) as Animation;

            AnimationClip animClipOriginal = new AnimationClip ();

            animClipOriginal.legacy = true;
            animClipOriginal.name = "anim_" + testName;

            for (int id = 0; id < keyData.NumProperties; id++) {
                // initialize keys
                Keyframe [] keys = new Keyframe [keyData.NumKeys];

                for (int idx = 0; idx < keyData.NumKeys; idx++) {
                    keys [idx].time = keyData.keyTimesInSeconds [idx];
                    keys [idx].value = keyData.GetKeyValues (id) [idx];
                }
                AnimationCurve animCurveOriginal = new AnimationCurve (keys);

                animClipOriginal.SetCurve ("", keyData.componentType, keyData.GetPropertyName (id), animCurveOriginal);
            }

            animOrig.AddClip (animClipOriginal, animClipOriginal.name);
            animOrig.clip = animClipOriginal;

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

            AnimClipPropertyTest (animClipOriginal, animClipImported);

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
                    AnimCurveTest (keyData.keyTimesInSeconds, hasAltBinding ? keyData.GetAltKeyValues (id) : keyData.GetKeyValues (id), animCurveImported, curveBinding.propertyName);
                    result++;
                }
            }

            return result;
        }

        private AnimationClip GetClipFromFbx(string path){
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

            var animClipImported = GetClipFromFbx (filename);

            // check clip properties match
            AnimClipPropertyTest (animClipOriginal, animClipImported);

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

                    AnimCurveTest(animCurveImported, animCurveOrig, curveBinding.propertyName);
                }
            }


        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases1")]
        public int SimplePropertyAnimTest (float [] keyTimesInSeconds, float [] keyValues, System.Type componentType, string componentName)
        {
            KeyData keyData = new ComponentKeyData { propertyName = componentName, componentType = componentType, keyTimesInSeconds = keyTimesInSeconds, keyFloatValues = keyValues };

            return AnimTest (keyData, componentName);
        }

        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases2")]
        public int QuaternionPropertyAnimTest (float [] keyTimesInSeconds, Vector3 [] keyValues, System.Type componentType, string[] componentNames)
        {
            KeyData keyData = new QuaternionKeyData { propertyNames = componentNames, componentType = componentType, keyTimesInSeconds = keyTimesInSeconds, keyEulerValues = keyValues };

            return AnimTest (keyData, componentType.ToString() + "_Quaternion");
        }

        [Ignore("Uni-34804 gimbal conditions")]
        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases3")]
        public int GimbalConditionsAnimTest (float [] keyTimesInSeconds, Vector3 [] keyValues, System.Type componentType, string [] componentNames)
        {
            KeyData keyData = new QuaternionKeyData { propertyNames = componentNames, componentType = componentType, keyTimesInSeconds = keyTimesInSeconds, keyEulerValues = keyValues };

            return AnimTest (keyData, componentType.ToString () + "_Gimbal");
        }

        [Description("Uni-35616 continuous rotations")]
        [Test, TestCaseSource (typeof (AnimationTestDataClass), "TestCases5")]
        public int ContinuousRotationAnimTest (RotationInterpolation rotInterp, float [] keyTimesInSeconds, Vector3 [] keyPosValues, Vector3 [] keyEulerValues)
        {
            System.Type componentType = typeof(Transform);

            string[] propertyNames = null;

            switch (rotInterp)
            {
            case RotationInterpolation.kEuler:
                propertyNames=AnimationTestDataClass.m_eulerRotationNames.Concat(AnimationTestDataClass.m_translationNames).ToArray();
                break;
            case RotationInterpolation.kQuaternion:
                propertyNames=AnimationTestDataClass.m_quaternionRotationNames.Concat(AnimationTestDataClass.m_translationNames).ToArray();
                break;
            case RotationInterpolation.kMixed:
                propertyNames=AnimationTestDataClass.m_eulerRotationNames.Concat(AnimationTestDataClass.m_quaternionRotationNames).Concat(AnimationTestDataClass.m_translationNames).ToArray();
                break;
            }

            KeyData keyData = new TransformKeyData { RotationType = rotInterp, propertyNames = propertyNames, componentType = componentType, keyTimesInSeconds = keyTimesInSeconds, keyPosValues = keyPosValues, keyEulerValues = keyEulerValues };

            return AnimTest (keyData, componentType.ToString () + "_ContinuousRotations");
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
            GameObject targetObject = CreateTargetObject (testName, componentType);

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

            return AnimTest (keyData, testName) <= propertyNames.Length ? 1 : 0;
        }

    }
}
