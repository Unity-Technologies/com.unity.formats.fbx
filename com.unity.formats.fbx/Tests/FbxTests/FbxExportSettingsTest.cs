using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Formats.Fbx.Exporter;
using NUnit.Framework;
using UnityEditor.Presets;

namespace FbxExporter.UnitTests
{
    public class FbxExportSettingsTest : ExporterTestBase
    {
        ExportSettings m_originalSettings;

        //For resetting environment variables:
        string originalVendorLocation = null;
        string originalMayaLocation = null;

        DefaultPreset[] m_exportSettingsDefaultPresets;
        DefaultPreset[] m_convertSettingsDefaultPresets;

        // We read two private fields for the test.
        static System.Reflection.FieldInfo s_InstanceField; // static
        static System.Reflection.FieldInfo s_SavePathField; // member

        static FbxExportSettingsTest()
        {
            // all names, private or public, instance or static
            var privates = System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.Instance;
            var t = typeof(ExportSettings);

            s_SavePathField = t.GetField("maxStoredSavePaths", privates);
            Assert.IsNotNull(s_SavePathField, "maxStoredSavePaths");

            // static fields can't be found through inheritance with GetField.
            // if we change the inheritance diagram, we have to change t.BaseType here.
            s_InstanceField = t.GetField("s_Instance", privates);
            Assert.IsNotNull(s_InstanceField, "s_Instance");
        }

        [NUnit.Framework.SetUp]
        public void SetUp()
        {
            var settings = (ExportSettings)s_InstanceField.GetValue(null);
            m_originalSettings = settings;

            // Clear out the current instance and create a new one (but keep the original around).
            s_InstanceField.SetValue(null, null);
            s_InstanceField.SetValue(null, ScriptableObject.CreateInstance<ExportSettings>());

            // keep track of any existing default presets to reset them once tests finish
            var exportPresetType = new PresetType(ExportSettings.instance.ExportModelSettings);
            var convertPresetType = new PresetType(ExportSettings.instance.ConvertToPrefabSettings);
            m_exportSettingsDefaultPresets = Preset.GetDefaultPresetsForType(exportPresetType);
            m_convertSettingsDefaultPresets = Preset.GetDefaultPresetsForType(convertPresetType);

            // clear default presets
            Preset.SetDefaultPresetsForType(exportPresetType, new DefaultPreset[] {});
            Preset.SetDefaultPresetsForType(convertPresetType, new DefaultPreset[] {});
        }

        [NUnit.Framework.TearDown]
        public void TearDown()
        {
            // Destroy the test settings and restore the original.
            // The original might be null -- not a problem.
            var settings = (ExportSettings)s_InstanceField.GetValue(null);
            ScriptableObject.DestroyImmediate(settings);

            s_InstanceField.SetValue(null, m_originalSettings);

            Preset.SetDefaultPresetsForType(new PresetType(ExportSettings.instance.ExportModelSettings), m_exportSettingsDefaultPresets);
            Preset.SetDefaultPresetsForType(new PresetType(ExportSettings.instance.ConvertToPrefabSettings), m_convertSettingsDefaultPresets);
        }

        [Test]
        public void TestNormalizePath()
        {
            // Test slashes in both directions, and leading and trailing slashes.
            var path = "/a\\b/c/\\";
            var norm = ExportSettings.NormalizePath(path, isRelative: true);
            Assert.AreEqual("a/b/c", norm);
            norm = ExportSettings.NormalizePath(path, isRelative: false);
            Assert.AreEqual("/a/b/c", norm);

            // Test empty path. Not actually absolute, so it's treated as a relative path.
            path = "";
            norm = ExportSettings.NormalizePath(path, isRelative: true);
            Assert.AreEqual(".", norm);
            norm = ExportSettings.NormalizePath(path, isRelative: false);
            Assert.AreEqual(".", norm);

            // Test just a bunch of slashes. Root or . depending on whether it's abs or rel.
            path = "///";
            norm = ExportSettings.NormalizePath(path, isRelative: true);
            Assert.AreEqual(".", norm);
            norm = ExportSettings.NormalizePath(path, isRelative: false);
            Assert.AreEqual("/", norm);

            // Test handling of .
            path = "/a/./b/././c/.";
            norm = ExportSettings.NormalizePath(path, isRelative: true);
            Assert.AreEqual("a/b/c", norm);

            // Test handling of leading ..
            path = "..";
            norm = ExportSettings.NormalizePath(path, isRelative: true);
            Assert.AreEqual("..", norm);

            path = "../a";
            norm = ExportSettings.NormalizePath(path, isRelative: true);
            Assert.AreEqual("../a", norm);

            // Test two leading ..
            path = "../../a";
            norm = ExportSettings.NormalizePath(path, isRelative: true);
            Assert.AreEqual("../../a", norm);

            // Test .. in the middle and effect on leading /
            path = "/a/../b";
            norm = ExportSettings.NormalizePath(path, isRelative: true);
            Assert.AreEqual("b", norm);
            norm = ExportSettings.NormalizePath(path, isRelative: false);
            Assert.AreEqual("/b", norm);

            // Test that we can change the separator
            norm = ExportSettings.NormalizePath(path, isRelative: false, separator: '\\');
            Assert.AreEqual("\\b", norm);
        }

        [Test]
        public void TestGetRelativePath()
        {
            var from = "//a/b/c";
            var to = "///a/b/c/d/e";
            var relative = ExportSettings.GetRelativePath(from, to);
            Assert.AreEqual("d/e", relative);

            from = "///a/b/c/";
            to = "///a/b/c/d/e/";
            relative = ExportSettings.GetRelativePath(from, to);
            Assert.AreEqual("d/e", relative);

            from = "///aa/bb/cc/dd/ee";
            to = "///aa/bb/cc";
            relative = ExportSettings.GetRelativePath(from, to);
            Assert.AreEqual("../..", relative);

            from = "///a/b/c/d/e/";
            to = "///a/b/c/";
            relative = ExportSettings.GetRelativePath(from, to);
            Assert.AreEqual("../..", relative);

            from = "///a/b/c/d/e/";
            to = "///a/b/c/";
            relative = ExportSettings.GetRelativePath(from, to, separator: ':');
            Assert.AreEqual("..:..", relative);

            from = Path.Combine(Application.dataPath, "foo");
            to = Application.dataPath;
            relative = ExportSettings.GetRelativePath(from, to);
            Assert.AreEqual("..", relative);

            to = Path.Combine(Application.dataPath, "foo");
            relative = ExportSettings.ConvertToAssetRelativePath(to);
            Assert.AreEqual("foo", relative);

            relative = ExportSettings.ConvertToAssetRelativePath("/path/to/somewhere/else");
            Assert.AreEqual("", relative);

            relative = ExportSettings.ConvertToAssetRelativePath("/path/to/somewhere/else", requireSubdirectory: false);
            Assert.IsTrue(relative.StartsWith("../"));
            Assert.IsFalse(relative.Contains("\\"));
        }

        [Test]
        public void TestGetSetFields()
        {
            // the path to Assets but with platform-dependent separators
            var appDataPath = Application.dataPath.Replace(Path.AltDirectorySeparatorChar,
                Path.DirectorySeparatorChar);

            var defaultAbsolutePath = ExportSettings.FbxAbsoluteSavePath;
            var dataPath = Path.GetFullPath(Path.Combine(appDataPath, ExportSettings.kDefaultSavePath));
            Assert.AreEqual(dataPath, defaultAbsolutePath);

            var prefabDefaultAbsPath = ExportSettings.PrefabAbsoluteSavePath;
            Assert.AreEqual(dataPath, prefabDefaultAbsPath);

            // set; check that the saved value is platform-independent,
            // that the relative path uses / like in unity,
            // and that the absolute path is platform-specific
            ExportSettings.AddFbxSavePath("/a\\b/c/\\");
            ExportSettings.AddPrefabSavePath("/a\\b/c/\\");

            string forwardSlash = " \u2044 "; // special unicode forward slash
            Assert.That(ExportSettings.GetRelativeFbxSavePaths()[0], Is.EqualTo(string.Format("Assets{0}a{0}b{0}c", forwardSlash)));
            Assert.That(ExportSettings.GetRelativePrefabSavePaths()[0], Is.EqualTo(string.Format("Assets{0}a{0}b{0}c", forwardSlash)));

            var platformPath = Path.Combine("a", Path.Combine("b", "c"));
            Assert.AreEqual(Path.Combine(appDataPath, platformPath), ExportSettings.FbxAbsoluteSavePath);
            Assert.AreEqual(Path.Combine(appDataPath, platformPath), ExportSettings.PrefabAbsoluteSavePath);

            ExportSettings.AddFbxSavePath("test");
            ExportSettings.AddPrefabSavePath("test2");

            // 3 including the default path
            Assert.That(ExportSettings.GetRelativeFbxSavePaths().Length, Is.EqualTo(3));
            Assert.That(ExportSettings.GetRelativePrefabSavePaths().Length, Is.EqualTo(3));
        }

        [Test]
        public void TestFindPreferredProgram()
        {
            //Add a number of fake programs to the list, including some garbage ones
            List<string> testList = new List<string>();
            testList.Add(null);
            testList.Add(ExportSettings.GetUniqueDCCOptionName(ExportSettings.kMaxOptionName + "2000"));
            testList.Add(ExportSettings.GetUniqueDCCOptionName(ExportSettings.kMayaOptionName + "2016"));
            testList.Add(ExportSettings.GetUniqueDCCOptionName(ExportSettings.kMayaOptionName + "2017"));
            testList.Add(ExportSettings.GetUniqueDCCOptionName(ExportSettings.kMaxOptionName + "2017"));
            testList.Add(ExportSettings.GetUniqueDCCOptionName(""));
            testList.Add(ExportSettings.GetUniqueDCCOptionName(null));
            testList.Add(ExportSettings.GetUniqueDCCOptionName(ExportSettings.kMayaLtOptionName));
            testList.Add(ExportSettings.GetUniqueDCCOptionName(ExportSettings.kMayaOptionName + "2017"));

            ExportSettings.instance.SetDCCOptionNames(testList);

            int preferred = ExportSettings.instance.PreferredDCCApp;
            //While Maya 2017 and 3ds Max 2017 are tied for most recent, Maya 2017 (index 8) should win because we prefer Maya.
            Assert.AreEqual(preferred, 8);

            ExportSettings.instance.ClearDCCOptionNames();
            //Try running it with an empty list
            preferred = ExportSettings.instance.PreferredDCCApp;

            Assert.AreEqual(preferred, -1);

            ExportSettings.instance.SetDCCOptionNames(null);
            //Try running it with a null list
            preferred = ExportSettings.instance.PreferredDCCApp;

            Assert.AreEqual(preferred, -1);

            //Testing the results of only having a mayaLT install
            ExportSettings.instance.SetDCCOptionNames(new List<string> { ExportSettings.kMayaLtOptionName + "2018" }); //hardcoded because the constant is changed in another branch but not this one at this time
            preferred = ExportSettings.instance.PreferredDCCApp;

            Assert.AreEqual(preferred, 0);

            //Testing the results of only having a maya install
            ExportSettings.instance.SetDCCOptionNames(new List<string> { ExportSettings.kMayaOptionName + "2018" });
            preferred = ExportSettings.instance.PreferredDCCApp;

            Assert.AreEqual(preferred, 0);

            //Testing the results of only having a max install
            ExportSettings.instance.SetDCCOptionNames(new List<string> { ExportSettings.kMaxOptionName + "2018" });
            preferred = ExportSettings.instance.PreferredDCCApp;

            Assert.AreEqual(preferred, 0);

            //Testing the preference priority
            ExportSettings.instance.SetDCCOptionNames(new List<string> { ExportSettings.kMaxOptionName + "2018", ExportSettings.kMayaOptionName + "2018", ExportSettings.kMayaLtOptionName + "2018" });
            preferred = ExportSettings.instance.PreferredDCCApp;

            Assert.AreEqual(preferred, 1);
        }

        [Test]
        public void TestGetDCCOptions()
        {
            //Our.exe file
            string executableName = "/maya.exe";

            //Our folder names
            string firstSubFolder = "/maya 3000";
            string secondSubFolder = "/maya 3001";

            //Make a folder structure to mimic an 'autodesk' type hierarchy
            string testFolder = Path.GetRandomFileName();
            var firstPath = Directory.CreateDirectory(testFolder + firstSubFolder);
            var secondPath = Directory.CreateDirectory(testFolder + secondSubFolder);

            try
            {
                //Create any files we need within the folders
                FileInfo firstExe = new FileInfo(firstPath.FullName + executableName);
                using (FileStream s = firstExe.Create()) {}

                //Add the paths which will be copied to DCCOptionPaths
                List<string> testPathList = new List<string>();
                testPathList.Add(firstPath.FullName + executableName);  //this path is valid!
                testPathList.Add(secondPath.FullName + executableName);
                testPathList.Add(null);
                testPathList.Add("cookies/milk/foo/bar");

                //Add the names which will be copied to DCCOptionNames
                List<string> testNameList = new List<string>();
                testNameList.Add(firstSubFolder.TrimStart('/'));
                testNameList.Add(secondSubFolder.TrimStart('/'));
                testNameList.Add(null);
                testNameList.Add("Cookies & Milk");

                ExportSettings.instance.SetDCCOptionNames(testNameList);
                ExportSettings.instance.SetDCCOptionPaths(testPathList);

                GUIContent[] options = ExportSettings.GetDCCOptions();

                //We expect 1, as the others are purposefully bogus
                Assert.AreEqual(options.Length, 1);

                Assert.IsTrue(options[0].text == "maya 3000");
            }
            finally
            {
                Directory.Delete(testFolder, true);
            }
        }

        private string MayaPath(string rootDir, string version, bool fullPath = true)
        {
            string result;

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                result = string.Format("{0}/maya{1}/Maya.app/Contents{2}", rootDir, version, (fullPath ? "/MacOS/Maya" : ""));
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                result = string.Format("{0}/Maya{1}{2}", rootDir, version, (fullPath ? "/bin/maya.exe" : ""));
            }
            else
                throw new System.NotImplementedException();

            return result;
        }

        [Test]
        [Platform(Exclude = "Linux", Reason = "Maya/Max integrations are only supported on Windows and OSX")]
        public void VendorLocationInstallationTest1()
        {
            /*
             * different directory roots denoted by ' (eg.) (maya 2017 in rootDir1) = a (maya 2017 in rootDir2) = a'
             * a (maya2017)
             * b (maya2018)
             * c (mayaLT2017)
             * d (mayaLT2018)
             * e (3dsmax2017)
             * f (3dsmax2018)
             */

            // case 1.1: VI=a ML=b' result=2
            // case 1.2: VL=a ML=d' result=2
            // case 1.3: VL=c ML=b' result=2
            // case 1.4: VL=c ML=d' result=2
            // case 1.5: VL=e ML=b' result=2
            // case 1.6: VL=e ML=d' result=2

            string rootDir1 = GetRandomFileNamePath(extName: "");
            string rootDir2 = GetRandomFileNamePath(extName: "");

            var data = new List<Dictionary<string, List<string>>>()
            {
                #region valid test case 1 data (unique locations, one in vendor, one for maya_loc)
                new Dictionary<string, List<string>>()
                {
                    {"VENDOR_INSTALLS", new List<string>(){ MayaPath(rootDir1, "2017"), MayaPath(rootDir2, "2018")} },
                    {"VENDOR_LOCATIONS", new List<string>(){ rootDir1 } },
                    {"MAYA_LOCATION", new List<string>(){ MayaPath(rootDir2, "2018", false) } },
                    {"expectedResult", new List<string>(){ 2.ToString() }}
                },
                new Dictionary<string, List<string>>()
                {
                    {"VENDOR_INSTALLS", new List<string>(){ MayaPath(rootDir1, "2017"), MayaPath(rootDir2, "LT2018") } },
                    {"VENDOR_LOCATIONS", new List<string>(){ rootDir1 } },
                    {"MAYA_LOCATION", new List<string>(){ MayaPath(rootDir2, "LT2018", false) } },
                    {"expectedResult", new List<string>(){ 2.ToString() }}
                },
                new Dictionary<string, List<string>>()
                {
                    {"VENDOR_INSTALLS", new List<string>(){ MayaPath(rootDir1, "LT2017"), MayaPath(rootDir2, "2018") } },
                    {"VENDOR_LOCATIONS", new List<string>(){ rootDir1 } },
                    {"MAYA_LOCATION", new List<string>(){ MayaPath(rootDir2, "2018", false) } },
                    {"expectedResult", new List<string>(){ 2.ToString() }}
                },
                new Dictionary<string, List<string>>()
                {
                    {"VENDOR_INSTALLS", new List<string>(){ MayaPath(rootDir1, "LT2017"), MayaPath(rootDir2, "LT2018") } },
                    {"VENDOR_LOCATIONS", new List<string>(){ rootDir1 } },
                    {"MAYA_LOCATION", new List<string>(){ MayaPath(rootDir2, "LT2018", false) } },
                    {"expectedResult", new List<string>(){ 2.ToString() }}
                },
#if UNITY_EDITOR_WIN
                new Dictionary<string, List<string>>()
                {
                    {"VENDOR_INSTALLS", new List<string>(){ rootDir1 + "/3ds Max 2017/3dsmax.exe", MayaPath(rootDir2, "2018") } },
                    {"VENDOR_LOCATIONS", new List<string>(){ rootDir1 } },
                    {"MAYA_LOCATION", new List<string>(){ MayaPath(rootDir2, "2018", false) } },
                    {"expectedResult", new List<string>(){ 2.ToString() }}
                },
                new Dictionary<string, List<string>>()
                {
                    {"VENDOR_INSTALLS", new List<string>(){ rootDir1 + "/3ds Max 2017/3dsmax.exe", MayaPath(rootDir2, "LT2018") } },
                    {"VENDOR_LOCATIONS", new List<string>(){ rootDir1 } },
                    {"MAYA_LOCATION", new List<string>(){ MayaPath(rootDir2, "LT2018", false) } },
                    {"expectedResult", new List<string>(){ 2.ToString() }}
                },
#endif
                #endregion
                #region case 3 data (EMPTY vendor locations, one for maya location)
                new Dictionary<string, List<string>>()
                {
                    {"VENDOR_INSTALLS", new List<string>(){ MayaPath(rootDir2, "LT2018") } },
                    {"VENDOR_LOCATIONS", new List<string>(){ rootDir1 } },
                    {"MAYA_LOCATION", new List<string>(){ MayaPath(rootDir2, "LT2018", false) } },
                    {"expectedResult", new List<string>(){ 1.ToString () }},
                    {"expected3DApp", new List<string>(){ 0.ToString() }}
                },
                #endregion
            };

            for (int idx = 0; idx < data.Count; idx++)
            {
                List<string> vendorInstallFolders = data[idx]["VENDOR_INSTALLS"];
                //SetUp
                //make the hierarchy for the single app path we need
                VendorLocations_Setup(vendorInstallFolders);

                TestLocations(data[idx]);

                //TearDown
                VendorLocations_TearDown(vendorInstallFolders);
            }
        }

        // Setup fake installation directories
        private void VendorLocations_Setup(List<string> paths)
        {
            //Preserve our environment variables for later
            originalVendorLocation = System.Environment.GetEnvironmentVariable("UNITY_3DAPP_VENDOR_LOCATIONS");
            originalMayaLocation = System.Environment.GetEnvironmentVariable("MAYA_LOCATION");

            foreach (var pathToExe in paths)
            {
                //make the directory
                Directory.CreateDirectory(Path.GetDirectoryName(pathToExe));
                if (Path.GetFileName(pathToExe) != null)
                {
                    //make the file (if we can)
                    FileInfo newExe = new FileInfo(pathToExe);
                    using (FileStream s = newExe.Create()) {}
                }
            }
        }

        //TearDown fake installs
        private void VendorLocations_TearDown(List<string> vendorInstallFolders)
        {
            //Put the environment variables back to what they were originally
            System.Environment.SetEnvironmentVariable("UNITY_3DAPP_VENDOR_LOCATIONS", originalVendorLocation);
            System.Environment.SetEnvironmentVariable("MAYA_LOCATION", originalMayaLocation);

            //Clean up vendor location(s)
            foreach (var vendorLocation in vendorInstallFolders)
            {
                Directory.Delete(Directory.GetParent(Path.GetDirectoryName(vendorLocation)).FullName, true);
            }
        }

        /// <summary>
        /// Sets environment variables to what is passed in, resets the dccOptionNames & dccOptionPaths, and calls FindDCCInstalls()
        /// </summary>
        /// <param name="vendorLocations"></param>
        /// <param name="mayaLocationPath"></param>
        private void SetEnvironmentVariables(string vendorLocation, string mayaLocationPath)
        {
            if (!string.IsNullOrEmpty(vendorLocation))
            {
                //if the given vendor location isn't null, set the environment variable to it.
                System.Environment.SetEnvironmentVariable("UNITY_3DAPP_VENDOR_LOCATIONS", vendorLocation);
            }
            if (mayaLocationPath != null)
            {
                //if the given MAYA_LOCATION isn't null, set the environment variable to it
                System.Environment.SetEnvironmentVariable("MAYA_LOCATION", mayaLocationPath);
            }
        }

        private void TestLocations(Dictionary<string, List<string>> data)
        {
            string envVendorLocations = string.Join(";", data["VENDOR_LOCATIONS"].ToArray());
            string envMayaLocation = data["MAYA_LOCATION"][0];
            int expectedResult = int.Parse(data["expectedResult"][0]);

            //Mayalocation should remain a List because we want to keep using the dictionary which must be of lists (maybe should make an overload)

            //Set Environment Variables
            SetEnvironmentVariables(envVendorLocations, envMayaLocation);

            //Nullify these lists so that we guarantee that FindDccInstalls will be called.
            ExportSettings.instance.ClearDCCOptions();

            GUIContent[] options = ExportSettings.GetDCCOptions();
            Assert.AreEqual(expectedResult, options.Length);

            if (data.ContainsKey("expected3DApp"))
            {
                int preferred = ExportSettings.instance.PreferredDCCApp;
                Assert.AreEqual(preferred, int.Parse(data["expected3DApp"][0]));
            }
        }

        [Test]
        public void TestExportSettingsPresets()
        {
            // make sure that the export settings exist
            ExportSettings.instance.LoadDefaults();

            var exportModelSettings = ExportSettings.instance.ExportModelSettings;
            var convertPrefabSettings = ExportSettings.instance.ConvertToPrefabSettings;

            // test ExportModelSettings preset
            exportModelSettings.info.SetExportFormat(ExportFormat.Binary);
            Assert.That(exportModelSettings.info.ExportFormat, Is.EqualTo(ExportFormat.Binary));

            var exportModelPreset = new Preset(exportModelSettings);
            exportModelSettings.info.SetExportFormat(ExportFormat.ASCII);
            Assert.That(exportModelSettings.info.ExportFormat, Is.EqualTo(ExportFormat.ASCII));

            exportModelPreset.ApplyTo(exportModelSettings);
            Assert.That(exportModelSettings.info.ExportFormat, Is.EqualTo(ExportFormat.Binary));

            // test ConvertToPrefabSettings preset
            convertPrefabSettings.info.SetExportFormat(ExportFormat.Binary);
            Assert.That(convertPrefabSettings.info.ExportFormat, Is.EqualTo(ExportFormat.Binary));

            var convertPrefabPreset = new Preset(convertPrefabSettings);
            convertPrefabSettings.info.SetExportFormat(ExportFormat.ASCII);
            Assert.That(convertPrefabSettings.info.ExportFormat, Is.EqualTo(ExportFormat.ASCII));

            convertPrefabPreset.ApplyTo(convertPrefabSettings);
            Assert.That(convertPrefabSettings.info.ExportFormat, Is.EqualTo(ExportFormat.Binary));
        }

        [Test]
        public void TestFbxExportSettingsPreset()
        {
            var instance = ExportSettings.instance;

            instance.DisplayOptionsWindow = false;
            Assert.That(instance.DisplayOptionsWindow, Is.False);

            var preset = new Preset(ExportSettings.instance);

            instance.DisplayOptionsWindow = true;
            Assert.That(instance.DisplayOptionsWindow, Is.True);

            preset.ApplyTo(instance);
            Assert.That(instance.DisplayOptionsWindow, Is.False);

            // Test that changing the preset doesn't change the instance
            var refObjMethod = preset.GetType().GetMethod("GetReferenceObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var refObj = refObjMethod.Invoke(preset, null) as ExportSettings;

            Assert.That(refObj, Is.Not.Null);

            refObj.DisplayOptionsWindow = true;
            Assert.That(refObj.DisplayOptionsWindow, Is.True);
            preset.UpdateProperties(refObj);

            Assert.That(instance.DisplayOptionsWindow, Is.False);

            preset.ApplyTo(instance);
            Assert.That(instance.DisplayOptionsWindow, Is.True);
        }

        [Test]
        public void TestMultipleInstances()
        {
            var instance = ExportSettings.instance;
            // Test creating a new ExportSettings instance
            ExportSettings newInstance = ScriptableObject.CreateInstance<ExportSettings>();
            // check that a new instance was created
#if UNITY_6000_2_OR_NEWER
            Assert.That(newInstance.GetEntityId(), Is.Not.EqualTo(instance.GetEntityId()));
#else
            Assert.That(newInstance.GetInstanceID(), Is.Not.EqualTo(instance.GetInstanceID()));
#endif
            // but that the "instance" member is the same
            Assert.That(ExportSettings.instance, Is.EqualTo(instance));
            Assert.That(ExportSettings.instance, Is.Not.EqualTo(newInstance));

            instance.DisplayOptionsWindow = false;
            Assert.That(instance.DisplayOptionsWindow, Is.False);

            newInstance.DisplayOptionsWindow = true;
            Assert.That(newInstance.DisplayOptionsWindow, Is.True);
            Assert.That(ExportSettings.instance.DisplayOptionsWindow, Is.False);
        }

        [Test]
        public void TestExportOptionsWindow()
        {
            var instance = ExportSettings.instance;

            // check loading export settings from preset
            var exportSettingsPreset = ScriptableObject.CreateInstance(typeof(ExportModelSettings)) as ExportModelSettings;
            exportSettingsPreset.info.SetAnimatedSkinnedMesh(true);
            exportSettingsPreset.info.SetExportFormat(ExportFormat.ASCII);
            var preset = new Preset(exportSettingsPreset);

            // set default preset
            var type = preset.GetPresetType();
            Assert.That(type.IsValidDefault(), Is.True);
            var defaultPreset = new DefaultPreset(string.Empty, preset);
            Assert.That(Preset.SetDefaultPresetsForType(type, new DefaultPreset[] { defaultPreset }), Is.True);

            // make sure the instance settings do not match the preset
            instance.ExportModelSettings.info.SetAnimatedSkinnedMesh(false);
            instance.ExportModelSettings.info.SetExportFormat(ExportFormat.Binary);

            Assert.That(exportSettingsPreset.info.AnimateSkinnedMesh, Is.Not.EqualTo(instance.ExportModelSettings.info.AnimateSkinnedMesh));
            Assert.That(exportSettingsPreset.info.ExportFormat, Is.Not.EqualTo(instance.ExportModelSettings.info.ExportFormat));

            // create an empty object to have something in the export set
            var go = new GameObject("temp");

            var exportWindow = ExportModelEditorWindow.Init(new Object[] { go });
            // clear any previous settings
            exportWindow.ResetSessionSettings();
            Assert.That(exportWindow.ExportModelSettingsInstance.info.AnimateSkinnedMesh, Is.EqualTo(exportSettingsPreset.info.AnimateSkinnedMesh));
            Assert.That(exportWindow.ExportModelSettingsInstance.info.ExportFormat, Is.EqualTo(exportSettingsPreset.info.ExportFormat));
            exportWindow.Close();

            // check loading export settings from project settings
            // remove preset
            Preset.RemoveFromDefault(preset);
            Assert.That(Preset.GetDefaultPresetsForType(type), Is.Empty);

            exportWindow = ExportModelEditorWindow.Init(new Object[] { go });
            // clear any previous settings
            exportWindow.ResetSessionSettings();
            Assert.That(exportWindow.ExportModelSettingsInstance.info.AnimateSkinnedMesh, Is.EqualTo(instance.ExportModelSettings.info.AnimateSkinnedMesh));
            Assert.That(exportWindow.ExportModelSettingsInstance.info.ExportFormat, Is.EqualTo(instance.ExportModelSettings.info.ExportFormat));

            // check modifying export settings persist and don't modify project settings
            exportWindow.ExportModelSettingsInstance.info.SetAnimatedSkinnedMesh(true);
            exportWindow.ExportModelSettingsInstance.info.SetExportFormat(ExportFormat.ASCII);
            exportWindow.SaveExportSettings();

            exportWindow.Close();
            exportWindow = ExportModelEditorWindow.Init(new Object[] { go });

            Assert.That(exportWindow.ExportModelSettingsInstance.info.AnimateSkinnedMesh, Is.Not.EqualTo(instance.ExportModelSettings.info.AnimateSkinnedMesh));
            Assert.That(exportWindow.ExportModelSettingsInstance.info.ExportFormat, Is.Not.EqualTo(instance.ExportModelSettings.info.ExportFormat));

            // make sure these settings don't persist and close window
            exportWindow.ResetSessionSettings();
            exportWindow.Close();
        }

        [Test]
        public void TestConvertOptionsWindow()
        {
            var instance = ExportSettings.instance;

            // check loading export settings from preset
            var convertSettingsPreset = ScriptableObject.CreateInstance(typeof(ConvertToPrefabSettings)) as ConvertToPrefabSettings;
            convertSettingsPreset.info.SetAnimatedSkinnedMesh(true);
            convertSettingsPreset.info.SetExportFormat(ExportFormat.ASCII);
            var preset = new Preset(convertSettingsPreset);

            // set default preset
            var type = preset.GetPresetType();
            Assert.That(type.IsValidDefault(), Is.True);
            var defaultPreset = new DefaultPreset(string.Empty, preset);
            Assert.That(Preset.SetDefaultPresetsForType(type, new DefaultPreset[] { defaultPreset }), Is.True);

            // make sure the instance settings do not match the preset
            instance.ConvertToPrefabSettings.info.SetAnimatedSkinnedMesh(false);
            instance.ConvertToPrefabSettings.info.SetExportFormat(ExportFormat.Binary);

            Assert.That(convertSettingsPreset.info.AnimateSkinnedMesh, Is.Not.EqualTo(instance.ConvertToPrefabSettings.info.AnimateSkinnedMesh));
            Assert.That(convertSettingsPreset.info.ExportFormat, Is.Not.EqualTo(instance.ConvertToPrefabSettings.info.ExportFormat));

            // create an empty object to have something in the export set
            var go = new GameObject("temp");

            var convertWindow = ConvertToPrefabEditorWindow.Init(new GameObject[] { go });
            // clear any previous settings
            convertWindow.ResetSessionSettings();
            Assert.That(convertWindow.ConvertToPrefabSettingsInstance.info.AnimateSkinnedMesh, Is.EqualTo(convertSettingsPreset.info.AnimateSkinnedMesh));
            Assert.That(convertWindow.ConvertToPrefabSettingsInstance.info.ExportFormat, Is.EqualTo(convertSettingsPreset.info.ExportFormat));
            convertWindow.Close();

            // check loading export settings from project settings
            // remove preset
            Preset.RemoveFromDefault(preset);
            Assert.That(Preset.GetDefaultPresetsForType(type), Is.Empty);

            convertWindow = ConvertToPrefabEditorWindow.Init(new GameObject[] { go });
            // clear any previous settings
            convertWindow.ResetSessionSettings();
            Assert.That(convertWindow.ConvertToPrefabSettingsInstance.info.AnimateSkinnedMesh, Is.EqualTo(instance.ConvertToPrefabSettings.info.AnimateSkinnedMesh));
            Assert.That(convertWindow.ConvertToPrefabSettingsInstance.info.ExportFormat, Is.EqualTo(instance.ConvertToPrefabSettings.info.ExportFormat));

            // check modifying export settings persist and don't modify project settings
            convertWindow.ConvertToPrefabSettingsInstance.info.SetAnimatedSkinnedMesh(true);
            convertWindow.ConvertToPrefabSettingsInstance.info.SetExportFormat(ExportFormat.ASCII);
            convertWindow.SaveExportSettings();

            convertWindow.Close();
            convertWindow = ConvertToPrefabEditorWindow.Init(new GameObject[] { go });

            Assert.That(convertWindow.ConvertToPrefabSettingsInstance.info.AnimateSkinnedMesh, Is.Not.EqualTo(instance.ConvertToPrefabSettings.info.AnimateSkinnedMesh));
            Assert.That(convertWindow.ConvertToPrefabSettingsInstance.info.ExportFormat, Is.Not.EqualTo(instance.ConvertToPrefabSettings.info.ExportFormat));

            // make sure these settings don't persist and close window
            convertWindow.ResetSessionSettings();
            convertWindow.Close();
        }

        [Test]
        public void TestExportOptionsFbxSavePath()
        {
            // check that adding a path or modifying selection in export options window does not modify project
            // settings and vice versa
            var instance = ExportSettings.instance;

            var go = new GameObject("temp");
            var exportWindow = ExportModelEditorWindow.Init(new Object[] { go });
            // clear any previous settings
            exportWindow.ResetSessionSettings();
            var savePathCount = exportWindow.FbxSavePaths.Count;

            // to begin the list of paths should match
            Assert.That(instance.GetCopyOfFbxSavePaths(), Is.EquivalentTo(exportWindow.FbxSavePaths));

            var randomFilePath = GetRandomFileNamePath();
            ExportSettings.AddSavePath(randomFilePath, exportWindow.FbxSavePaths);
            Assert.That(exportWindow.FbxSavePaths.Count, Is.EqualTo(savePathCount + 1));

            exportWindow.SaveExportSettings();

            // make sure the project settings didn't change
            Assert.That(instance.GetCopyOfFbxSavePaths(), Is.Not.EquivalentTo(exportWindow.FbxSavePaths));

            // now change the project settings and make sure the export options paths don't change
            var randomFilePath2 = GetRandomFileNamePath();
            ExportSettings.AddFbxSavePath(randomFilePath2);

            var projectSettingsFbxSavePaths = instance.GetCopyOfFbxSavePaths();
            Assert.That(exportWindow.FbxSavePaths, Is.Not.EquivalentTo(projectSettingsFbxSavePaths));
            Assert.That(exportWindow.FbxSavePaths.Count, Is.EqualTo(savePathCount + 1));

            // When the settings are cleared the file paths should match again
            exportWindow.ResetSessionSettings();

            Assert.That(projectSettingsFbxSavePaths, Is.EquivalentTo(exportWindow.FbxSavePaths));

            exportWindow.Close();
        }

        [Test]
        public void TestExportOptionsReset()
        {
            var instance = ExportSettings.instance;
            var go = new GameObject("temp");
            var exportWindow = ExportModelEditorWindow.Init(new Object[] { go });
            var convertWindow = ConvertToPrefabEditorWindow.Init(new GameObject[] { go });

            // test export window reset
            // change one setting from default
            instance.ExportModelSettings.info.SetExportFormat(ExportFormat.Binary);
            Assert.That(instance.ExportModelSettings.info.ExportFormat, Is.EqualTo(ExportFormat.Binary));

            // reset export settings
            instance.Reset();

            // check that setting was reverted to default
            Assert.That(instance.ExportModelSettings.info.ExportFormat, Is.EqualTo(ExportFormat.ASCII));

            exportWindow.ResetSessionSettings();
            exportWindow.Close();

            // test convert prefab window reset
            // change one setting from default
            instance.ConvertToPrefabSettings.info.SetExportFormat(ExportFormat.Binary);
            Assert.That(instance.ConvertToPrefabSettings.info.ExportFormat, Is.EqualTo(ExportFormat.Binary));

            // reset export settings
            instance.Reset();

            // check that setting was reverted to default
            Assert.That(instance.ConvertToPrefabSettings.info.ExportFormat, Is.EqualTo(ExportFormat.ASCII));

            convertWindow.ResetSessionSettings();
            convertWindow.Close();
        }
    }
}
