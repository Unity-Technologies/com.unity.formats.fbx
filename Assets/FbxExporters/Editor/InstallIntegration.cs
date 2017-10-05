using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace FbxExporters.Editor
{
    class Integrations
    {
        private const string MODULE_FILENAME = "unityoneclick";
        private const string PACKAGE_NAME = "FbxExporters";
        private const string VERSION_FILENAME = "README.txt";
        private const string VERSION_FIELD = "**Version**";
        private const string VERSION_TAG = "{Version}";
        private const string PROJECT_TAG = "{UnityProject}";

        private const string FBX_EXPORT_SETTINGS_PATH = "FbxExporters/Integrations/Autodesk/maya/scripts/unityFbxExportSettings.mel";

        private const string MAYA_INSTRUCTION_FILENAME = "_safe_to_delete/_temp.txt";

        private const string MODULE_TEMPLATE_PATH = "FbxExporters/Integrations/Autodesk/maya/" + MODULE_FILENAME + ".txt";

#if UNITY_EDITOR_OSX
        private const string MAYA_MODULES_PATH = "Library/Preferences/Autodesk/Maya/modules";
#elif UNITY_EDITOR_LINUX
        private const string MAYA_MODULES_PATH = "Maya/modules";
#else
        private const string MAYA_MODULES_PATH = "maya/modules";
#endif

        public class MayaException : System.Exception {
            public MayaException() { }
            public MayaException(string message) : base(message) { }
            public MayaException(string message, System.Exception inner) : base(message, inner) { }
        }

        // Use string to define escaped quote
        // Windows needs the backslash
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        private const string ESCAPED_QUOTE = "\"";
#else
        private const string ESCAPED_QUOTE = "\\\"";
#endif

        private static string MAYA_COMMANDS { get {
                return string.Format("configureUnityOneClick {0}{1}{0} {0}{2}{0} {0}{3}{0} {0}{4}{0} {0}{5}{0} {6}; scriptJob -idleEvent quit;",
                    ESCAPED_QUOTE, GetProjectPath(), GetAppPath(), GetTempSavePath(),
                    GetExportSettingsPath(), GetMayaInstructionPath(), (IsHeadlessInstall()?1:0));
        }}
        private static Char[] FIELD_SEPARATORS = new Char[] {':'};

        private static string GetUserFolder()
        {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            return System.Environment.GetEnvironmentVariable("HOME");
#else
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
#endif
        }

        public static bool IsHeadlessInstall ()
        {
            return false;
        }

        public static string GetModulePath()
        {
            return System.IO.Path.Combine(GetUserFolder(), MAYA_MODULES_PATH);
        }

        public static string GetModuleTemplatePath()
        {
            return System.IO.Path.Combine(Application.dataPath, MODULE_TEMPLATE_PATH);
        }

        public static string GetAppPath()
        {
            return EditorApplication.applicationPath.Replace("\\","/");
        }

        public static string GetProjectPath()
        {
            return System.IO.Directory.GetParent(Application.dataPath).FullName.Replace("\\","/");
        }

        public static string GetPackagePath()
        {
            return System.IO.Path.Combine(Application.dataPath, PACKAGE_NAME);
        }

        public static string GetTempSavePath()
        {
            return FbxExporters.Review.TurnTable.TempSavePath.Replace("\\", "/");
        }

        /// <summary>
        /// Gets the maya instruction path relative to the Assets folder.
        /// Assets folder is not included in the path.
        /// </summary>
        /// <returns>The relative maya instruction path.</returns>
        public static string GetMayaInstructionPath()
        {
            return MAYA_INSTRUCTION_FILENAME;
        }

        /// <summary>
        /// Gets the full maya instruction path as an absolute Unity path.
        /// </summary>
        /// <returns>The full maya instruction path.</returns>
        public static string GetFullMayaInstructionPath()
        {
            return Application.dataPath + "/" + FbxExporters.Editor.Integrations.GetMayaInstructionPath ();
        }

        /// <summary>
        /// Gets the path to the export settings file.
        /// Returns a relative path with forward slashes as path separators.
        /// </summary>
        /// <returns>The export settings path.</returns>
        public static string GetExportSettingsPath()
        {
            return FBX_EXPORT_SETTINGS_PATH;
        }

        public static string GetPackageVersion()
        {
            string result = null;

            try {
                string FileName = System.IO.Path.Combine(GetPackagePath(), VERSION_FILENAME);

                System.IO.StreamReader sr = new System.IO.StreamReader(FileName);

                // Read the first line of text
                string line = sr.ReadLine();

                // Continue to read until you reach end of file
                while (line != null)
                {
                    if (line.StartsWith(VERSION_FIELD, StringComparison.CurrentCulture))
                    {
                        string[] fields = line.Split(FIELD_SEPARATORS);

                        if (fields.Length>1)
                        {
                            result = fields[1];
                        }
                        break;
                    }
                    line = sr.ReadLine();
                }
            }
            catch(Exception e)
            {
                Debug.LogError(string.Format("Exception failed to read file containing package version ({0})", e.Message));
            }

            return result;
        }

        private static List<string> ParseTemplateFile(string FileName, Dictionary<string,string> Tokens )
        {
            List<string> lines = new List<string>();

            try
            {
                // Pass the file path and file name to the StreamReader constructor
                System.IO.StreamReader sr = new System.IO.StreamReader(FileName);

                // Read the first line of text
                string line = sr.ReadLine();

                // Continue to read until you reach end of file
                while (line != null)
                {
                    foreach(KeyValuePair<string, string> entry in Tokens)
                    {
                        line = line.Replace(entry.Key, entry.Value);
                    }
                    lines.Add(line);

                    //Read the next line
                    line = sr.ReadLine();
                }

                //close the file
                sr.Close();
            }
            catch(Exception e)
            {
                Debug.LogError(string.Format("Exception reading module file template ({0})", e.Message));
            }

            return lines;
        }

        private static void WriteFile(string FileName, List<string> Lines )
        {
            try
            {
                //Pass the filepath and filename to the StreamWriter Constructor
                System.IO.StreamWriter sw = new System.IO.StreamWriter(FileName);

                foreach (string line in Lines)
                {
                    //Write a line of text
                    sw.WriteLine(line);
                }

                //Close the file
                sw.Close();
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                Debug.LogError(string.Format("Exception while writing module file ({0})", e.Message));
            }
        }

        public static int ConfigureMaya(string mayaPath)
        {
             int ExitCode = 0;

             try {
                if (!System.IO.File.Exists(mayaPath))
                {
                    Debug.LogError (string.Format ("No maya installation found at {0}", mayaPath));
                    return -1;
                }

                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                myProcess.StartInfo.FileName = mayaPath;
                myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.StartInfo.UseShellExecute = false;

#if UNITY_EDITOR_OSX
                myProcess.StartInfo.Arguments = string.Format(@"-command '{0}'", MAYA_COMMANDS);
#elif UNITY_EDITOR_LINUX
                throw new NotImplementedException();
#else // UNITY_EDITOR_WINDOWS
                myProcess.StartInfo.Arguments = string.Format("-command \"{0}\"", MAYA_COMMANDS);
#endif
                myProcess.EnableRaisingEvents = true;
                myProcess.Start();
                myProcess.WaitForExit();
                ExitCode = myProcess.ExitCode;
                Debug.Log(string.Format("Ran maya: [{0}]\nWith args [{1}]\nResult {2}",
                            mayaPath, myProcess.StartInfo.Arguments, ExitCode));
             }
             catch (Exception e)
             {
                UnityEngine.Debug.LogError(string.Format ("Exception failed to start Maya ({0})", e.Message));
                ExitCode = -1;
             }
            return ExitCode;
        }

        public static bool InstallMaya(bool verbose = false)
        {
            // What's happening here is that we copy the module template to
            // the module path, basically:
            // - copy the template to the user Maya module path
            // - search-and-replace its tags
            // - done.
            // But it's complicated because we can't trust any files actually exist.

            string moduleTemplatePath = GetModuleTemplatePath();
            if (!System.IO.File.Exists(moduleTemplatePath))
            {
                Debug.LogError(string.Format("Missing Maya module file at: \"{0}\"", moduleTemplatePath));
                return false;
            }

            // Create the {USER} modules folder and empty it so it's ready to set up.
            string modulePath = GetModulePath();
            string moduleFilePath = System.IO.Path.Combine(modulePath, MODULE_FILENAME + ".mod");
            bool installed = false;

            if (!System.IO.Directory.Exists(modulePath))
            {
                if (verbose) { Debug.Log(string.Format("Creating Maya Modules Folder {0}", modulePath)); }

                try
                {
                    System.IO.Directory.CreateDirectory(modulePath);
                }
                catch (Exception xcp)
                {
                    Debug.LogException(xcp);
                    Debug.LogError(string.Format("Failed to create Maya Modules Folder {0}", modulePath));
                    return false;
                }

                if (!System.IO.Directory.Exists(modulePath)) {
                    Debug.LogError(string.Format("Failed to create Maya Modules Folder {0}", modulePath));
                    return false;
                }

                installed = false;
            }
            else
            {
                // detect if unityoneclick.mod is installed
                installed = System.IO.File.Exists(moduleFilePath);

                if (installed)
                {
                    // FIXME: remove this when we support parsing existing .mod files
                    try {
                        if (verbose) { Debug.Log(string.Format("Deleting module file {0}", moduleFilePath)); }
                        System.IO.File.Delete(moduleFilePath);
                        installed = false;
                    }
                    catch (Exception xcp)
                    {
                        Debug.LogException(xcp);
                        Debug.LogWarning(string.Format ("Failed to delete plugin module file {0}", moduleFilePath));
                    }
                }
            }

            // if not installed
            if (!installed)
            {
                Dictionary<string,string> Tokens = new Dictionary<string,string>()
                {
                    {VERSION_TAG, GetPackageVersion() },
                    {PROJECT_TAG, GetProjectPath() }
                 };

                // parse template, replace "{UnityProject}" with project path
                List<string> lines = ParseTemplateFile(moduleTemplatePath, Tokens);

                if (verbose) Debug.Log(string.Format("Copying plugin module file to {0}", moduleFilePath));

                // write out .mod file
                WriteFile(moduleFilePath, lines);
            }
            else
            {
                throw new NotImplementedException();

                // TODO: parse installed .mod file

                // TODO: if maya version not installed add

                // TODO: else check installation path

                // TODO: if installation path different

                // TODO: print message package already installed else where
            }

            return true;
        }
    }

    class MaxIntegration
    {
        private const string MaxScriptsPath = "FbxExporters/Integrations/Autodesk/max/scripts/";

        private const string PluginName = "UnityFbxForMaxPlugin.ms";
        private const string PluginPath = MaxScriptsPath + PluginName;

        private const string ConfigureMaxScript = MaxScriptsPath + "configureUnityFbxForMax.ms";

        private const string ExportSettingsFile = MaxScriptsPath + "unityFbxExportSettings.ms";

        private const string PluginSourceTag = "UnityPluginScript_Source";
        private const string PluginNameTag = "UnityPluginScript_Name";
        private const string ProjectTag = "UnityProject";
        private const string ExportSettingsTag = "UnityFbxExportSettings";

        // TODO: get this from the export settings
        private static string GetMaxExe(){
            return "C:/Program Files/Autodesk/3ds Max 2018/3dsmax.exe";
        }

        /// <summary>
        /// Gets the absolute Unity path for relative path in Assets folder.
        /// </summary>
        /// <returns>The absolute path.</returns>
        /// <param name="relPath">Relative path.</param>
        public static string GetAbsPath(string relPath){
            return Application.dataPath + "/" + relPath;
        }

        private static string GetInstallScript(){
            Dictionary<string,string> Tokens = new Dictionary<string,string>()
            {
                {PluginSourceTag, GetAbsPath(PluginPath) },
                {PluginNameTag,  PluginName },
                {ProjectTag,GetAbsPath("")},
                {ExportSettingsTag, GetAbsPath(ExportSettingsFile) }
            };

            var installScript = "";
            // setup the variables to be used in the configure max script
            foreach (var t in Tokens) {
                installScript += string.Format (@"global {0} = @\""{1}\"";", t.Key, t.Value);
            }
            installScript += string.Format(@"filein \""{0}\""", GetAbsPath(ConfigureMaxScript));
            return installScript;
        }

        public static int InstallMaxPlugin(){
            var maxExe = GetMaxExe ();
            var installScript = GetInstallScript ();

            int ExitCode = 0;

            try {
                if (!System.IO.File.Exists(maxExe))
                {
                    Debug.LogError (string.Format ("No 3DsMax installation found at {0}", maxExe));
                    return -1;
                }

                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                myProcess.StartInfo.FileName = maxExe;
                myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.StartInfo.UseShellExecute = false;

#if UNITY_EDITOR_OSX
                throw new NotImplementedException();
#elif UNITY_EDITOR_LINUX
                throw new NotImplementedException();
#else // UNITY_EDITOR_WINDOWS
                myProcess.StartInfo.Arguments = string.Format("-q -silent -mxs \"{0}\"", installScript);
#endif
                myProcess.EnableRaisingEvents = true;
                myProcess.Start();
                myProcess.WaitForExit();
                ExitCode = myProcess.ExitCode;
                Debug.Log(string.Format("Ran max: [{0}]\nWith args [{1}]\nResult {2}",
                    maxExe, myProcess.StartInfo.Arguments, ExitCode));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(string.Format ("Exception failed to start Max ({0})", e.Message));
                ExitCode = -1;
            }
            return ExitCode;
        }
    }

    class IntegrationsUI
    {
        /// <summary>
        /// The path of the Maya executable.
        /// </summary>
        public static string GetMayaExe () {
            return FbxExporters.EditorTools.ExportSettings.GetSelectedMayaPath ();
        }

        /// <summary>
        /// Opens a dialog showing whether the installation succeeded.
        /// </summary>
        /// <param name="dcc">Dcc name.</param>
        private static void ShowSuccessDialog(string dcc, int exitCode){
            string title, message;
            if (exitCode != 0) {
                title = string.Format("Failed to install {0} Integration.", dcc);
                message = string.Format("Failed to configure {0}, please check logs (exitcode={1}).", dcc, exitCode);
            } else {
                title = string.Format("Completed installation of {0} Integration.", dcc);
                message = string.Format("Enjoy the new Unity menu in {0}.", dcc);
            }
            UnityEditor.EditorUtility.DisplayDialog (title, message, "Ok");
        }

        public static void InstallMayaIntegration ()
        {
            var mayaExe = GetMayaExe ();
            if (string.IsNullOrEmpty (mayaExe)) {
                return;
            }

            if (!Integrations.InstallMaya(verbose: true)) {
                return;
            }

            int exitCode = Integrations.ConfigureMaya (mayaExe);
            ShowSuccessDialog ("Maya", exitCode);
        }

        public static void InstallMaxIntegration()
        {
            int exitCode = MaxIntegration.InstallMaxPlugin ();
            ShowSuccessDialog ("3DsMax", exitCode);
        }
    }
}
