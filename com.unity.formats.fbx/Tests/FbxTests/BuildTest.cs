using UnityEngine;
using NUnit.Framework;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEditor;

namespace FbxExporter.UnitTests
{
    public class BuildTest
    {
        private const BuildTargetGroup k_buildTargetGroup = BuildTargetGroup.Standalone;

        private const string k_temporaryFolderName = "_safe_to_delete_build";

        private string BuildFolder { get { return Path.Combine(Path.GetDirectoryName(Application.dataPath), k_temporaryFolderName); } }

        private string BuildTestScenePath { get { return $"Assets/{k_temporaryFolderName}"; } }

        [SetUp]
        public void Init()
        {
            // Create build folder
            Directory.CreateDirectory(BuildFolder);

            // Create temporary scene folder
            Directory.CreateDirectory(BuildTestScenePath);
        }

        [TearDown]
        public void Term()
        {
            // delete build folder
            if (Directory.Exists(BuildFolder))
            {
                Directory.Delete(BuildFolder, recursive: true);
            }

            // if the folder exists in the AssetDatabase, remove it
            // with AssetDatabase to avoid "Files not cleaned up after test" warnings.
            if (AssetDatabase.IsValidFolder(BuildTestScenePath))
            {
                AssetDatabase.DeleteAsset(BuildTestScenePath);
            }

            if (Directory.Exists(BuildTestScenePath))
            {
                Directory.Delete(BuildTestScenePath, recursive: true);
            }
        }

        [Test]
        public void TestBuildPlayer()
        {
            // create simple test scene
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var scenePath = Path.Combine(BuildTestScenePath, "test.unity");
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            BuildTarget buildTarget;
            string buildName;
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    buildTarget = BuildTarget.StandaloneWindows64;
                    buildName = "test.exe";
                    break;
                case RuntimePlatform.OSXEditor:
                    buildTarget = BuildTarget.StandaloneOSX;
                    buildName = "test.app";
                    break;
                case RuntimePlatform.LinuxEditor:
                    buildTarget = BuildTarget.StandaloneLinux64;
                    buildName = "test.x86_64";
                    break;
                default:
                    throw new System.PlatformNotSupportedException($"Platform {Application.platform} is not supported.");
            }

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.locationPathName = Path.Combine(BuildFolder, buildName);
            options.target = buildTarget;
            options.targetGroup = k_buildTargetGroup;
            options.scenes = new string[] { scenePath };

            var report = BuildPipeline.BuildPlayer(options);

            // Check that build completes without errors
            Assert.That(report.summary.result, Is.EqualTo(BuildResult.Succeeded));
            Assert.That(report.summary.totalErrors, Is.EqualTo(0));
            Assert.That(report.summary.outputPath, Is.Not.Null.Or.Empty);
            Assert.That(report.summary.outputPath, Does.Exist);
        }
    }
}