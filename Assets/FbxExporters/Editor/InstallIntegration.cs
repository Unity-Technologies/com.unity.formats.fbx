// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.
//
// Licensed under the ##LICENSENAME##.
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace FbxExporters.Editor
{
    abstract class DCCIntegration
    {
        public abstract string DccDisplayName { get; }
        public abstract string IntegrationZipPath { get; }

        private static string m_integrationFolderPath = null;
        public static string INTEGRATION_FOLDER_PATH
        {
            get{
                if (string.IsNullOrEmpty (m_integrationFolderPath)) {
                    m_integrationFolderPath = Application.dataPath;
                }
                return m_integrationFolderPath;
            }
            set{
                if (!string.IsNullOrEmpty (value) && System.IO.Directory.Exists (value)) {
                    m_integrationFolderPath = value;
                } else {
                    Debug.LogError (string.Format("Failed to set integration folder path, invalid directory \"{0}\"", value));
                }
            }
        }

        public void SetIntegrationFolderPath(string path){
            INTEGRATION_FOLDER_PATH = path;
        }

        /// <summary>
        /// Gets the integration zip full path as an absolute Unity-style path.
        /// </summary>
        /// <returns>The integration zip full path.</returns>
        public string GetIntegrationZipFullPath()
        {
            return Application.dataPath + "/" + IntegrationZipPath;
        }

        /// <summary>
        /// Gets the project path.
        /// </summary>
        /// <returns>The project path.</returns>
        public static string GetProjectPath()
        {
            return System.IO.Directory.GetParent(Application.dataPath).FullName.Replace("\\","/");
        }

        /// <summary>
        /// Installs the integration using the provided executable.
        /// </summary>
        /// <returns>The integration.</returns>
        /// <param name="exe">Exe.</param>
        public abstract int InstallIntegration(string exe);

        /// <summary>
        /// Determines if folder is already unzipped at the specified path.
        /// </summary>
        /// <returns><c>true</c> if folder is already unzipped at the specified path; otherwise, <c>false</c>.</returns>
        /// <param name="path">Path.</param>
        public abstract bool FolderAlreadyUnzippedAtPath (string path);

        /// <summary>
        /// Launches application at given path
        /// </summary>
        /// <param name="AppPath"></param>
        public static void LaunchDCCApplication(string AppPath)
        {
            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            myProcess.StartInfo.FileName = AppPath;
            myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            myProcess.StartInfo.CreateNoWindow = false;
            myProcess.StartInfo.UseShellExecute = false;

            myProcess.EnableRaisingEvents = false;
            myProcess.Start();
        }
    }


    class MayaIntegration : DCCIntegration
    {
        public override string DccDisplayName { get { return "Maya"; } }

        public override string IntegrationZipPath { get { return "FbxExporters/UnityFbxForMaya.zip"; } }

        private string FBX_EXPORT_SETTINGS_PATH { get { return "/Integrations/Autodesk/maya/scripts/unityFbxExportSettings.mel"; } }

        private string FBX_IMPORT_SETTINGS_PATH { get { return "/Integrations/Autodesk/maya/scripts/unityFbxImportSettings.mel"; } }

        private string MODULE_TEMPLATE_PATH { get { return "Integrations/Autodesk/maya/" + MODULE_FILENAME + ".txt"; } }
        private string MODULE_FILENAME { get { return "UnityFbxForMaya"; } }

        private const string PACKAGE_NAME = "FbxExporters";
        private const string VERSION_FILENAME = "README.txt";
        private const string VERSION_FIELD = "VERSION";
        private const string VERSION_TAG = "{Version}";
        private const string PROJECT_TAG = "{UnityProject}";
        private const string INTEGRATION_TAG = "{UnityIntegrationsPath}";

        private const string MAYA_USER_STARTUP_SCRIPT = "userSetup.mel";

        private const string UI_SETUP_FUNCTION = "unitySetupUI";
        private string USER_STARTUP_CALL { get { return string.Format ("if(`exists {0}`){{ {0}; }}", UI_SETUP_FUNCTION); } }

        private static string MAYA_DOCUMENTS_PATH {
            get {
                switch (Application.platform) {
                case RuntimePlatform.WindowsEditor:
                    return "maya";
                case RuntimePlatform.OSXEditor:
                    return "Library/Preferences/Autodesk/Maya";
                default:
                    throw new NotImplementedException ();
                }
            }
        }

        private static string MAYA_MODULES_PATH {
            get {
                return System.IO.Path.Combine(GetUserFolder(), MAYA_DOCUMENTS_PATH + "/modules");
            }
        }

        private static string MAYA_SCRIPTS_PATH {
            get {
                return System.IO.Path.Combine(GetUserFolder(), MAYA_DOCUMENTS_PATH + "/scripts");
            }
        }

        // Use string to define escaped quote
        // Windows needs the backslash
        protected static string ESCAPED_QUOTE {
            get {
                switch (Application.platform) {
                case RuntimePlatform.WindowsEditor:
                    return "\\\"";
                case RuntimePlatform.OSXEditor:
                    return "\"";
                default:
                    throw new NotImplementedException ();
                }
            }
        }

        protected string MAYA_CONFIG_COMMAND { get {
                return string.Format("unityConfigure {0}{1}{0} {0}{2}{0} {0}{3}{0} {4};",
                    ESCAPED_QUOTE, GetProjectPath(), GetExportSettingsPath(), GetImportSettingsPath(), (IsHeadlessInstall()?1:0));
            }}

        private string MAYA_CLOSE_COMMAND { get {
                return string.Format("scriptJob -idleEvent quit;");
        }}
        private static Char[] FIELD_SEPARATORS = new Char[] {':'};

        protected static string GetUserFolder()
        {
            switch (Application.platform) {
            case RuntimePlatform.WindowsEditor:
                return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            case RuntimePlatform.OSXEditor:
                return System.Environment.GetEnvironmentVariable("HOME");
            default:
                throw new NotImplementedException ();
            }
        }

        public static bool IsHeadlessInstall ()
        {
            return false;
        }

        public string GetModuleTemplatePath()
        {
            return System.IO.Path.Combine(INTEGRATION_FOLDER_PATH, MODULE_TEMPLATE_PATH);
        }

        public static string GetPackagePath()
        {
            return System.IO.Path.Combine(Application.dataPath, PACKAGE_NAME);
        }

        /// <summary>
        /// Gets the path to the export settings file.
        /// Returns a relative path with forward slashes as path separators.
        /// </summary>
        /// <returns>The export settings path.</returns>
        public string GetExportSettingsPath()
        {
            return INTEGRATION_FOLDER_PATH + FBX_EXPORT_SETTINGS_PATH;
        }

        /// <summary>
        /// Gets the path to the import settings file.
        /// Returns a relative path with forward slashes as path separators.
        /// </summary>
        /// <returns>The import settings path.</returns>
        public string GetImportSettingsPath(){
            return INTEGRATION_FOLDER_PATH + FBX_IMPORT_SETTINGS_PATH;
        }

        /// <summary>
        /// Gets the user startup script path.
        /// Returns a relative path with forward slashes as path separators.
        /// </summary>
        /// <returns>The user startup script path.</returns>
        private static string GetUserStartupScriptPath(){
            return MAYA_SCRIPTS_PATH + "/" + MAYA_USER_STARTUP_SCRIPT;
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

        /// <summary>
        /// Creates the missing directories in path.
        /// </summary>
        /// <returns><c>true</c>, if directory was created, <c>false</c> otherwise.</returns>
        /// <param name="path">Path to create.</param>
        protected static bool CreateDirectory(string path){
            try
            {
                System.IO.Directory.CreateDirectory(path);
            }
            catch (Exception xcp)
            {
                Debug.LogException(xcp);
                return false;
            }

            if (!System.IO.Directory.Exists(path)) {
                return false;
            }
            return true;
        }

        public int ConfigureMaya(string mayaPath)
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

                string commandString;

                switch (Application.platform) {
                case RuntimePlatform.WindowsEditor:
                    commandString = "-command \"{0}\"";
                    break;
                case RuntimePlatform.OSXEditor:
                    commandString = @"-command '{0}'";
                    break;
                default:
                    throw new NotImplementedException ();
                }

                if (EditorTools.ExportSettings.instance.launchAfterInstallation)
                {
                    myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    myProcess.StartInfo.CreateNoWindow = false;
                    myProcess.StartInfo.Arguments = string.Format(commandString, MAYA_CONFIG_COMMAND);
                }
                else
                {
                    myProcess.StartInfo.Arguments = string.Format(commandString, MAYA_CONFIG_COMMAND + MAYA_CLOSE_COMMAND);
                }

                myProcess.EnableRaisingEvents = true;
                myProcess.Start();

                if (!EditorTools.ExportSettings.instance.launchAfterInstallation)
                {
                    myProcess.WaitForExit();
                    ExitCode = myProcess.ExitCode;
                    Debug.Log(string.Format("Ran maya: [{0}]\nWith args [{1}]\nResult {2}",
                                mayaPath, myProcess.StartInfo.Arguments, ExitCode));
                }
                else
                {
                    ExitCode = 0;
                }
            }
             catch (Exception e)
             {
                UnityEngine.Debug.LogError(string.Format ("Exception failed to start Maya ({0})", e.Message));
                ExitCode = -1;
             }
            return ExitCode;
        }

        public bool InstallMaya(bool verbose = false)
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
            string modulePath = MAYA_MODULES_PATH;
            string moduleFilePath = System.IO.Path.Combine(modulePath, MODULE_FILENAME + ".mod");
            bool installed = false;

            if (!System.IO.Directory.Exists(modulePath))
            {
                if (verbose) { Debug.Log(string.Format("Creating Maya Modules Folder {0}", modulePath)); }
                if (!CreateDirectory (modulePath)) {
                    Debug.LogError(string.Format("Failed to create Maya Modules Folder {0}", modulePath));
                    return false;
                }
                installed = false;
            }
            else
            {
                // detect if UnityFbxForMaya.mod is installed
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
                    {PROJECT_TAG, GetProjectPath() },
                    {INTEGRATION_TAG, INTEGRATION_FOLDER_PATH },
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

            return SetupUserStartupScript (verbose);
        }

        private bool SetupUserStartupScript(bool verbose = false){
            // setup user startup script
            string mayaStartupScript = GetUserStartupScriptPath ();
            string fileContents = string.Format("\n{0}", USER_STARTUP_CALL);

            // make sure scripts directory exists
            if (!System.IO.Directory.Exists(MAYA_SCRIPTS_PATH))
            {
                if (verbose) { Debug.Log(string.Format("Creating Maya Scripts Folder {0}", MAYA_SCRIPTS_PATH)); }
                if (!CreateDirectory (MAYA_SCRIPTS_PATH)) {
                    Debug.LogError(string.Format("Failed to create Maya Scripts Folder {0}", MAYA_SCRIPTS_PATH));
                    return false;
                }
            }
            else if (System.IO.File.Exists (mayaStartupScript)) {
                // script exists, check that the UI setup is being called
                try{
                    using (System.IO.StreamReader sr = new System.IO.StreamReader (mayaStartupScript)) {
                        while (sr.Peek () >= 0) {
                            string line = sr.ReadLine ();
                            if (line.Trim().Contains (UI_SETUP_FUNCTION)) {
                                // startup call already in the file, nothing to do
                                return true;
                            }
                        }
                    }
                }
                catch(Exception e){
                    Debug.LogException(e);
                    Debug.LogError(string.Format("Exception while reading user startup file ({0})", e.Message));
                    return false;
                }
            }

            // append text to file
            try{
                System.IO.File.AppendAllText (mayaStartupScript, fileContents);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                Debug.LogError(string.Format("Exception while writing to user startup file ({0})", e.Message));
                return false;
            }
            return true;
        }

        public override int InstallIntegration (string mayaExe)
        {
            if (!InstallMaya(verbose: true)) {
                return -1;
            }

            return ConfigureMaya (mayaExe);
        }

        /// <summary>
        /// Determines if folder is already unzipped at the specified path
        /// by checking if UnityFbxForMaya.txt exists at expected location.
        /// </summary>
        /// <returns><c>true</c> if folder is already unzipped at the specified path; otherwise, <c>false</c>.</returns>
        /// <param name="path">Path.</param>
        public override bool FolderAlreadyUnzippedAtPath(string path)
        {
            if (string.IsNullOrEmpty (path)) {
                return false;
            }
            return System.IO.File.Exists (System.IO.Path.Combine (path, MODULE_TEMPLATE_PATH));
        }
    }

    class MayaLTIntegration : MayaIntegration 
    {
        public override string DccDisplayName { get { return "Maya LT"; } }
    }

    class MaxIntegration : DCCIntegration
    {
        public override string DccDisplayName { get { return "3Ds Max"; } }

        private const string MaxScriptsPath = "Integrations/Autodesk/max/scripts/";

        private const string PluginName = "UnityFbxForMaxPlugin.ms";
        public const string PluginPath = MaxScriptsPath + PluginName;

        private const string ConfigureMaxScript = MaxScriptsPath + "configureUnityFbxForMax.ms";

        private const string ExportSettingsFile = MaxScriptsPath + "unityFbxExportSettings.ms";

        private const string PluginSourceTag = "UnityPluginScript_Source";
        private const string PluginNameTag = "UnityPluginScript_Name";
        private const string ProjectTag = "UnityProject";
        private const string ExportSettingsTag = "UnityFbxExportSettings";

        public override string IntegrationZipPath { get { return "FbxExporters/UnityFbxForMax.zip"; } }

        /// <summary>
        /// Gets the absolute Unity path for relative path in Integrations folder.
        /// </summary>
        /// <returns>The absolute path.</returns>
        /// <param name="relPath">Relative path.</param>
        public static string GetAbsPath(string relPath){
            return MayaIntegration.INTEGRATION_FOLDER_PATH + "/" + relPath;
        }

        private static string GetInstallScript(){
            Dictionary<string,string> Tokens = new Dictionary<string,string>()
            {
                {PluginSourceTag, GetAbsPath(PluginPath) },
                {PluginNameTag,  PluginName },
                {ProjectTag, GetProjectPath() },
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

        public static int InstallMaxPlugin(string maxExe){
            if (Application.platform != RuntimePlatform.WindowsEditor) {
                Debug.LogError ("The 3DsMax Unity plugin is Windows only, please try installing a Maya plugin instead");
                return -1;
            }

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

                myProcess.StartInfo.Arguments = string.Format("-q -silent -mxs \"{0}\"", installScript);

                myProcess.EnableRaisingEvents = true;
                myProcess.Start();
                myProcess.WaitForExit();
                ExitCode = myProcess.ExitCode;

                if (EditorTools.ExportSettings.instance.launchAfterInstallation)
                {
                    LaunchDCCApplication(maxExe);
                }

                // TODO (UNI-29910): figure out what exactly causes this exit code + how to resolve
                if (ExitCode == -1073740791){
                    Debug.Log(string.Format("Detected 3ds max exitcode {0} -- safe to ignore", ExitCode));
                    ExitCode = 0;
                }

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

        public override int InstallIntegration(string maxExe){
            return MaxIntegration.InstallMaxPlugin (maxExe);
        }

        /// <summary>
        /// Determines if folder is already unzipped at the specified path
        /// by checking if plugin exists at expected location.
        /// </summary>
        /// <returns><c>true</c> if folder is already unzipped at the specified path; otherwise, <c>false</c>.</returns>
        /// <param name="path">Path.</param>
        public override bool FolderAlreadyUnzippedAtPath(string path)
        {
            if (string.IsNullOrEmpty (path)) {
                return false;
            }
            return System.IO.File.Exists (System.IO.Path.Combine (path, MaxIntegration.PluginPath));
        }
    }

    class IntegrationsUI
    {
        /// <summary>
        /// The path of the DCC executable.
        /// </summary>
        public static string GetDCCExe () {
            return FbxExporters.EditorTools.ExportSettings.GetSelectedDCCPath ();
        }

        /// <summary>
        /// Gets the name of the selected DCC.
        /// </summary>
        /// <returns>The DCC name.</returns>
        public static string GetDCCName() {
            return FbxExporters.EditorTools.ExportSettings.GetSelectedDCCName ();
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

        private static string DefaultIntegrationSavePath
        {
            get{
                return Application.dataPath;
            }
        }

        private static string LastIntegrationSavePath = DefaultIntegrationSavePath;

        public static void InstallDCCIntegration ()
        {
            var dccExe = GetDCCExe ();
            if (string.IsNullOrEmpty (dccExe)) {
                return;
            }

            string dccType = System.IO.Path.GetFileNameWithoutExtension (dccExe).ToLower();
            DCCIntegration dccIntegration;
            if (dccType.Equals ("maya")) {
                // could be Maya or Maya LT
                if (GetDCCName ().ToLower ().Contains ("lt")) {
                    dccIntegration = new MayaLTIntegration ();
                } else {
                    dccIntegration = new MayaIntegration ();
                }
            } else if (dccType.Equals ("3dsmax")) {
                dccIntegration = new MaxIntegration ();
            } else {
                throw new System.NotImplementedException ();
            }

            if (!GetIntegrationFolder (dccIntegration)) {
                // failed to get integration folder
                return;
            }
            int exitCode = dccIntegration.InstallIntegration (dccExe);
            ShowSuccessDialog (dccIntegration.DccDisplayName, exitCode);
        }

        private static bool GetIntegrationFolder(DCCIntegration dcc){
            // decompress zip file if it exists, otherwise try using default location
            var zipPath = dcc.GetIntegrationZipFullPath();
            if (System.IO.File.Exists (zipPath)) {
                return DecompressIntegrationZipFile (zipPath, dcc);
            }
            dcc.SetIntegrationFolderPath (DefaultIntegrationSavePath);
            return true;
        }

        private static bool DecompressIntegrationZipFile(string zipPath, DCCIntegration dcc)
        {
            // prompt user to enter location to unzip file
            var unzipFolder = EditorUtility.OpenFolderPanel(string.Format("Select Location to Save {0} Integration", dcc.DccDisplayName),LastIntegrationSavePath,"");
            if (string.IsNullOrEmpty (unzipFolder)) {
                // user has cancelled, do nothing
                return false;
            }

            // check that this is a valid location to unzip the file
            if (!DirectoryHasWritePermission (unzipFolder)) {
                // display dialog to try again or cancel
                var result = UnityEditor.EditorUtility.DisplayDialog ("No Write Permission",
                    string.Format("Directory \"{0}\" does not have write access", unzipFolder),
                    "Select another Directory", 
                    "Cancel"
                );

                if (result) {
                    InstallDCCIntegration ();
                } else {
                    return false;
                }
            }

            LastIntegrationSavePath = unzipFolder;

            // if file already unzipped in this location, then prompt user
            // if they would like to continue unzipping or use what is there
            if (dcc.FolderAlreadyUnzippedAtPath (unzipFolder)) {
                var result = UnityEditor.EditorUtility.DisplayDialogComplex ("Integrations Exist at Path",
                    string.Format ("Directory \"{0}\" already contains the decompressed integration", unzipFolder),
                    "Overwrite", 
                    "Use Existing",
                    "Cancel"
                );

                if (result == 0) {
                    DecompressZip (zipPath, unzipFolder);
                } else if (result == 2) {
                    return false;
                }
            } else {
                // unzip Integration folder
                DecompressZip (zipPath, unzipFolder);
            }

            dcc.SetIntegrationFolderPath(unzipFolder);

            return true;
        }

        /// <summary>
        /// Make sure we can write to this directory.
        /// Try creating a file in path directory, if it raises an error, then we can't
        /// write here.
        /// TODO: find a more reliable way to check this
        /// </summary>
        /// <returns><c>true</c>, if possible to write to path, <c>false</c> otherwise.</returns>
        /// <param name="path">Path.</param>
        public static bool DirectoryHasWritePermission(string path)
        {
            try
            {
                using (System.IO.FileStream fs = System.IO.File.Create(
                    System.IO.Path.Combine(
                        path, 
                        System.IO.Path.GetRandomFileName()
                    ), 
                    1,
                    System.IO.FileOptions.DeleteOnClose)
                )
                { }
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public static void DecompressZip(string zipPath, string destPath){
            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            var ZIPAPP = "7z.exe";
            if (Application.platform == RuntimePlatform.OSXEditor) {
                ZIPAPP = "7za";
            }
            myProcess.StartInfo.FileName = EditorApplication.applicationContentsPath + "/Tools/" + ZIPAPP;
            myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.StartInfo.UseShellExecute = false;

            // Command line flags used:
            // x : extract the zip contents so that they maintain the file hierarchy
            // -o : specify where to extract contents
            // -r : recurse subdirectories
            // -y : auto yes to all questions (without this Unity freezes as the process waits for a response)
            myProcess.StartInfo.Arguments = string.Format("x \"{0}\" -o\"{1}\" -r -y", zipPath, destPath);
            myProcess.EnableRaisingEvents = true;
            myProcess.Start();
            myProcess.WaitForExit();

            // in case we unzip inside the Assets folder, make sure it updates
            AssetDatabase.Refresh ();
        }
    }
}
