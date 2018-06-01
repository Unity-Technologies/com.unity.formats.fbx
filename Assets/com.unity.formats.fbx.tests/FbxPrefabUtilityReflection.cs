using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests
{
    /// <summary>
    // This class allows accessing the FbxPrefabUtility methods that this
    // unit test needs to validate
    /// </summary>
    public class FbxPrefabUtilityReflection
    {
        public static void Initialize<T>(ref T item) where T : new()
        {
            object[] args = new object[] { item };
            Invoke.InvokeStaticGenericMethod<FbxPrefabUtility>("Initialize", ref args, typeof(T));
            item = (T)args[0];
        }

        public static void Add<TKey, TValue>(ref Dictionary<TKey, TValue> thedict, TKey key, TValue value)
        {
            object[] args = new object[] { thedict, key, value };
            Invoke.InvokeStaticGenericMethod<FbxPrefabUtility>("Add", ref args, typeof(TKey), typeof(TValue));
            thedict = (Dictionary<TKey,TValue>)args[0];
        }

        public static void Append<T>(ref List<T> thelist, T item)
        {
            object[] args = new object[] { thelist, item };
            Invoke.InvokeStaticGenericMethodOverload<FbxPrefabUtility>("Append", ref args, typeof(T));
            thelist = (List<T>)args[0];
        }

        public static void Append<TKey, TValue>(ref Dictionary<TKey, List<TValue>> thedict, TKey key, TValue item)
        {
            object[] args = new object[] { thedict, key, item };
            Invoke.InvokeStaticGenericMethodOverload<FbxPrefabUtility>(
                "Append",
                ref args,
                typeof(TKey), typeof(TValue)
                );
            thedict = (Dictionary<TKey, List<TValue>>)args[0];
        }

        public static void Append<TKey1, TKey2, TValue>(ref Dictionary<TKey1, Dictionary<TKey2, List<TValue>>> thedict, TKey1 key1, TKey2 key2, TValue item)
        {
            object[] args = new object[] { thedict, key1, key2, item };
            Invoke.InvokeStaticGenericMethodOverload<FbxPrefabUtility>(
                "Append",
                ref args,
                typeof(TKey1), typeof(TKey2), typeof(TValue)
                );
            thedict = (Dictionary<TKey1, Dictionary<TKey2, List<TValue>>>)args[0];
        }
    }

    public class FbxRepresentationReflection
    {
        public static bool Consume(char expected, string json, ref int index, bool required = true)
        {
            object[] args = new object[] { expected, json, index, required };
            var result = (bool)Invoke.InvokeStaticMethod<FbxRepresentation>("Consume", ref args);
            index = (int)args[2];
            return result;
        }

        public static string ReadString(string json, ref int index)
        {
            object[] args = new object[] { json, index };
            var result =  (string)Invoke.InvokeStaticMethod<FbxRepresentation>("ReadString", ref args);
            index = (int)args[1];
            return result;
        }
    }
}
