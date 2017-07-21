using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace FbxExporters
{
    class Integrations
    {
        public const string MAYA_VERSION = "2017";
        private const string MODULE_FILENAME = "unityoneclick.mod";
        private const string PACKAGE_NAME = "FbxExporters";
        private const string VERSION_FILENAME = "README.txt";
        private const string VERSION_FIELD = "**Version**";
        private const string VERSION_TAG = "{Version}";
        private const string PROJECT_TAG = "{UnityProject}";
        private static string MAYA_COMMANDS { get { 
		// NOTE: we use @"" + (char) 34 to get the correct escape quoting for \"
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX 
            return string.Format(@"configureUnityOneClick {1}{0}{1} {1}{2}{1} 0; scriptJob -idleEvent quit;", 
                Integrations.GetProjectPath(), @"" + (char) 34, GetUnityPath(); 
#else
            return string.Format("configureUnityOneClick \\{1}{0}\\{1} {1}{2}{1} 0; scriptJob -idleEvent quit;", 
                Integrations.GetProjectPath().Replace("\\","/"), @"" + (char) 34, GetUnityPath().Replace("\\","/"); 
#endif
        }}
        private static Char[] FIELD_SEPARATORS = new Char[] {':'};

        private const string MODULE_TEMPLATE_PATH = "Integrations/Autodesk/maya"+VERSION_TAG+"/unityoneclick.mod";

#if UNITY_EDITOR_OSX
        private const string REL_MAYA_MODULES_PATH = "Library/Preferences/Autodesk/Maya/"+VERSION_TAG+"/modules";
#elif UNITY_EDITOR_LINUX 
        private const string REL_MAYA_MODULES_PATH = "Maya/"+VERSION_TAG+"/modules";
#else
        private const string REL_MAYA_MODULES_PATH = "maya/"+VERSION_TAG+"/modules";
#endif

        private static string GetUserFolder()
        {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            return System.Environment.GetEnvironmentVariable("HOME");
#else
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
#endif
        }

        private static string GetModulePath(string version)
        {
            string result = System.IO.Path.Combine(GetUserFolder(), REL_MAYA_MODULES_PATH);

            return result.Replace(VERSION_TAG,version);
        }

        private static string GetModuleTemplatePath(string version)
        {
            string result = System.IO.Path.Combine(Application.dataPath, MODULE_TEMPLATE_PATH);

            return result.Replace(VERSION_TAG,version);
        }

        public static string GetUnityPath()
        {
            return EditorApplication.applicationPath;
        }

        public static string GetProjectPath()
        {
            return System.IO.Directory.GetParent(Application.dataPath).FullName;
        }

        private static string GetPackagePath()
        {
            return System.IO.Path.Combine(Application.dataPath, PACKAGE_NAME);
        }

        private static string GetPackageVersion()
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
                    if (line.StartsWith(VERSION_FIELD))
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
                Debug.LogError(string.Format("Exception while writing module file ({0})", e.Message));
            }
        }

        public static int ConfigureMaya(string version)
        {
             int ExitCode = 0;

             try {
                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.StartInfo.UseShellExecute = false;

#if UNITY_EDITOR_OSX
                myProcess.StartInfo.FileName = "open";
                string mayaPath = string.Format ("/Applications/Autodesk/maya{0}/Maya.app", version);

                if (!System.IO.Directory.Exists(mayaPath))
                {
                    Debug.LogError (string.Format ("No maya installation found at {0}",mayaPath));
                    return -1;
                }

                string mayaCommandLine = string.Format(@"{0} --args -command '{1}'", mayaPath, MAYA_COMMANDS);
                myProcess.StartInfo.Arguments = "-a " + mayaCommandLine;
#elif UNITY_EDITOR_LINUX
                throw new NotImplementedException();
#else
                string mayaPath = string.Format ("C:/Program Files/Autodesk/Maya{0}/bin/maya.exe", version);

                if (!System.IO.File.Exists(mayaPath))
                {
                    Debug.LogError (string.Format ("No maya installation found at {0}", mayaPath));
                    return -1;
                }

                myProcess.StartInfo.FileName = mayaPath;
                myProcess.StartInfo.Arguments = string.Format("-command \"{0}\"", MAYA_COMMANDS);
#endif
                myProcess.EnableRaisingEvents = true;
                myProcess.Start();
                myProcess.WaitForExit();
                ExitCode = myProcess.ExitCode;
             }
             catch (Exception e)
             {
                UnityEngine.Debug.LogError(string.Format ("Exception failed to start Maya ({0})", e.Message));
                ExitCode = -1;
             }
            return ExitCode;
        }

        public static bool InstallMaya(string version, bool verbose = false)
        {
            // check if package installed
            string moduleTemplatePath = GetModuleTemplatePath(version);

            if (!System.IO.File.Exists(moduleTemplatePath))
            {
                Debug.LogError(string.Format("FbxExporters package not installed, please install first"));
                return false;
            }

            // check for {USER} modules folder
            string modulePath = GetModulePath(version);

            string moduleFilePath = System.IO.Path.Combine( modulePath, MODULE_FILENAME);

            bool installed = false;

            if (!System.IO.Directory.Exists(modulePath))
            {
                if (verbose) Debug.Log(string.Format("Creating Maya Modules Folder {0}", modulePath));

                try
                {
                    System.IO.Directory.CreateDirectory(modulePath);
                }
                catch
                {
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
                        System.IO.File.Delete(moduleFilePath);
                        installed = false;
                    }
                    catch
                    {
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

                 if (verbose) Debug.Log(string.Format("Copying plugin module file to {0}",moduleFilePath));

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

        public static void InstallMaya2017()
        {
            const string version = Integrations.MAYA_VERSION;

            Debug.Log(string.Format("Installing Maya {0} Integration", version));

            if (!InstallMaya (version)) {
                Debug.LogError (string.Format ("Failed to install Maya {0} Integration.", version));
            }
        }
    }

    namespace Editors
    {
        class IntegrationsUI
        {
            const string MenuItemName1 = "FbxExporters/Install Maya" + Integrations.MAYA_VERSION + " Integration";

            [MenuItem (MenuItemName1, false, 0)]
            public static void OnMenuItem1 ()
            {
            	if (Integrations.InstallMaya(Integrations.MAYA_VERSION))
            	{
                    int exitCode = Integrations.ConfigureMaya (Integrations.MAYA_VERSION);

                    string title = string.Format("Completed installation of Maya{0} Integration.", Integrations.MAYA_VERSION);
                    string message = "Maya will close when it has finished configuring integration.";

                    if (exitCode!=0)
                    {
                        message = string.Format("Failed to configure Maya, please check logs (exitcode={0})", exitCode);
                    }

                    UnityEditor.EditorUtility.DisplayDialog (title, message, "Ok");
                }
            }
        }
    }
}
