using UnityEditor.Formats.Fbx.Exporter;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.Fbx;
using UnityEngine;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests
{
    public static class ModelExporterReflection
    {
        /// <summary>
        // This class allows accessing the ModelExporter methods that this
        // unit test needs to validate
        /// </summary>

        /////////////
        // Properties
        /////////////
        public static Material DefaultMaterial
        {
            get
            {
                return (Material)GetStaticProperty("DefaultMaterial");
            }
        }

        /////////////
        // Fields
        /////////////
        public static float UnitScaleFactor
        {
            get
            {
                return (float)GetStaticField("UnitScaleFactor");
            }
        }
        public static Dictionary<System.Type, KeyValuePair<System.Type,ModelExporter.FbxNodeRelationType>> MapsToFbxObject
        {
            get
            {
                return (Dictionary<System.Type, KeyValuePair<System.Type,ModelExporter.FbxNodeRelationType>>) GetStaticField("MapsToFbxObject");
            }
        }

        /////////////
        // Methods
        /////////////
        public static GameObject GetGameObject(UnityEngine.Object obj)
        {
            return (GameObject) InvokeMethod("GetGameObject", new object[] { obj });
        }

        public static HashSet<GameObject> RemoveRedundantObjects(IEnumerable<UnityEngine.Object> unityExportSet)
        {
            return (HashSet<GameObject>) InvokeMethod("RemoveRedundantObjects", new object[] {unityExportSet});
        }

        public static string GetVersionFromReadme()
        {
            return (string)InvokeMethod("GetVersionFromReadme", null);
        }

        public static string ConvertToValidFilename(string filename)
        {
            return (string)InvokeMethod("ConvertToValidFilename", new object[] {filename});
        }

        public static Vector3 FindCenter(IEnumerable<GameObject> gameObjects)
        {
            return (Vector3)InvokeMethod("FindCenter", new object[] {gameObjects});
        }

        public static Vector3 GetRecenteredTranslation(Transform t, Vector3 center)
        {
            return (Vector3)InvokeMethod("GetRecenteredTranslation", new object[] {t, center});
        }

        public static FbxLayer GetOrCreateLayer(FbxMesh fbxMesh, int layer = 0 /* default layer */)
        {
            return (FbxLayer)InvokeMethod("GetOrCreateLayer", new object[] {fbxMesh,layer});
        }

        public static FbxVector4 ConvertToRightHanded(Vector3 leftHandedVector, float unitScale = 1f)
        {
            return (FbxVector4)InvokeMethod("ConvertToRightHanded", new object[] {leftHandedVector,unitScale});
        }

        public static bool ExportMaterial(ModelExporter instance, Material unityMaterial, FbxScene fbxScene, FbxNode fbxNode)
        {
            return (bool)InvokeMethod("ExportMaterial", new object[] {unityMaterial,fbxScene,fbxNode},instance);
        }

        public static string ExportObjects(string filePath,
                UnityEngine.Object[] objects = null,
                IExportOptions exportOptions = null,
                Dictionary<GameObject, IExportData> exportData = null)
        {
            return (string)InvokeMethodOverload("ExportObjects",
                                              new object[] { filePath, objects, exportOptions, exportData },
                                              new Type[] { typeof(string), typeof(UnityEngine.Object[]), typeof(IExportOptions), typeof(Dictionary<GameObject, IExportData>) });
        }

        public static IExportData GetExportData(GameObject rootObject, AnimationClip animationClip, IExportOptions exportOptions = null)
        {
            return (IExportData)InvokeMethodOverload("GetExportData",
                                                    new object[] { rootObject, animationClip, exportOptions },
                                                    new Type[] { typeof(GameObject), typeof(AnimationClip), typeof(IExportOptions) });
        }

        // Redefinition of the internal delegate. There might be a way to re-use the one in ModelExporter
        public static void RegisterMeshObjectCallback(ModelExporter.GetMeshForObject callback)
        {
            InvokeMethod("RegisterMeshObjectCallback", new object[] {callback});
        }

        public static void RegisterMeshCallback<T>(ModelExporter.GetMeshForComponent<T> callback, bool replace = false)
                where T : UnityEngine.MonoBehaviour
        {
            // Get the template method first (we assume there is only one 
            // templated RegisterMeshCallback method in ModelExporter
            var methods = from methodInfo in typeof(ModelExporter).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                where methodInfo.Name == "RegisterMeshCallback"
                    && methodInfo.IsGenericMethodDefinition
                select methodInfo;
            
            MethodInfo templateMethod = methods.Single();
            MethodInfo genericMethod = templateMethod.MakeGenericMethod(new Type[]{typeof(T)});
            genericMethod.Invoke(null, new object[]{callback, replace});
        }
        
        public static void UnRegisterMeshCallback<T>()
        {
            // Get the template method first (we assume there is only one 
            // templated RegisterMeshCallback method in ModelExporter
            var methods = from methodInfo in typeof(ModelExporter).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                where methodInfo.Name == "UnRegisterMeshCallback"
                    && methodInfo.IsGenericMethodDefinition
                select methodInfo;
            
            MethodInfo templateMethod = methods.Single();
            MethodInfo genericMethod = templateMethod.MakeGenericMethod(new Type[]{typeof(T)});
            genericMethod.Invoke(null, null);
        }

        public static void UnRegisterMeshCallback(ModelExporter.GetMeshForObject callback)
        {
            InvokeMethodOverload("UnRegisterMeshCallback", 
                                 new object[] {callback},
                                 new Type[] {typeof(ModelExporter.GetMeshForObject)});
        }


        public static bool ExportTexture(ModelExporter instance, Material unityMaterial, string unityPropName,
                           FbxSurfaceMaterial fbxMaterial, string fbxPropName)
        {
            return (bool)InvokeMethod("ExportTexture", 
                                      new object[] {unityMaterial,unityPropName,fbxMaterial,fbxPropName}, 
                                      instance);
        }

        public static FbxDouble3 ConvertQuaternionToXYZEuler(Quaternion q)
        {
            return (FbxDouble3)InvokeMethodOverload("ConvertQuaternionToXYZEuler", 
                                                    new object[] {q}, 
                                                    new Type[] {typeof(Quaternion)});
        }

        public static bool ExportMesh(ModelExporter instance, Mesh mesh, FbxNode fbxNode, Material[] materials = null)
        {
            return (bool)InvokeMethodOverload("ExportMesh",
                                              new object[] {mesh, fbxNode, materials},
                                              new Type[] {typeof(Mesh), typeof(FbxNode), typeof(Material [])},
                                              instance);
  
        }

        /////////// Helpers ///////////
        private static object InvokeMethod(string methodName, object[] argsToPass, ModelExporter instance = null)
        {
            // Use reflection to call the internal ModelExporter.GetGameObject static method
            var internalMethod = typeof(ModelExporter).GetMethod(methodName,
                                                                 (instance == null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic);
            return internalMethod.Invoke(instance, argsToPass);
        }

        // Same as InvokeMethod, but for a specific overload
        private static object InvokeMethodOverload(string methodName, 
                                                   object[] argsToPass, 
                                                   Type[] overloadArgTypes, 
                                                   ModelExporter instance = null)
        {
            var internalMethod = typeof(ModelExporter).GetMethod(methodName,
                                                                 (instance == null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic,
                                                                 null,
                                                                 overloadArgTypes,
                                                                 null);
            return internalMethod.Invoke(instance, argsToPass);
        }

        private static object GetStaticProperty(string propertyName)
        {
            // Use reflection to get the internal property
            var internalProperty = typeof(ModelExporter).GetProperty(propertyName, 
                                          BindingFlags.NonPublic | BindingFlags.Static);
            return internalProperty.GetValue(null,null);
        }

        private static object GetStaticField(string fieldName)
        {
            // Use reflection to get the internal property
            var internalField = typeof(ModelExporter).GetField(fieldName, 
                                                               BindingFlags.NonPublic | BindingFlags.Static);
            return internalField.GetValue(null);
        }
    }
}
