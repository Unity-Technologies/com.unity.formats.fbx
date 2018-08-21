// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.
//
// Licensed under the ##LICENSENAME##.
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************

using UnityEngine;
using NUnit.Framework;
using UnityEditor.Formats.Fbx.Exporter;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests
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
            // test Maya integration
           {
                var mayaIntegration = new MayaIntegration ();

                LogNonEmptyString ("display name", mayaIntegration.DccDisplayName);
                LogNonEmptyString ("integration zip path", mayaIntegration.IntegrationZipPath);

                Assert.IsTrue (MayaIntegration.IsHeadlessInstall () == 0);

                LogNonEmptyString ("module template path", mayaIntegration.ModuleTemplatePath);
                LogNonEmptyString ("package path", MayaIntegration.PackagePath);

                LogNonEmptyString ("export settings path", mayaIntegration.ExportSettingsPath);
                LogNonEmptyString ("package version", MayaIntegration.PackageVersion);

                // check if folder is unzipped at invalid paths
                Assert.IsFalse (mayaIntegration.FolderAlreadyUnzippedAtPath (null));
                Assert.IsFalse (mayaIntegration.FolderAlreadyUnzippedAtPath (""));
                Assert.IsFalse (mayaIntegration.FolderAlreadyUnzippedAtPath ("X:/a/b/a/c"));

                // test that the paths don't contain backslashes
                Assert.IsFalse (MayaIntegration.ProjectPath.Contains ("\\"));
                Assert.IsFalse (mayaIntegration.ExportSettingsPath.Contains ("\\"));
            }

            // test Maya LT integration
            {
                var mayaLTIntegration = new MayaLTIntegration ();

                // make sure that the values we get are for Maya LT since it inherits a lot from Maya Integration
                Assert.AreEqual ("Maya LT", mayaLTIntegration.DccDisplayName);
            }

            // test 3ds Max integration
            {
                var maxIntegration = new MaxIntegration ();

                LogNonEmptyString ("display name", maxIntegration.DccDisplayName);
                LogNonEmptyString ("integration zip path", maxIntegration.IntegrationZipPath);

                // check getting absolute path
                var absPath = MaxIntegration.GetAbsPath ("foo");
                Assert.IsTrue (System.IO.Path.IsPathRooted (absPath));
                Assert.IsFalse (absPath.Contains ("\\"));

                // check if folder is unzipped at invalid paths
                Assert.IsFalse (maxIntegration.FolderAlreadyUnzippedAtPath (null));
                Assert.IsFalse (maxIntegration.FolderAlreadyUnzippedAtPath (""));
                Assert.IsFalse (maxIntegration.FolderAlreadyUnzippedAtPath ("X:/a/b/a/c"));
            }
        }
    }
}
