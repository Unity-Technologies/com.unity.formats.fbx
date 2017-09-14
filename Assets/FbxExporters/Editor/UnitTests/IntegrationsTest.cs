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
            Assert.IsFalse(Editor.Integrations.IsHeadlessInstall());

            LogNonEmptyString("module path", Editor.Integrations.GetModulePath());
            LogNonEmptyString("module template path", Editor.Integrations.GetModuleTemplatePath());

            LogNonEmptyString("app path", Editor.Integrations.GetAppPath());
            LogNonEmptyString("project path", Editor.Integrations.GetProjectPath());
            LogNonEmptyString("package path", Editor.Integrations.GetPackagePath());
            LogNonEmptyString("package version", Editor.Integrations.GetPackageVersion());
            LogNonEmptyString("temp path", Editor.Integrations.GetTempSavePath());
            LogNonEmptyString("export settings path", Editor.Integrations.GetExportSettingsPath ());

            // test that the paths don't contain backslashes
            Assert.IsFalse(Editor.Integrations.GetAppPath().Contains("\\"));
            Assert.IsFalse(Editor.Integrations.GetProjectPath().Contains("\\"));
            Assert.IsFalse(Editor.Integrations.GetTempSavePath().Contains("\\"));
            Assert.IsFalse(Editor.Integrations.GetExportSettingsPath ().Contains("\\"));
        }
    }
}
