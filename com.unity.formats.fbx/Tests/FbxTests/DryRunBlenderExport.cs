using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

namespace FbxExporter.UnitTests
{
    // Dry run only: writes binary FBX files to TestResults/blender so they can be opened in Blender.
    public class DryRunBlenderExport : ExporterTestBase
    {
        static string OutputFolder
        {
            get
            {
                var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
                var folder = Path.Combine(repoRoot, "TestResults", "blender");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        static ExportModelOptions BinaryOptions(Include include, bool mayaCompatibleNames = true)
        {
            var options = new ExportModelOptions();
            options.ExportFormat = ExportFormat.Binary;
            options.ModelAnimIncludeOption = include;
            options.UseMayaCompatibleNames = mayaCompatibleNames;
            return options;
        }

        static Material CreateMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            return material;
        }

        [Test]
        public void ExportNonAsciiAnimatedScene()
        {
            var root = new GameObject("Scene");

            var cube = CreateGameObject("片仮名", root.transform, PrimitiveType.Cube);
            cube.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial(Color.red);

            var sphere = CreateGameObject("Accentué", root.transform, PrimitiveType.Sphere);
            sphere.transform.localPosition = new Vector3(2, 0, 0);
            sphere.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial(Color.green);

            var child = CreateGameObject("Würfel", sphere.transform, PrimitiveType.Cylinder);
            child.transform.localPosition = new Vector3(0, 1.5f, 0);
            child.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial(Color.blue);

            var clip = new AnimationClip { legacy = true, name = "MoveUp" };
            clip.SetCurve("片仮名", typeof(Transform), "localPosition.y",
                new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 3f), new Keyframe(2f, 0f)));
            var animation = root.AddComponent<Animation>();
            animation.AddClip(clip, clip.name);
            animation.clip = clip;

            var path = Path.Combine(OutputFolder, "non_ascii_animated_scene.fbx");
            Assert.AreEqual(path, ModelExporter.ExportObject(path, root, BinaryOptions(Include.ModelAndAnim, mayaCompatibleNames: false)));
            Assert.That(path, Does.Exist);
        }

        [TestCase("Models/DefaultMale/DefaultMale.prefab", "skinned_character.fbx")]
        [TestCase("Models/blendshape_with_skinning.fbx", "blendshapes_with_skinning.fbx")]
        public void ExportTestModel(string assetPath, string outputName)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(FindPathInUnitTests(assetPath)) as GameObject;
            Assert.That(asset, Is.Not.Null, assetPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);

            var path = Path.Combine(OutputFolder, outputName);
            Assert.AreEqual(path, ModelExporter.ExportObject(path, instance, BinaryOptions(Include.ModelAndAnim)));
            Assert.That(path, Does.Exist);
            Object.DestroyImmediate(instance);
        }
    }
}
