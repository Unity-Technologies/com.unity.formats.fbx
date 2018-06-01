using UnityEditor.Formats.Fbx.Exporter;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.Fbx;
using UnityEngine;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests
{
    public static class Invoke
    {
        public static object InvokeMethod<T>(string methodName, object[] argsToPass, T instance = null) where T : class
        {
            // Use reflection to call the internal ModelExporter.GetGameObject static method
            var internalMethod = typeof(T).GetMethod(methodName, (instance == null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic);
            return internalMethod.Invoke(instance, argsToPass);
        }

        // Same as InvokeMethod, but for a specific overload
        public static object InvokeMethodOverload<T>(string methodName,
                                                   object[] argsToPass,
                                                   Type[] overloadArgTypes,
                                                   T instance = null) where T : class
        {
            var internalMethod = typeof(T).GetMethod(methodName,
                                                    (instance == null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic,
                                                    null,
                                                    overloadArgTypes,
                                                    null);
            return internalMethod.Invoke(instance, argsToPass);
        }

        public static object GetStaticProperty<T>(string propertyName)
        {
            // Use reflection to get the internal property
            var internalProperty = typeof(T).GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Static);
            return internalProperty.GetValue(null, null);
        }

        public static object GetStaticField<T>(string fieldName)
        {
            // Use reflection to get the internal property
            var internalField = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            return internalField.GetValue(null);
        }

        public static object InvokeStaticMethod<T>(string methodName, object[] argsToPass)
        {
            var internalMethod = typeof(T).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            return internalMethod.Invoke(null, argsToPass);
        }

        public static object InvokeStaticMethod<T>(string methodName, ref object[] argsToPass)
        {
            // for functions with "ref" or "out" parameters. argsToPass list will have items replaced with the updated
            // values.
            var internalMethod = typeof(T).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            return internalMethod.Invoke(null, argsToPass);
        }

        public static object InvokeStaticGenericMethod<T>(string methodName, ref object[] argsToPass, params Type[] typeArgs)
        {
            MethodInfo method = typeof(T).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo generic = method.MakeGenericMethod(typeArgs);
            return generic.Invoke(null, argsToPass);
        }

        // Same as InvokeStaticGenericMethod, but for a specific overload
        public static object InvokeStaticGenericMethodOverload<T>(string methodName,
                                                                   ref object[] argsToPass,
                                                                   params Type[] typeArgs)
        {
            var internalMethod = typeof(T).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).First(
                m => m.Name == methodName && m.GetGenericArguments().Length == typeArgs.Length
                );
            MethodInfo generic = internalMethod.MakeGenericMethod(typeArgs);
            return generic.Invoke(null, argsToPass);
        }
    }

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
                return (Material)Invoke.GetStaticProperty<ModelExporter>("DefaultMaterial");
            }
        }

        /////////////
        // Fields
        /////////////
        public static float UnitScaleFactor
        {
            get
            {
                return (float)Invoke.GetStaticField<ModelExporter>("UnitScaleFactor");
            }
        }
        public static Dictionary<System.Type, KeyValuePair<System.Type,ModelExporter.FbxNodeRelationType>> MapsToFbxObject
        {
            get
            {
                return (Dictionary<System.Type, KeyValuePair<System.Type,ModelExporter.FbxNodeRelationType>>) Invoke.GetStaticField<ModelExporter>("MapsToFbxObject");
            }
        }

        /////////////
        // Methods
        /////////////
        public static GameObject GetGameObject(UnityEngine.Object obj)
        {
            return (GameObject) Invoke.InvokeMethod<ModelExporter>("GetGameObject", new object[] { obj });
        }

        public static HashSet<GameObject> RemoveRedundantObjects(IEnumerable<UnityEngine.Object> unityExportSet)
        {
            return (HashSet<GameObject>) Invoke.InvokeMethod<ModelExporter>("RemoveRedundantObjects", new object[] {unityExportSet});
        }

        public static string GetVersionFromReadme()
        {
            return (string)Invoke.InvokeMethod<ModelExporter>("GetVersionFromReadme", null);
        }

        public static string ConvertToValidFilename(string filename)
        {
            return (string)Invoke.InvokeMethod<ModelExporter>("ConvertToValidFilename", new object[] {filename});
        }

        public static Vector3 FindCenter(IEnumerable<GameObject> gameObjects)
        {
            return (Vector3)Invoke.InvokeMethod<ModelExporter>("FindCenter", new object[] {gameObjects});
        }

        public static Vector3 GetRecenteredTranslation(Transform t, Vector3 center)
        {
            return (Vector3)Invoke.InvokeMethod<ModelExporter>("GetRecenteredTranslation", new object[] {t, center});
        }

        public static FbxLayer GetOrCreateLayer(FbxMesh fbxMesh, int layer = 0 /* default layer */)
        {
            return (FbxLayer)Invoke.InvokeMethod<ModelExporter>("GetOrCreateLayer", new object[] {fbxMesh,layer});
        }

        public static FbxVector4 ConvertToRightHanded(Vector3 leftHandedVector, float unitScale = 1f)
        {
            return (FbxVector4)Invoke.InvokeMethod<ModelExporter>("ConvertToRightHanded", new object[] {leftHandedVector,unitScale});
        }

        public static bool ExportMaterial(ModelExporter instance, Material unityMaterial, FbxScene fbxScene, FbxNode fbxNode)
        {
            return (bool)Invoke.InvokeMethod("ExportMaterial", new object[] {unityMaterial,fbxScene,fbxNode},instance);
        }

        public static string ExportObjects(string filePath,
                UnityEngine.Object[] objects = null,
                IExportOptions exportOptions = null,
                Dictionary<GameObject, IExportData> exportData = null)
        {
            return (string)Invoke.InvokeMethodOverload<ModelExporter>("ExportObjects",
                                                                      new object[] { filePath, objects, exportOptions, exportData },
                                                                      new Type[] { typeof(string), typeof(UnityEngine.Object[]), typeof(IExportOptions), typeof(Dictionary<GameObject, IExportData>) });
        }

        public static IExportData GetExportData(GameObject rootObject, AnimationClip animationClip, IExportOptions exportOptions = null)
        {
            return (IExportData)Invoke.InvokeMethodOverload<ModelExporter>("GetExportData",
                                                                            new object[] { rootObject, animationClip, exportOptions },
                                                                            new Type[] { typeof(GameObject), typeof(AnimationClip), typeof(IExportOptions) });
        }

        // Redefinition of the internal delegate. There might be a way to re-use the one in ModelExporter
        public static void RegisterMeshObjectCallback(GetMeshForObject callback)
        {
            Invoke.InvokeMethod<ModelExporter>("RegisterMeshObjectCallback", new object[] {callback});
        }

        public static void RegisterMeshCallback<T>(GetMeshForComponent<T> callback, bool replace = false)
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

        public static void UnRegisterMeshCallback(GetMeshForObject callback)
        {
            Invoke.InvokeMethodOverload<ModelExporter>("UnRegisterMeshCallback", 
                                                         new object[] {callback},
                                                         new Type[] {typeof(GetMeshForObject)});
        }


        public static bool ExportTexture(ModelExporter instance, Material unityMaterial, string unityPropName,
                           FbxSurfaceMaterial fbxMaterial, string fbxPropName)
        {
            return (bool)Invoke.InvokeMethod("ExportTexture", 
                                              new object[] {unityMaterial,unityPropName,fbxMaterial,fbxPropName}, 
                                              instance);
        }

        public static FbxDouble3 ConvertQuaternionToXYZEuler(Quaternion q)
        {
            return (FbxDouble3)Invoke.InvokeMethodOverload<ModelExporter>("ConvertQuaternionToXYZEuler", 
                                                                            new object[] {q}, 
                                                                            new Type[] {typeof(Quaternion)});
        }

        public static bool ExportMesh(ModelExporter instance, Mesh mesh, FbxNode fbxNode, Material[] materials = null)
        {
            return (bool)Invoke.InvokeMethodOverload("ExportMesh",
                                                      new object[] {mesh, fbxNode, materials},
                                                      new Type[] {typeof(Mesh), typeof(FbxNode), typeof(Material [])},
                                                      instance);
  
        }
    }
}
