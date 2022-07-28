using UnityEngine;

namespace UnityEditor.Formats.Fbx.Exporter
{
    [CustomEditor(typeof(ExportModelSettings))]
    internal class ExportModelSettingsEditor : UnityEditor.Editor
    {
        private const float DefaultLabelWidth = 175;
        private const float DefaultFieldOffset = 18;

        public float LabelWidth { get; set; } = DefaultLabelWidth;
        public float FieldOffset { get; set; } = DefaultFieldOffset;

        private string[] exportFormatOptions = new string[] { "ASCII", "Binary" };
        private string[] includeOptions = new string[] {"Model(s) Only", "Animation Only", "Model(s) + Animation"};
        private string[] lodOptions = new string[] {"All Levels", "Highest", "Lowest"};

        public const string singleHierarchyOption = "Local Pivot";
        public const string multiHerarchyOption = "Local Centered";
        private string hierarchyDepOption = singleHierarchyOption;
        private string[] objPositionOptions { get { return new string[] {hierarchyDepOption, "World Absolute"}; }}

        private bool disableIncludeDropdown = false;

        private bool m_exportingOutsideProject = false;
        public void SetExportingOutsideProject(bool val)
        {
            m_exportingOutsideProject = val;
        }

        public void SetIsSingleHierarchy(bool singleHierarchy)
        {
            if (singleHierarchy)
            {
                hierarchyDepOption = singleHierarchyOption;
                return;
            }
            hierarchyDepOption = multiHerarchyOption;
        }

        public void DisableIncludeDropdown(bool disable)
        {
            disableIncludeDropdown = disable;
        }

        public override void OnInspectorGUI()
        {
            var exportSettings = ((ExportModelSettings)target).info;

            EditorGUIUtility.labelWidth = LabelWidth;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Format", "Export the FBX file in the standard binary format." +
                " Select ASCII to export the FBX file in ASCII format."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.SetExportFormat((ExportFormat)EditorGUILayout.Popup((int)exportSettings.ExportFormat, exportFormatOptions));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Include", "Select whether to export models, animation or both."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUI.BeginDisabledGroup(disableIncludeDropdown);
            exportSettings.SetModelAnimIncludeOption((Include)EditorGUILayout.Popup((int)exportSettings.ModelAnimIncludeOption, includeOptions));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == Include.Anim);
            exportSettings.SetLODExportType((LODExportType)EditorGUILayout.Popup((int)exportSettings.LODExportType, lodOptions));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == Include.Anim);
            exportSettings.SetObjectPosition((ObjectPosition)EditorGUILayout.Popup((int)exportSettings.ObjectPosition, objPositionOptions));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Animated Skinned Mesh",
                "If checked, animation on objects with skinned meshes will be exported"), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if model
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == Include.Model);
            exportSettings.SetAnimatedSkinnedMesh(EditorGUILayout.Toggle(exportSettings.AnimateSkinnedMesh));
            EditorGUI.EndDisabledGroup();
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

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Unrendered",
                "If checked, meshes will be exported even if they don't have a Renderer component."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == Include.Anim);
            exportSettings.SetExportUnrendered(EditorGUILayout.Toggle(exportSettings.ExportUnrendered));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Preserve Import Settings",
                "If checked, the import settings from the overwritten FBX will be carried over to the new version."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if exporting outside assets folder
            EditorGUI.BeginDisabledGroup(m_exportingOutsideProject);
            exportSettings.SetPreserveImportSettings(EditorGUILayout.Toggle(exportSettings.PreserveImportSettings));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Keep Instances",
                "If enabled, instances will be preserved as instances in the FBX file. This can cause issues with e.g. Blender if different instances have different materials assigned."),
                GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.SetKeepInstances(EditorGUILayout.Toggle(exportSettings.KeepInstances));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Embed Textures",
                "If enabled, textures are embedded into the resulting FBX file instead of referenced."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.SetEmbedTextures(EditorGUILayout.Toggle(exportSettings.EmbedTextures));
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Interface of export options that you can set when exporting to FBX.
    /// </summary>
    internal interface IExportOptions
    {
        /// <summary>
        /// The export format (binary or ascii).
        /// </summary>
        ExportFormat ExportFormat { get; }

        /// <summary>
        /// Option to export the model only, the animation only, or both the model and the animation.
        /// </summary>
        Include ModelAnimIncludeOption { get; }

        /// <summary>
        /// The type of LOD to export (All, Highest or Lowest).
        /// </summary>
        LODExportType LODExportType { get; }

        /// <summary>
        /// The position to export the object to (Local centered, World absolute, or Reset). Use Reset for converting to a Prefab.
        /// </summary>
        ObjectPosition ObjectPosition { get; }

        /// <summary>
        /// Option to export the animation on GameObjects that have a skinned mesh.
        /// </summary>
        bool AnimateSkinnedMesh { get; }

        /// <summary>
        /// Option to convert the GameObject and material names to Maya compatible names.
        /// </summary>
        bool UseMayaCompatibleNames { get; }

        /// <summary>
        /// Option to change the GameObjects and material names in the scene to keep them
        /// Maya compatible after the export. Only works if UseMayaCompatibleNames is also enabled.
        /// </summary>
        bool AllowSceneModification { get; }

        /// <summary>
        /// Option to export GameObjects that don't have a renderer.
        /// </summary>
        bool ExportUnrendered { get; }

        /// <summary>
        /// Option to preserve the previous import settings after the export when overwriting an existing FBX file.
        /// </summary>
        bool PreserveImportSettings { get; }

        /// <summary>
        /// Option to keep multiple instances of the same mesh as separate instances on export.
        /// </summary>
        bool KeepInstances { get; }

        /// <summary>
        /// Option to embed textures in the exported FBX file.
        /// </summary>
        /// <remarks>
        /// To embed textures, you must set the file ExportFormat to binary.
        /// </remarks>
        bool EmbedTextures { get; }

        /// <summary>
        /// The transform to transfer the animation from. The animation is transferred to AnimationDest.
        /// </summary>
        /// <remarks>
        /// Transform must be an ancestor of AnimationDest, and may be an ancestor of the selected GameObject.
        /// </remarks>
        Transform AnimationSource { get; }

        /// <summary>
        /// The transform to transfer the animation to.
        /// This GameObject receives the transform animation on GameObjects between Source
        /// and Destination as well as the animation on the Source itself.
        /// </summary>
        Transform AnimationDest { get; }
    }

    internal abstract class ExportOptionsSettingsBase<T> : ScriptableObject where T : ExportOptionsSettingsSerializeBase, new()
    {
        [SerializeField]
        private T m_info = new T();
        public T info
        {
            get { return m_info; }
            set { m_info = value; }
        }

        public override bool Equals(object e)
        {
            var expOptions = e as ExportOptionsSettingsBase<T>;
            if (expOptions == null)
            {
                return false;
            }
            return this.info.Equals(expOptions.info);
        }

        public override int GetHashCode()
        {
            return this.info.GetHashCode();
        }
    }

    internal class ExportModelSettings : ExportOptionsSettingsBase<ExportModelSettingsSerialize>
    {}

    /// <summary>
    /// Base class for the export model settings and convert to prefab settings.
    /// </summary>
    [System.Serializable]
    internal abstract class ExportOptionsSettingsSerializeBase : IExportOptions
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

        /// <inheritdoc/>
        public ExportFormat ExportFormat { get { return exportFormat; } }

        /// <summary>
        /// Specifies the export format to binary or ascii.
        /// </summary>
        /// <param name="format">Binary or ASCII</param>
        public void SetExportFormat(ExportFormat format) { this.exportFormat = format; }

        /// <inheritdoc/>
        public bool AnimateSkinnedMesh { get { return animatedSkinnedMesh; } }

        /// <summary>
        /// Specifies whether to export animation on GameObjects containing a skinned mesh.
        /// </summary>
        /// <param name="animatedSkinnedMesh">True to export animation on skinned meshes, false otherwise.</param>
        public void SetAnimatedSkinnedMesh(bool animatedSkinnedMesh) { this.animatedSkinnedMesh = animatedSkinnedMesh; }

        /// <inheritdoc/>
        public bool UseMayaCompatibleNames { get { return mayaCompatibleNaming; } }

        /// <summary>
        /// Specifies whether to rename the exported GameObjects to Maya compatible names.
        /// </summary>
        /// <param name="useMayaCompNames">True to have export Maya compatible names, false otherwise.</param>
        public void SetUseMayaCompatibleNames(bool useMayaCompNames) { this.mayaCompatibleNaming = useMayaCompNames; }

        /// <inheritdoc/>
        public Transform AnimationSource { get { return animSource; } }

        /// <summary>
        /// Specifies the transform to transfer the animation from.
        /// </summary>
        /// <param name="source">The transform to transfer the animation from.</param>
        public void SetAnimationSource(Transform source) { this.animSource = source; }

        /// <inheritdoc/>
        public Transform AnimationDest { get { return animDest; } }

        /// <summary>
        /// Specifies the transform to transfer the source animation to.
        /// </summary>
        /// <param name="dest">The transform to transfer the animation to.</param>
        public void SetAnimationDest(Transform dest) { this.animDest = dest; }

        /// <inheritdoc/>
        public abstract Include ModelAnimIncludeOption { get; }

        /// <inheritdoc/>
        public abstract LODExportType LODExportType { get; }

        /// <inheritdoc/>
        public abstract ObjectPosition ObjectPosition { get; }

        /// <inheritdoc/>
        public abstract bool ExportUnrendered { get; }

        /// <inheritdoc/>
        public virtual bool PreserveImportSettings { get { return false; } }

        /// <inheritdoc/>
        public abstract bool AllowSceneModification { get; }

        /// <inheritdoc/>
        public virtual bool KeepInstances { get { return true; } }

        /// <inheritdoc/>
        public virtual bool EmbedTextures { get { return false; } }

        /// <summary>
        /// Checks if two instances of the export settings are equal.
        /// </summary>
        /// <param name="e">The other export setting object to check.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public override bool Equals(object e)
        {
            var expOptions = e as ExportOptionsSettingsSerializeBase;
            if (expOptions == null)
            {
                return false;
            }
            return animatedSkinnedMesh == expOptions.animatedSkinnedMesh &&
                mayaCompatibleNaming == expOptions.mayaCompatibleNaming &&
                exportFormat == expOptions.exportFormat;
        }

        /// <summary>
        /// Gets the hash code for this instance of the export model settings.
        /// </summary>
        /// <returns>Unique hash code for the export model settings.</returns>
        public override int GetHashCode()
        {
            return (animatedSkinnedMesh ? 1 : 0) | ((mayaCompatibleNaming ? 1 : 0) << 1) | ((int)exportFormat << 2);
        }
    }

    /// <summary>
    /// Class specifying the settings for exporting to FBX.
    /// </summary>
    [System.Serializable]
    internal class ExportModelSettingsSerialize : ExportOptionsSettingsSerializeBase
    {
        [SerializeField]
        private Include include = Include.ModelAndAnim;
        [SerializeField]
        private LODExportType lodLevel = LODExportType.All;
        [SerializeField]
        private ObjectPosition objectPosition = ObjectPosition.LocalCentered;
        [SerializeField]
        private bool exportUnrendered = true;
        [SerializeField]
        private bool preserveImportSettings = false;
        [SerializeField]
        private bool keepInstances = true;
        [SerializeField]
        private bool embedTextures = false;

        /// <inheritdoc/>
        public override Include ModelAnimIncludeOption { get { return include; } }

        /// <summary>
        /// Specifies to export the model only, the animation only, or both the model and the animation.
        /// </summary>
        /// <param name="include">Model, animation, or model and animation</param>
        public void SetModelAnimIncludeOption(Include include) { this.include = include; }

        /// <inheritdoc/>
        public override LODExportType LODExportType { get { return lodLevel; } }

        /// <summary>
        /// Specifies the type of LOD to export (All, Highest or Lowest).
        /// </summary>
        /// <param name="lodLevel">All, Highest, or Lowest</param>
        public void SetLODExportType(LODExportType lodLevel) { this.lodLevel = lodLevel; }

        /// <inheritdoc/>
        public override ObjectPosition ObjectPosition { get { return objectPosition; } }

        /// <summary>
        /// Specifies the position to export the object to (Local centered, World absolute, or Reset). Use Reset for converting to a Prefab).
        /// </summary>
        /// <param name="objectPosition">Local centered, World absolute, or Reset</param>
        public void SetObjectPosition(ObjectPosition objectPosition) { this.objectPosition = objectPosition; }

        /// <inheritdoc/>
        public override bool ExportUnrendered { get { return exportUnrendered; } }

        /// <summary>
        /// Specifies whether to export GameObjects that don't have a renderer.
        /// </summary>
        /// <param name="exportUnrendered">True to export unrendered, false otherwise.</param>
        public void SetExportUnrendered(bool exportUnrendered) { this.exportUnrendered = exportUnrendered; }

        /// <inheritdoc/>
        public override bool PreserveImportSettings { get { return preserveImportSettings; } }

        /// <summary>
        /// Specifies whether to preserve the previous import settings after the export when overwriting
        /// an existing FBX file.
        /// </summary>
        /// <param name="preserveImportSettings">True to preserve the previous import settings, false otherwise.</param>
        public void SetPreserveImportSettings(bool preserveImportSettings) { this.preserveImportSettings = preserveImportSettings; }

        /// <inheritdoc/>
        public override bool AllowSceneModification { get { return false; } }

        /// <inheritdoc/>
        public override bool KeepInstances { get { return keepInstances; } }

        /// <summary>
        /// Specifies whether to keep multiple instances of the same mesh as separate instances on export.
        /// </summary>
        /// <param name="keepInstances">True to export as separate instances, false otherwise.</param>
        public void SetKeepInstances(bool keepInstances) { this.keepInstances = keepInstances; }

        /// <inheritdoc/>
        public override bool EmbedTextures { get { return embedTextures; } }

        /// <summary>
        /// Specifies whether to embed textures in the exported FBX file.
        /// </summary>
        /// <param name="embedTextures">True to embed textures, false otherwise.</param>
        public void SetEmbedTextures(bool embedTextures) { this.embedTextures = embedTextures; }

        /// <inheritdoc/>
        public override bool Equals(object e)
        {
            var expOptions = e as ExportModelSettingsSerialize;
            if (expOptions == null)
            {
                return false;
            }
            return base.Equals(e) &&
                include == expOptions.include &&
                lodLevel == expOptions.lodLevel &&
                objectPosition == expOptions.objectPosition &&
                exportUnrendered == expOptions.exportUnrendered &&
                preserveImportSettings == expOptions.preserveImportSettings;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var bitmask =  base.GetHashCode();
            bitmask = (bitmask << 2) ^ (int)include;
            bitmask = (bitmask << 2) ^ (int)lodLevel;
            bitmask = (bitmask << 2) ^ (int)objectPosition;
            bitmask = (bitmask << 1) | (exportUnrendered ? 1 : 0);
            bitmask = (bitmask << 1) | (preserveImportSettings ? 1 : 0);
            return bitmask;
        }
    }

    /// <summary>
    /// Class specifying the settings for exporting to FBX.
    /// </summary>
    [System.Serializable]
    public class ExportModelOptions : IExportOptions
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

        [SerializeField]
        private Include include = Include.ModelAndAnim;
        [SerializeField]
        private LODExportType lodLevel = LODExportType.All;
        [SerializeField]
        private ObjectPosition objectPosition = ObjectPosition.LocalCentered;
        [SerializeField]
        private bool exportUnrendered = true;
        [SerializeField]
        private bool preserveImportSettings = false;
        [SerializeField]
        private bool keepInstances = true;
        [SerializeField]
        private bool embedTextures = false;

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
        public Include ModelAnimIncludeOption
        {
            get { return include; }
            set { include = value; }
        }

        /// <summary>
        /// The type of LOD to export (All, Highest or Lowest).
        /// </summary>
        public LODExportType LODExportType
        {
            get { return lodLevel; }
            set { lodLevel = value; }
        }

        /// <summary>
        /// The position to export the object to (Local centered, World absolute, or Reset). Use Reset for converting to a Prefab.
        /// </summary>
        public ObjectPosition ObjectPosition
        {
            get { return objectPosition; }
            set { objectPosition = value; }
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
            get { return false; }
        }

        /// <summary>
        /// Option to export GameObjects that don't have a renderer.
        /// </summary>
        public bool ExportUnrendered
        {
            get { return exportUnrendered; }
            set { exportUnrendered = value; }
        }

        /// <summary>
        /// Option to preserve the previous import settings after the export when overwriting an existing FBX file.
        /// </summary>
        public bool PreserveImportSettings
        {
            get { return preserveImportSettings; }
            set { preserveImportSettings = value; }
        }

        /// <summary>
        /// Option to keep multiple instances of the same mesh as separate instances on export.
        /// </summary>
        public bool KeepInstances
        {
            get { return keepInstances; }
            set { keepInstances = value; }
        }

        /// <summary>
        /// Option to embed textures in the exported FBX file.
        /// </summary>
        /// <remarks>
        /// To embed textures, you must set the file ExportFormat to binary.
        /// </remarks>
        public bool EmbedTextures
        {
            get { return embedTextures; }
            set { embedTextures = value; }
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

        internal ExportModelSettingsSerialize ConvertToModelSettingsSerialize()
        {
            var exportSettings = new ExportModelSettingsSerialize();
            exportSettings.SetAnimatedSkinnedMesh(animatedSkinnedMesh);
            exportSettings.SetAnimationDest(animDest);
            exportSettings.SetAnimationSource(animSource);
            exportSettings.SetEmbedTextures(embedTextures);
            exportSettings.SetExportFormat(exportFormat);
            exportSettings.SetExportUnrendered(exportUnrendered);
            exportSettings.SetKeepInstances(keepInstances);
            exportSettings.SetLODExportType(lodLevel);
            exportSettings.SetModelAnimIncludeOption(include);
            exportSettings.SetObjectPosition(objectPosition);
            exportSettings.SetPreserveImportSettings(preserveImportSettings);
            exportSettings.SetUseMayaCompatibleNames(mayaCompatibleNaming);

            return exportSettings;
        }
    }
}
