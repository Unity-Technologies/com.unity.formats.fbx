using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;

namespace FbxExporters.EditorTools {

    [CustomEditor(typeof(ExportSettings))]
    public class ExportSettingsEditor : UnityEditor.Editor {
        Vector2 scrollPos = Vector2.zero;
        const float LabelWidth = 225;
        const float SelectableLabelMinWidth = 200;
        const float BrowseButtonWidth = 55;

        public override void OnInspectorGUI() {
            ExportSettings exportSettings = (ExportSettings)target;

            // Increasing the label width so that none of the text gets cut off
            EditorGUIUtility.labelWidth = LabelWidth;

            scrollPos = GUILayout.BeginScrollView (scrollPos);

            exportSettings.weldVertices = EditorGUILayout.Toggle ("Weld Vertices:", exportSettings.weldVertices);
            exportSettings.embedTextures = EditorGUILayout.Toggle ("Embed Textures:", exportSettings.embedTextures);
            exportSettings.mayaCompatibleNames = EditorGUILayout.Toggle (
                new GUIContent ("Convert to Maya Compatible Naming:",
                    "In Maya some symbols such as spaces and accents get replaced when importing an FBX " +
                    "(e.g. \"foo bar\" becomes \"fooFBXASC032bar\"). " +
                    "On export, convert the names of GameObjects so they are Maya compatible." +
                    (exportSettings.mayaCompatibleNames ? "" : 
                        "\n\nWARNING: Disabling this feature may result in lost material connections," +
                    " and unexpected character replacements in Maya.")
                ),
                exportSettings.mayaCompatibleNames);
            
            exportSettings.centerObjects = EditorGUILayout.Toggle (
                new GUIContent("Center Objects:",
                    "Export objects centered around the union of the bounding box of selected objects"),
                exportSettings.centerObjects
            );
                
            GUILayout.BeginHorizontal ();
            GUILayout.Label (new GUIContent (
                "Model Prefab Path:",
                "Relative path for saving Model Prefabs."));

            EditorGUILayout.SelectableLabel(GetRelativePath(exportSettings.convertToModelSavePath, Application.dataPath),
                EditorStyles.textField, GUILayout.MinWidth(SelectableLabelMinWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));

            if (GUILayout.Button ("Browse", EditorStyles.miniButton, GUILayout.Width (BrowseButtonWidth))) {
                string path = EditorUtility.OpenFolderPanel (
                    "Select Model Prefabs Path", Application.dataPath, null
                );
                // Unless the user canceled, make sure they chose something in the Assets folder.
                if (!string.IsNullOrEmpty (path)) {
                   if(path.StartsWith (Application.dataPath)) {
                       exportSettings.convertToModelSavePath = path;
                   } else {
                       Debug.LogWarning ("Please select a location in Assets/");
                   }
                }
            }

            GUILayout.EndHorizontal ();
            GUILayout.FlexibleSpace ();
            GUILayout.EndScrollView ();

            if (GUI.changed) {
                EditorUtility.SetDirty (exportSettings);
                exportSettings.Save ();
            }
        }

        private string GetRelativePath(string filePath, string folder){
            Uri pathUri;
            try{
                pathUri = new Uri (filePath);
            }
            catch(UriFormatException){
                return filePath;
            }
            if (!folder.EndsWith (Path.DirectorySeparatorChar.ToString ())) {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri (folder);
            string relativePath = Uri.UnescapeDataString (
                                      folderUri.MakeRelativeUri (pathUri).ToString ().Replace ('/', Path.DirectorySeparatorChar)
                                  );
            if (!relativePath.StartsWith ("Assets")) {
                relativePath = string.Format("Assets{0}{1}", Path.DirectorySeparatorChar, relativePath);
            }
            return relativePath;
        }
    }

    [FilePath("ProjectSettings/FbxExportSettings.asset",FilePathAttribute.Location.ProjectFolder)]
    public class ExportSettings : FbxExporters.EditorTools.ScriptableSingleton<ExportSettings>
    {
        public bool weldVertices = true;
        public bool embedTextures = false;
        public bool mayaCompatibleNames = true;
        public bool centerObjects = true;
        public string convertToModelSavePath;

        void OnEnable()
        {
            convertToModelSavePath = Path.Combine (Application.dataPath, "Objects");
        }

        [MenuItem("Edit/Project Settings/Fbx Export", priority = 300)]
        static void ShowManager()
        {
            instance.name = "Fbx Export Settings";
            Selection.activeObject = instance;
        }

        public void Save()
        {
            instance.Save (true);
        }
    }

    public class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T s_Instance;
        public static T instance
        {
            get
            {
                if (ScriptableSingleton<T>.s_Instance == null)
                {
                    return ScriptableSingleton<T>.CreateAndLoad();
                }
                return ScriptableSingleton<T>.s_Instance;
            }
        }

        protected ScriptableSingleton()
        {
            if (ScriptableSingleton<T>.s_Instance != null)
            {
                Debug.LogError("ScriptableSingleton already exists. Did you query the singleton in a constructor?");
            }
        }
        private static T CreateAndLoad()
        {
            string filePath = ScriptableSingleton<T>.GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                var loaded = InternalEditorUtility.LoadSerializedFileAndForget(filePath);

                if (loaded.Length > 0) {
                    ScriptableSingleton<T>.s_Instance = loaded [0] as T;
                }
            }
            if (ScriptableSingleton<T>.s_Instance == null)
            {
                T t = ScriptableObject.CreateInstance<T>();
                ScriptableSingleton<T>.s_Instance = t;
            }
            return ScriptableSingleton<T>.s_Instance;
        }
        protected virtual void Save(bool saveAsText)
        {
            if (ScriptableSingleton<T>.s_Instance == null)
            {
                Debug.Log("Cannot save ScriptableSingleton: no instance!");
                return;
            }
            string filePath = ScriptableSingleton<T>.GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                string directoryName = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                InternalEditorUtility.SaveToSerializedFileAndForget(new T[]
                    {
                        ScriptableSingleton<T>.s_Instance
                    }, filePath, saveAsText);
            }
        }
        private static string GetFilePath()
        {
            Type typeFromHandle = typeof(T);
            object[] customAttributes = typeFromHandle.GetCustomAttributes(true);
            object[] array = customAttributes;
            for (int i = 0; i < array.Length; i++)
            {
                object obj = array[i];
                if (obj is FilePathAttribute)
                {
                    FilePathAttribute filePathAttribute = obj as FilePathAttribute;
                    return filePathAttribute.filepath;
                }
            }
            return null;
        }
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class FilePathAttribute : Attribute
    {
        public enum Location
        {
            PreferencesFolder,
            ProjectFolder
        }
        public string filepath
        {
            get;
            set;
        }
        public FilePathAttribute(string relativePath, FilePathAttribute.Location location)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                Debug.LogError("Invalid relative path! (its null or empty)");
                return;
            }
            if (relativePath[0] == '/')
            {
                relativePath = relativePath.Substring(1);
            }
            if (location == FilePathAttribute.Location.PreferencesFolder)
            {
                this.filepath = InternalEditorUtility.unityPreferencesFolder + "/" + relativePath;
            }
            else
            {
                this.filepath = relativePath;
            }
        }
    }

}
