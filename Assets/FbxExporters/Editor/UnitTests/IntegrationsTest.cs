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
        public void BatchModeTest() {
            // test that the commands work in batch mode

            // install maya integration
            var mayaVersion = new Editor.Integrations.MayaVersion();
            bool result = Editor.Integrations.InstallMaya (mayaVersion, verbose: true);
            Assert.IsTrue (result);

            int exitCode = Editor.Integrations.ConfigureMaya (mayaVersion);
            Assert.AreEqual (0, exitCode);

            // run + quit maya in batch mode
            // make sure the plugin loads before quit
            RunMaya (mayaVersion.MayaExe, "loadPlugin -quiet unityOneClickPlugin");

            // run import command in batch mode
            RunMaya (mayaVersion.MayaExe, "loadPlugin -quiet unityOneClickPlugin; unityImport");
        }

        private void RunMaya(string mayaExe, string command = null, bool batchMode = true)
        {
            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            myProcess.StartInfo.FileName = mayaExe;
            myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.StartInfo.UseShellExecute = false;

            string commands = (string.IsNullOrEmpty (command) ? "" : command + "; ") + "scriptJob -idleEvent quit;";
            string arguments = (batchMode ? "-batch" : "") + " -command ";

#if UNITY_EDITOR_OSX
            myProcess.StartInfo.Arguments = string.Format(arguments + "'{0}'", commands);
#elif UNITY_EDITOR_LINUX
            throw new NotImplementedException();
#else // UNITY_EDITOR_WINDOWS
            myProcess.StartInfo.Arguments = string.Format(arguments + "\"{0}\"", commands);
#endif
            myProcess.EnableRaisingEvents = true;
            myProcess.Start();
            myProcess.WaitForExit();
            int exitCode = myProcess.ExitCode;
            Assert.AreEqual (0, exitCode);
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
            LogNonEmptyString ("instruction path", Editor.Integrations.GetMayaInstructionPath ());
            LogNonEmptyString ("full instruction path", Editor.Integrations.GetFullMayaInstructionPath ());

            // test that the paths don't contain backslashes
            Assert.IsFalse(Editor.Integrations.GetAppPath().Contains("\\"));
            Assert.IsFalse(Editor.Integrations.GetProjectPath().Contains("\\"));
            Assert.IsFalse(Editor.Integrations.GetTempSavePath().Contains("\\"));
            Assert.IsFalse(Editor.Integrations.GetExportSettingsPath ().Contains("\\"));
        }
    }
}
