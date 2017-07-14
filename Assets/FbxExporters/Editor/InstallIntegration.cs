using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

// NOTE: uncomment this define to enable installation from menu item
//#define DEBUG_INSTALLER 1

namespace FbxExporters
{
   class Integrations
   {
        private const string PACKAGE_NAME = "FbxExporters";
        private const string VERSION_FILENAME = "README.txt";
        private const string VERSION_FIELD = "**Version**";
        private static Char[] FIELD_SEPARATORS = new Char[] {':'};

        private const string REL_MODULE_TEMPLATE_PATH = "Integrations/Autodesk/maya<version>/unityoneclick.mod";

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private const string REL_MAYA_MODULES_PATH = "Library/Preferences/Autodesk/Maya/<version>/modules";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        private const string REL_MAYA_MODULES_PATH = "Maya/<version>/modules";
#else
        private const string REL_MAYA_MODULES_PATH = "My Documents/Maya/<version>/modules";
#endif

        private static string GetUserFolder()
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            return System.Environment.GetEnvironmentVariable("HOME");
#else
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)
#endif
        }

        private static string GetModulePath(string version)
        {
            string result = System.IO.Path.Combine(GetUserFolder(), REL_MAYA_MODULES_PATH);

            return result.Replace("<version>",version);
        }

        private static string GetModuleTemplatePath(string version)
        {
            string result = System.IO.Path.Combine(Application.dataPath, REL_MODULE_TEMPLATE_PATH);

            return result.Replace("<version>",version);
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

#if DEBUG_INSTALLER
        const string MenuItemName = "File/Install Maya2017 Integration";
        [MenuItem (MenuItemName, false)]
        public static void OnMenuItem ()
        {
        	InstallMaya2017 ();
        }
#endif

        public static void InstallMaya2017()
        {
            Debug.Log("Installing Maya2017 Integration");

            // check if package installed
            string moduleTemplatePath = GetModuleTemplatePath("2017");

            if (!System.IO.File.Exists(moduleTemplatePath))
            {
                Debug.LogError(string.Format("FbxExporters package not installed, please install first"));
                return;
            }

            // TODO: detect maya2017 installation

            // TODO:  if not maya2017 installed warn user

            // check for {USER} modules folder
            string modulePath = GetModulePath("2017");
            string moduleFilePath = System.IO.Path.Combine( modulePath, "unityoneclick.mod");

            bool installed = false;

            if (!System.IO.Directory.Exists(modulePath))
            {
                Debug.Log(string.Format("Creating Maya Modules Folder {0}", modulePath));

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
                        Debug.LogError(string.Format ("Failed to delete plugin module file {0}", moduleFilePath));
                    }
                }
            }

            // if not installed
            if (!installed)
            {
                Dictionary<string,string> Tokens = new Dictionary<string,string>()
                {
                    {"{UnityOneClickVersion}", GetPackageVersion() },
                    {"{UnityProject}", GetProjectPath() }
                 };

                // parse template, replace "{UnityProject}" with project path
                List<string> lines = ParseTemplateFile(moduleTemplatePath, Tokens);

                Debug.Log(string.Format("Installing plugin module file: {0}",moduleFilePath));

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

            // TODO: configure maya to auto-load plugin on next startup

            Debug.Log("Finished installing Maya 2017 Integration.");
        }
   }
}