using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;

namespace FbxExporters.UnitTests
{
    public class IntegrationsTest {
        string m_oldMayaLocation;

        [SetUp]
        public void ClearEnv() {
            m_oldMayaLocation = System.Environment.GetEnvironmentVariable ("MAYA_LOCATION");
            System.Environment.SetEnvironmentVariable("MAYA_LOCATION", null);
        }

        [TearDown]
        public void ResetEnv() {
            System.Environment.SetEnvironmentVariable("MAYA_LOCATION", m_oldMayaLocation);
        }

        void LogNonEmptyString(string name, string str) {
            Debug.Log(name + ": " + str);
            Assert.IsFalse(string.IsNullOrEmpty(str));
        }

        [Test]
        public void BasicTest() {
            Assert.IsFalse(Editor.MayaIntegration.IsHeadlessInstall());

            LogNonEmptyString("module path", Editor.MayaIntegration.GetModulePath());
            LogNonEmptyString("module template path", Editor.MayaIntegration.GetModuleTemplatePath());

            LogNonEmptyString("app path", Editor.MayaIntegration.GetAppPath());
            LogNonEmptyString("project path", Editor.MayaIntegration.GetProjectPath());
            LogNonEmptyString("package path", Editor.MayaIntegration.GetPackagePath());
            LogNonEmptyString("package version", Editor.MayaIntegration.GetPackageVersion());
            LogNonEmptyString("temp path", Editor.MayaIntegration.GetTempSavePath());
            LogNonEmptyString("export settings path", Editor.MayaIntegration.GetExportSettingsPath ());
            LogNonEmptyString ("instruction path", Editor.MayaIntegration.GetMayaInstructionPath ());
            LogNonEmptyString ("full instruction path", Editor.MayaIntegration.GetFullMayaInstructionPath ());

            // test that the paths don't contain backslashes
            Assert.IsFalse(Editor.MayaIntegration.GetAppPath().Contains("\\"));
            Assert.IsFalse(Editor.MayaIntegration.GetProjectPath().Contains("\\"));
            Assert.IsFalse(Editor.MayaIntegration.GetTempSavePath().Contains("\\"));
            Assert.IsFalse(Editor.MayaIntegration.GetExportSettingsPath ().Contains("\\"));
        }
    }
}
