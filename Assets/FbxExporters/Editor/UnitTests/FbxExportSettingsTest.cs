using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using FbxExporters.EditorTools;
using NUnit.Framework;

namespace FbxExporters.UnitTests
{
    public class FbxExportSettingsTest {
        ExportSettings m_originalSettings;

        static System.Reflection.FieldInfo s_InstanceField; // static
        static System.Reflection.FieldInfo s_SavePathField; // member
        static System.Reflection.MethodInfo s_NormalizeRelativePath; // static
        static System.Reflection.MethodInfo s_GetRelativePath; // static

		static FbxExportSettingsTest() {
            var privates = System.Reflection.BindingFlags.NonPublic 
                | System.Reflection.BindingFlags.Static
				| System.Reflection.BindingFlags.Instance;
            var t = typeof(ExportSettings);

			s_SavePathField = t.GetField("convertToModelSavePath", privates);
			Assert.IsNotNull(s_SavePathField, "convertToModelSavePath");

            s_NormalizeRelativePath = t.GetMethod("NormalizeRelativePath", privates);
			Assert.IsNotNull(s_NormalizeRelativePath, "NormalizeRelativePath");

            s_GetRelativePath = t.GetMethod("GetRelativePath", privates);
			Assert.IsNotNull(s_GetRelativePath, "GetRelativePath");

			// static fields can't be found through inheritance with GetField.
			// if we change the inheritance diagram, we have to change t.BaseType here.
			s_InstanceField = t.BaseType.GetField("s_Instance", privates);
			Assert.IsNotNull(s_InstanceField, "s_Instance");

			#if ENABLE_COVERAGE_TEST
            FbxSdk.CoverageTester.RegisterReflectionCall(
				typeof(UnitTests).GetMethod("NormalizePath"), s_NormalizeRelativePath);
            FbxSdk.CoverageTester.RegisterReflectionCall(
				typeof(UnitTests).GetMethod("GetRelativePath"), s_GetRelativePath);
			#endif
        }

		#if ENABLE_COVERAGE_TEST
		[Test]
		public void TestCoverage()
		{
			FbxSdk.CoverageTester.TestCoverage(typeof(ExportSettings), this.GetType());
		}
		#endif


        string NormalizePath(string path) {
            return FbxSdk.Invoker.InvokeStatic<string>(s_NormalizeRelativePath, path);
        }

        string GetRelativePath(string a, string b) {
			return FbxSdk.Invoker.InvokeStatic<string>(s_GetRelativePath, a, b);
        }

        [NUnit.Framework.SetUp]
            public void SetUp()
            {
                var settings = (ExportSettings)s_InstanceField.GetValue(null);
                m_originalSettings = settings;

                // Clear out the current instance and create a new one (but keep the original around).
                s_InstanceField.SetValue(null, null);
                s_InstanceField.SetValue(null, ScriptableObject.CreateInstance<ExportSettings>());
            }

        [NUnit.Framework.TearDown]
            public void TearDown()
            {
                // Destroy the test settings and restore the original.
                // The original might be null -- not a problem.
                var settings = (ExportSettings)s_InstanceField.GetValue(null);
                ScriptableObject.DestroyImmediate(settings);

                s_InstanceField.SetValue(null, m_originalSettings);
            }


        [Test]
            public void TestNormalizePath()
            {
                var path = "/a\\b/c/\\";
                var norm = NormalizePath(path);
                Assert.AreEqual(Path.Combine("a", Path.Combine("b", "c")), norm);

                path = "";
                norm = NormalizePath(path);
                Assert.AreEqual(".", norm);

                path = "///";
                norm = NormalizePath(path);
                Assert.AreEqual(".", norm);
            }

        [Test]
            public void TestGetRelativePath()
            {
                var from = "file:///a/b/c";
                var to = "http://google.com";
                var relative = GetRelativePath(from, to);
                Assert.IsNull(relative);

                from = "file:///a/b/c";
                to = "file:///a/b/c/d/e";
                relative = GetRelativePath(from, to);
                Assert.AreEqual(Path.Combine("d", "e"), relative);

                from = "file:///a/b/c/";
                to = "file:///a/b/c/d/e/";
                relative = GetRelativePath(from, to);
                Assert.AreEqual(Path.Combine("d", "e"), relative);

			from = "file:///aa/bb/cc/dd/ee";
                to = "file:///aa/bb/cc";
                relative = GetRelativePath(from, to);
                Assert.AreEqual(Path.Combine("..", ".."), relative);

			from = "file:///a/b/c/d/e/";
			to = "file:///a/b/c/";
			relative = GetRelativePath(from, to);
			Assert.AreEqual(Path.Combine("..", ".."), relative);

			from = Path.Combine(Application.dataPath, "foo");
                to = Application.dataPath;
                relative = GetRelativePath(from, to);
                Assert.AreEqual("..", relative);

                to = Path.Combine(Application.dataPath, "foo");
                relative = ExportSettings.ConvertToAssetRelativePath(to);
                Assert.AreEqual("foo", relative);
            }

        [Test]
            public void TestGetSetFields()
            {
                var defaultRelativePath = ExportSettings.GetRelativeSavePath();
                Assert.AreEqual(ExportSettings.kDefaultSavePath, defaultRelativePath);

                var defaultAbsolutePath = ExportSettings.GetAbsoluteSavePath();
                Assert.AreEqual(Path.Combine(Application.dataPath, ExportSettings.kDefaultSavePath),
                        defaultAbsolutePath);

                // set; check that the saved value is platform-independent,
                // but the relative save path function is platform-specific.
                ExportSettings.SetRelativeSavePath("/a\\b/c/\\");
                var convertToModelSavePath = s_SavePathField.GetValue(ExportSettings.instance);
                Assert.AreEqual("a/b/c", convertToModelSavePath);
                var platformPath = Path.Combine("a", Path.Combine("b", "c"));
                Assert.AreEqual(platformPath, ExportSettings.GetRelativeSavePath());
            }
    }
}
