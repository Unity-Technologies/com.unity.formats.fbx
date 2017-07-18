using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace FbxExporters
{
    class Integrations
    {
        private const string MAYA_VERSION = "2017";
        private const string MODULE_FILENAME = "unityoneclick.mod";
        private const string PACKAGE_NAME = "FbxExporters";
        private const string VERSION_FILENAME = "README.txt";
        private const string VERSION_FIELD = "**Version**";
        private const string VERSION_TAG = "{Version}";
        private const string PROJECT_TAG = "{UnityProject}";

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

        private static string GetProjectPath()
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

        private static void _InstallMaya2017(bool verbose=true, bool commandsOnly=false)
        {
            // check if package installed
            string moduleTemplatePath = GetModuleTemplatePath(MAYA_VERSION);

            if (!System.IO.File.Exists(moduleTemplatePath))
            {
                Debug.LogError(string.Format("FbxExporters package not installed, please install first"));
                return;
            }

            // TODO: detect maya2017 installation

            // TODO:  if not maya2017 installed warn user

            // check for {USER} modules folder
            string modulePath = GetModulePath(MAYA_VERSION);

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
                    return;
                }

                if (!System.IO.Directory.Exists(modulePath)) {
                    Debug.LogError(string.Format("Failed to create Maya Modules Folder {0}", modulePath));
                    return;
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

            if (commandsOnly)
                throw new NotImplementedException();

            // TODO: configure maya to auto-load plugin on next startup

        }

        public static void InstallMaya2017()
        {
            bool verbose=true;

            Debug.Log(string.Format("Installing Maya {0} Integration",MAYA_VERSION));

            _InstallMaya2017(verbose);

            if (verbose) Debug.Log(string.Format("Finished installing Maya {0} Integration.",MAYA_VERSION));
        }

        public static void InstallMaya2017CommandsOnly()
        {
            bool verbose=true;

            Debug.Log(string.Format("Installing Maya {0} Integration Commands Only",MAYA_VERSION));

            _InstallMaya2017(verbose, true);

            if (verbose) Debug.Log(string.Format("Finished installing Maya {0} Integration Commands Only.",MAYA_VERSION));
        }
    }

    namespace Editors
    {
        class IntegrationsUI
        {
            const string MenuItemName = "FbxExporters/Install Maya2017 Integration";

            [MenuItem (MenuItemName, false, 0)]
            public static void OnMenuItem ()
            {
            	Integrations.InstallMaya2017();
            }
        }
    }
}