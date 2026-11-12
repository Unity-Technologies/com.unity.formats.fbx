using System;
using NUnit.Framework;

namespace FbxExporter.RuntimeTests
{
    public class FbxPrefabRuntimeTest
    {
        [Test]
        public void RuntimeAssemblyContainsFbxPrefab()
        {
            var type = Type.GetType("UnityEngine.Formats.Fbx.Exporter.FbxPrefab, Unity.Formats.Fbx.Runtime");
            Assert.IsNotNull(type);
        }
    }
}
