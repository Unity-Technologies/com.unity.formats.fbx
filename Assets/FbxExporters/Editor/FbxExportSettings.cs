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
        const float SelectableLabelMinWidth = 100;
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
                "Export Path:",
                "Relative path for saving Model Prefabs."));

            var pathLabel = ExportSettings.GetRelativeSavePath();
            if (pathLabel == "./") { pathLabel = "(Assets root)"; }
            EditorGUILayout.SelectableLabel(pathLabel,
                EditorStyles.textField,
                GUILayout.MinWidth(SelectableLabelMinWidth),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            if (GUILayout.Button ("Browse", EditorStyles.miniButton, GUILayout.Width (BrowseButtonWidth))) {
                string initialPath = ExportSettings.GetAbsoluteSavePath();
                string fullPath = EditorUtility.OpenFolderPanel (
                        "Select Model Prefabs Path", initialPath, null
                        );

                // Unless the user canceled, make sure they chose something in the Assets folder.
                if (!string.IsNullOrEmpty (fullPath)) {
                    var relativePath = GetRelativePath(Application.dataPath, fullPath);
                    if (string.IsNullOrEmpty(relativePath)
                            || relativePath == ".."
                            || relativePath.StartsWith(".." + Path.DirectorySeparatorChar)) {
                        Debug.LogWarning ("Please select a location in the Assets folder");
                    } else {
                        ExportSettings.SetRelativeSavePath(relativePath);
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

        private string GetRelativePath(string fromDir, string toDir) {
            // https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
            // With fixes to handle that fromDir and toDir are both directories (not files).
            if (String.IsNullOrEmpty(fromDir)) throw new ArgumentNullException("fromDir");
            if (String.IsNullOrEmpty(toDir))   throw new ArgumentNullException("toDir");

            // MakeRelativeUri assumes the path is a file unless it ends with a
            // path separator, so add one. Having multiple in a row is no problem.
            fromDir += Path.DirectorySeparatorChar;
            toDir += Path.DirectorySeparatorChar;

            // Workaround for https://bugzilla.xamarin.com/show_bug.cgi?id=5921
            fromDir += Path.DirectorySeparatorChar;

            Uri fromUri = new Uri(fromDir);
            Uri toUri = new Uri(toDir);

            if (fromUri.Scheme != toUri.Scheme) { return null; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.IsNullOrEmpty(relativePath)) {
                // The relative path is empty if it's the same directory.
                relativePath = "./";
            }

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase)) {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }

    [FilePath("ProjectSettings/FbxExportSettings.asset",FilePathAttribute.Location.ProjectFolder)]
    public class ExportSettings : FbxExporters.EditorTools.ScriptableSingleton<ExportSettings>
    {
        public const string kDefaultSavePath = "Objects";

        public bool weldVertices = true;
        public bool embedTextures = false;
        public bool mayaCompatibleNames = true;
        public bool centerObjects = true;

        /// <summary>
        /// The path where Convert To Model will save the new fbx and prefab.
        /// This is relative to the Application.dataPath
        /// </summary>
        [SerializeField]
        string convertToModelSavePath = kDefaultSavePath;

        /// <summary>
        /// The path where Convert To Model will save the new fbx and prefab.
        /// This is relative to the Application.dataPath
        /// </summary>
        public static string GetRelativeSavePath() {
            var relativePath = instance.convertToModelSavePath;
            if (string.IsNullOrEmpty(relativePath)) {
                relativePath = kDefaultSavePath;
            }
            return relativePath;
        }

        /// <summary>
        /// The path where Convert To Model will save the new fbx and prefab.
        /// This is an absolute path
        /// </summary>
        public static string GetAbsoluteSavePath() {
            var relativePath = GetRelativeSavePath();
            var absolutePath = Path.Combine(Application.dataPath, relativePath);
            return Path.GetFullPath(absolutePath);
        }

        /// <summary>
        /// Set the path where Convert To Model will save the new fbx and prefab.
        /// This is interpreted as being relative to the Application.dataPath
        /// </summary>
        public static void SetRelativeSavePath(string newPath) {
            instance.convertToModelSavePath = newPath;
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
