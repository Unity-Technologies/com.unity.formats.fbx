using UnityEngine;

namespace UnityEditor.Formats.Fbx.Exporter
{
    [CustomEditor (typeof(ConvertToPrefabSettings))]
    internal class ConvertToPrefabSettingsEditor : UnityEditor.Editor
    {
        private const float DefaultLabelWidth = 175;
        private const float DefaultFieldOffset = 18;

        public float LabelWidth { get; set; } = DefaultLabelWidth;
        public float FieldOffset { get; set; } = DefaultFieldOffset;

        private string[] exportFormatOptions = new string[]{ "ASCII", "Binary" };
        private string[] includeOptions = new string[]{"Model(s) + Animation"};
        private string[] lodOptions = new string[]{"All Levels"};

        private string[] objPositionOptions { get { return new string[]{"Local Pivot"}; }}

        public override void OnInspectorGUI ()
        {
            var exportSettings = ((ConvertToPrefabSettings)target).info;

            EditorGUIUtility.labelWidth = LabelWidth;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Format", "Export the FBX file in the standard binary format." +
                " Select ASCII to export the FBX file in ASCII format."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.SetExportFormat((ExportFormat)EditorGUILayout.Popup((int)exportSettings.ExportFormat, exportFormatOptions));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Include", "Select whether to export models, animation or both."), GUILayout.Width(LabelWidth - FieldOffset));
            // always greyed out, show only to let user know what will happen
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Popup(0, includeOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            // always greyed out, show only to let user know what will happen
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Popup(0, lodOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            // always greyed out, show only to let user know what will happen
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Popup(0, objPositionOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Animated Skinned Mesh"), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.SetAnimatedSkinnedMesh(EditorGUILayout.Toggle (exportSettings.AnimateSkinnedMesh));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Compatible Naming",
                    "In Maya some symbols such as spaces and accents get replaced when importing an FBX " +
                    "(e.g. \"foo bar\" becomes \"fooFBXASC032bar\"). " +
                    "On export, convert the names of GameObjects so they are Maya compatible." +
                    (exportSettings.UseMayaCompatibleNames ? "" :
                        "\n\nWARNING: Disabling this feature may result in lost material connections," +
                        " and unexpected character replacements in Maya.")),
                    GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.SetUseMayaCompatibleNames(EditorGUILayout.Toggle (exportSettings.UseMayaCompatibleNames));
            GUILayout.EndHorizontal();
        }
    }

    internal class ConvertToPrefabSettings : ExportOptionsSettingsBase<ConvertToPrefabSettingsSerialize>
    {}

    /// <summary>
    /// Class specifying the FBX export settings when converting to a Prefab Variant.
    /// </summary>
    [System.Serializable]
    internal class ConvertToPrefabSettingsSerialize : ExportOptionsSettingsSerializeBase
    {
        /// <inheritdoc/>
        public override Include ModelAnimIncludeOption { get { return Include.ModelAndAnim; } }

        /// <inheritdoc/>
        public override LODExportType LODExportType { get { return LODExportType.All; } }

        /// <inheritdoc/>
        public override ObjectPosition ObjectPosition { get { return ObjectPosition.Reset; } }

        /// <inheritdoc/>
        public override bool ExportUnrendered { get { return true; } }

        /// <inheritdoc/>
        public override bool AllowSceneModification { get { return true; } }
    }

    [System.Serializable]
    public class ConvertToPrefabVariantOptions : IExportOptions
    {
        [SerializeField]
        private ExportFormat exportFormat = ExportFormat.ASCII;
        [SerializeField]
        private bool animatedSkinnedMesh = false;
        [SerializeField]
        private bool mayaCompatibleNaming = true;

        [System.NonSerialized]
        private Transform animSource;
        [System.NonSerialized]
        private Transform animDest;

        public ExportFormat ExportFormat
        {
            get { return exportFormat; }
            set { exportFormat = value; }
        }

        public Include ModelAnimIncludeOption { get { return Include.ModelAndAnim; } }

        public LODExportType LODExportType { get { return LODExportType.All; } }

        public ObjectPosition ObjectPosition { get { return ObjectPosition.Reset; } }

        public bool AnimateSkinnedMesh
        {
            get { return animatedSkinnedMesh; }
            set { animatedSkinnedMesh = value; }
        }

        public bool UseMayaCompatibleNames
        {
            get { return mayaCompatibleNaming; }
            set { mayaCompatibleNaming = value; }
        }

        public bool AllowSceneModification { get { return true; } }

        public bool ExportUnrendered { get { return true; } }

        public bool PreserveImportSettings
        {
            get { return false; }
        }

        public bool KeepInstances
        {
            get { return true; }
        }

        public bool EmbedTextures
        {
            get { return false; }
        }

        public Transform AnimationSource
        {
            get { return animSource; }
            set { animSource = value; }
        }

        public Transform AnimationDest
        {
            get { return animDest; }
            set { animDest = value; }
        }

        internal ConvertToPrefabSettingsSerialize ConvertToModelSettingsSerialize()
        {
            var exportSettings = new ConvertToPrefabSettingsSerialize();
            exportSettings.SetAnimatedSkinnedMesh(animatedSkinnedMesh);
            exportSettings.SetAnimationDest(animDest);
            exportSettings.SetAnimationSource(animSource);
            exportSettings.SetExportFormat(exportFormat);
            exportSettings.SetUseMayaCompatibleNames(mayaCompatibleNaming);

            return exportSettings;
        }
    }
}