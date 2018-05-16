using FbxExporters.Editor;
using FbxExporters.EditorTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.FbxSdk;
using UnityEngine;

namespace FbxExporters.UnitTests
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

        public static float UnitScaleFactor
        {
            get
            {
                return (float)GetStaticProperty("UnitScaleFactor");
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
            return (Vector3)InvokeMethod("GetRecenteredTranslation", new object[] {center});
        }

        public static ModelExporter.IExportData GetExportData(GameObject rootObject, AnimationClip animationClip, IExportOptions exportOptions = null)
        {
            return (ModelExporter.IExportData)InvokeMethod("GetExportData", new object[] {rootObject, animationClip, exportOptions});
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

        // Redefinition of the internal delegate. There might be a way to re-use the one in ModelExporter
        public delegate bool GetMeshForObject(ModelExporter exporter, GameObject gameObject, FbxNode fbxNode);
        public static void RegisterMeshObjectCallback(GetMeshForObject callback)
        {
            InvokeMethod("RegisterMeshObjectCallback", new object[] {callback});
        }

        public delegate bool GetMeshForComponent<T>(ModelExporter exporter, T component, FbxNode fbxNode) where T : MonoBehaviour;
        public static void RegisterMeshCallback<T>(GetMeshForComponent<T> callback, bool replace = false)
                where T : UnityEngine.MonoBehaviour
        {
            // Get the template method first
            MethodInfo templateMethod = typeof(ModelExporter).GetMethod("RegisterMeshCallback",
                                                                        BindingFlags.Static | BindingFlags.NonPublic,
                                                                        null,
                                                                        new Type[] {typeof(GetMeshForComponent<T>),typeof(bool)},
                                                                        null);
            MethodInfo genericMethod = templateMethod.MakeGenericMethod(new Type[]{T});
            genericMethod.Invoke(null, new object[]{callback, replace});
        }
        
        public static void UnRegisterMeshCallback<T>()
        {
            // Get the template method first
            MethodInfo templateMethod = typeof(ModelExporter).GetMethod("UnRegisterMeshCallback",
                                                                        BindingFlags.Static | BindingFlags.NonPublic,
                                                                        null,
                                                                        null,
                                                                        null);
            MethodInfo genericMethod = templateMethod.MakeGenericMethod(new Type[]{T});
            genericMethod.Invoke(null, null);
        }

        public static void UnRegisterMeshCallback(GetMeshForObject callback)
        {
            InvokeMethodOverload("UnRegisterMeshCallback", 
                                 new object[] {callback},
                                 new Type[] {typeof(GetMeshForObject)});
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
                                              new Type[] {typeof(Mesh), typeof(FbxNode), typeof(Material)},
                                              instance);
        }

        private static object InvokeMethod(string methodName, object[] argsToPass, ModelExporter instance = null)
        {
            // Use reflection to call the internal ModelExporter.GetGameObject static method
            MethodInfo internalMethod = typeof(ModelExporter).GetMethod(methodName,
                                                                        BindingFlags.Static | BindingFlags.NonPublic);
            return internalMethod.Invoke(instance, argsToPass);
        }

        // Same as InvokeMethod, but for a specific overload
        private static object InvokeMethodOverload(string methodName, 
                                                   object[] argsToPass, 
                                                   Type[] overloadArgTypes, 
                                                   ModelExporter instance = null)
        {
            MethodInfo internalMethod = typeof(ModelExporter).GetMethod(methodName,
                                                                        BindingFlags.Static | BindingFlags.NonPublic,
                                                                        null,
                                                                        overloadArgTypes,
                                                                        null);
            return internalMethod.Invoke(instance, argsToPass);
        }

        // Also works for const properties
        private static object GetStaticProperty( string propertyName)
        {
            // Use reflection to get the internal property
            PropertyInfo internalProperty = typeof(ModelExporter).GetProperty(propertyName, 
                                                   BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return internalProperty.GetValue(null,null);
        }
    }
}
