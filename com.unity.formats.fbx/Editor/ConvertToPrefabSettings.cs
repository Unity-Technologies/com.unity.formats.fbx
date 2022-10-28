using UnityEngine;

namespace UnityEditor.Formats.Fbx.Exporter
{
    [CustomEditor(typeof(ConvertToPrefabSettings))]
    internal class ConvertToPrefabSettingsEditor : UnityEditor.Editor
    {
        private const float DefaultLabelWidth = 175;
        private const float DefaultFieldOffset = 18;

        public float LabelWidth { get; set; } = DefaultLabelWidth;
        public float FieldOffset { get; set; } = DefaultFieldOffset;

        private string[] exportFormatOptions = new string[] { "ASCII", "Binary" };
        private string[] includeOptions = new string[] {"Model(s) + Animation"};
        private string[] lodOptions = new string[] {"All Levels"};

        private string[] objPositionOptions { get { return new string[] {"Local Pivot"}; }}

        public override void OnInspectorGUI()
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
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            // always greyed out, show only to let user know what will happen
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Popup(0, lodOptions);
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            // always greyed out, show only to let user know what will happen
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Popup(0, objPositionOptions);
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Animated Skinned Mesh"), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.SetAnimatedSkinnedMesh(EditorGUILayout.Toggle(exportSettings.AnimateSkinnedMesh));
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
            exportSettings.SetUseMayaCompatibleNames(EditorGUILayout.Toggle(exportSettings.UseMayaCompatibleNames));
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

    /// <summary>
    /// Class specifying the FBX export settings when converting to a Prefab Variant.
    /// </summary>
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

        /// <summary>
        /// The export format (binary or ascii).
        /// </summary>
        public ExportFormat ExportFormat
        {
            get { return exportFormat; }
            set { exportFormat = value; }
        }

        /// <summary>
        /// Option to export the model only, the animation only, or both the model and the animation.
        /// </summary>
        Include IExportOptions.ModelAnimIncludeOption
        {
            get { return Include.ModelAndAnim; }
        }

        /// <summary>
        /// The type of LOD to export (All, Highest or Lowest).
        /// </summary>
        LODExportType IExportOptions.LODExportType
        {
            get { return LODExportType.All; }
        }

        /// <summary>
        /// The position to export the object to (Local centered, World absolute, or Reset). Use Reset for converting to a Prefab.
        /// </summary>
        ObjectPosition IExportOptions.ObjectPosition
        {
            get { return ObjectPosition.Reset; }
        }

        /// <summary>
        /// Option to export the animation on GameObjects that have a skinned mesh.
        /// </summary>
        public bool AnimateSkinnedMesh
        {
            get { return animatedSkinnedMesh; }
            set { animatedSkinnedMesh = value; }
        }

        /// <summary>
        /// Option to convert the GameObject and material names to Maya compatible names.
        /// </summary>
        public bool UseMayaCompatibleNames
        {
            get { return mayaCompatibleNaming; }
            set { mayaCompatibleNaming = value; }
        }

        /// <summary>
        /// Option to change the GameObjects and material names in the scene to keep them
        /// Maya compatible after the export. Only works if UseMayaCompatibleNames is also enabled.
        /// </summary>
        bool IExportOptions.AllowSceneModification
        {
            get { return true; }
        }

        /// <summary>
        /// Option to export GameObjects that don't have a renderer.
        /// </summary>
        bool IExportOptions.ExportUnrendered
        {
            get { return true; }
        }

        /// <summary>
        /// Option to preserve the previous import settings after the export when overwriting an existing FBX file.
        /// </summary>
        bool IExportOptions.PreserveImportSettings
        {
            get { return false; }
        }

        /// <summary>
        /// Option to keep multiple instances of the same mesh as separate instances on export.
        /// </summary>
        bool IExportOptions.KeepInstances
        {
            get { return true; }
        }

        /// <summary>
        /// Option to embed textures in the exported FBX file.
        /// </summary>
        /// <remarks>
        /// To embed textures, you must set the file ExportFormat to binary.
        /// </remarks>
        bool IExportOptions.EmbedTextures
        {
            get { return false; }
        }

        /// <summary>
        /// The transform to transfer the animation from. The animation is transferred to AnimationDest.
        /// </summary>
        /// <remarks>
        /// Transform must be an ancestor of AnimationDest, and may be an ancestor of the selected GameObject.
        /// </remarks>
        public Transform AnimationSource
        {
            get { return animSource; }
            set { animSource = value; }
        }

        /// <summary>
        /// The transform to transfer the animation to.
        /// This GameObject receives the transform animation on GameObjects between Source
        /// and Destination as well as the animation on the Source itself.
        /// </summary>
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
