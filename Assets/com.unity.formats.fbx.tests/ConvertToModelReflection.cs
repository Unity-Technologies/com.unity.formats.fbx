using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests
{
    /// <summary>
    // This class allows accessing the ConvertToModel methods that this
    // unit test needs to validate
    /// </summary>
    public class ConvertToModelReflection
    {
        public static GameObject GetOrCreateFbxAsset(GameObject toConvert,
                string fbxDirectoryFullPath = null,
                string fbxFullPath = null,
                ConvertToPrefabSettingsSerialize exportOptions = null)
        {
            return (GameObject)InvokeStaticMethod("GetOrCreateFbxAsset", new object[] { toConvert, fbxDirectoryFullPath, fbxFullPath, exportOptions });
        }

        public static void CopyComponents(GameObject to, GameObject from)
        {
            InvokeStaticMethod("CopyComponents", new object[] { to, from });
        }

        public static void UpdateFromSourceRecursive(GameObject dest, GameObject source)
        {
            InvokeStaticMethod("UpdateFromSourceRecursive", new object[] { dest, source });
        }

        public static Dictionary<string, GameObject> MapNameToSourceRecursive(GameObject dest, GameObject source)
        {
            return (Dictionary<string, GameObject>)InvokeStaticMethod("MapNameToSourceRecursive", new object[] { dest, source });
        }

        /////////// Helpers ///////////
        private static object InvokeStaticMethod(string methodName, object[] argsToPass)
        {
            // Use reflection to call the internal ModelExporter.GetGameObject static method
            var internalMethod = typeof(ConvertToModel).GetMethod(methodName,
                                                                  BindingFlags.Static | BindingFlags.NonPublic);
            return internalMethod.Invoke(null, argsToPass);
        }
    }
}
