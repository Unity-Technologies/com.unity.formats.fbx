using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Fbx.Exporter;

namespace FbxExporter.RuntimeTests
{
    public class FbxPrefabRuntimeTest
    {
        [Test]
        public void FbxPrefabCanBeAddedToGameObject()
        {
            var go = new GameObject("FbxPrefabRuntimeTest");
            try
            {
                Assert.IsNotNull(go.AddComponent<FbxPrefab>());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
