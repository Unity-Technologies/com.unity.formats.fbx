using UnityEngine;

namespace UnityEditor.Formats.Fbx.Exporter
{
    [CustomEditor (typeof(ExportModelSettings))]
    internal class ExportModelSettingsEditor : UnityEditor.Editor
    {
        private const float DefaultLabelWidth = 175;
        private const float DefaultFieldOffset = 18;

        public float LabelWidth { get; set; } = DefaultLabelWidth;
        public float FieldOffset { get; set; } = DefaultFieldOffset;

        private string[] exportFormatOptions = new string[]{ "ASCII", "Binary" };
        private string[] includeOptions = new string[]{"Model(s) Only", "Animation Only", "Model(s) + Animation"};
        private string[] lodOptions = new string[]{"All Levels", "Highest", "Lowest"};

        public const string singleHierarchyOption = "Local Pivot";
        public const string multiHerarchyOption = "Local Centered";
        private string hierarchyDepOption = singleHierarchyOption;
        private string[] objPositionOptions { get { return new string[]{hierarchyDepOption, "World Absolute"}; }}

        private bool disableIncludeDropdown = false;

        private bool m_exportingOutsideProject = false;
        public void SetExportingOutsideProject(bool val)
        {
            m_exportingOutsideProject = val;
        }

        public void SetIsSingleHierarchy(bool singleHierarchy){
            if (singleHierarchy) {
                hierarchyDepOption = singleHierarchyOption;
                return;
            }
            hierarchyDepOption = multiHerarchyOption;
        }

        public void DisableIncludeDropdown(bool disable){
            disableIncludeDropdown = disable;
        }

        public override void OnInspectorGUI ()
        {
            var exportSettings = ((ExportModelSettings)target).info;

            EditorGUIUtility.labelWidth = LabelWidth;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Format", "Export the FBX file in the standard binary format." +
                " Select ASCII to export the FBX file in ASCII format."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.SetExportFormat((ExportFormat)EditorGUILayout.Popup((int)exportSettings.ExportFormat, exportFormatOptions) );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Include", "Select whether to export models, animation or both."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUI.BeginDisabledGroup(disableIncludeDropdown);
            exportSettings.SetModelAnimIncludeOption((Include)EditorGUILayout.Popup((int)exportSettings.ModelAnimIncludeOption, includeOptions));
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == Include.Anim);
            exportSettings.SetLODExportType((LODExportType)EditorGUILayout.Popup((int)exportSettings.LODExportType, lodOptions));
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == Include.Anim);
            exportSettings.SetObjectPosition((ObjectPosition)EditorGUILayout.Popup((int)exportSettings.ObjectPosition, objPositionOptions));
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Animated Skinned Mesh",
                "If checked, animation on objects with skinned meshes will be exported"), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if model
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == Include.Model);
            exportSettings.SetAnimatedSkinnedMesh(EditorGUILayout.Toggle(exportSettings.AnimateSkinnedMesh));
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal ();

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

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Unrendered",
                "If checked, meshes will be exported even if they don't have a Renderer component."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == Include.Anim);
            exportSettings.SetExportUnredererd(EditorGUILayout.Toggle(exportSettings.ExportUnrendered));
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal ();
            
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
    /// Interface of export options that can be set when exporting to FBX.
    /// </summary>
    public interface IExportOptions {
        ExportFormat ExportFormat { get; }
        Include ModelAnimIncludeOption { get; }
        LODExportType LODExportType { get; }
        ObjectPosition ObjectPosition { get; }
        bool AnimateSkinnedMesh { get; }
        bool UseMayaCompatibleNames { get; }
        bool AllowSceneModification { get; }
        bool ExportUnrendered { get; }
        bool PreserveImportSettings { get; }
        bool KeepInstances { get; }
        bool EmbedTextures { get; }
        Transform AnimationSource { get; }
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
            if(expOptions == null)
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
    public abstract class ExportOptionsSettingsSerializeBase : IExportOptions
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

        public ExportFormat ExportFormat { get { return exportFormat; } }
        public void SetExportFormat(ExportFormat format){ this.exportFormat = format; }
        public bool AnimateSkinnedMesh { get { return animatedSkinnedMesh; } }
        public void SetAnimatedSkinnedMesh(bool animatedSkinnedMesh){ this.animatedSkinnedMesh = animatedSkinnedMesh; }
        public bool UseMayaCompatibleNames { get { return mayaCompatibleNaming; } }
        public void SetUseMayaCompatibleNames(bool useMayaCompNames){ this.mayaCompatibleNaming = useMayaCompNames; }
        public Transform AnimationSource { get { return animSource; } }
        public void SetAnimationSource(Transform source) { this.animSource = source; }
        public Transform AnimationDest { get { return animDest; } }
        public void SetAnimationDest(Transform dest) { this.animDest = dest; }

        /// <summary>
        /// Get option to export model only, animation only, or both model and animation.
        /// </summary>
        public abstract Include ModelAnimIncludeOption { get; }

        /// <summary>
        /// Get the type of LOD to export (options are: All, Highest or Lowest).
        /// </summary>
        public abstract LODExportType LODExportType { get; }

        /// <summary>
        /// Get the position to export the object to (options are: Local centered, World absolute, and Reset (used for converting to prefab)).
        /// </summary>
        public abstract ObjectPosition ObjectPosition { get; }

        /// <summary>
        /// Should objects that do not have a renderer be exported?
        /// </summary>
        public abstract bool ExportUnrendered { get; }

        /// <summary>
        /// If an FBX file is being overwritten, should the previous import settings be preserved after export.
        /// </summary>
        public virtual bool PreserveImportSettings { get { return false; } }
        public abstract bool AllowSceneModification { get; }

        /// <summary>
        /// If multiple instances of the same mesh are being exported, should they be kept as instances on export?
        /// </summary>
        public virtual bool KeepInstances { get { return true; } }

        /// <summary>
        /// Should textures be embedded in the FBX file.
        /// </summary>
        /// <remarks>
        /// Note: For textures to be embedded the file must also be exported as binary.
        /// </remarks>
        public virtual bool EmbedTextures { get { return false; } }

        /// <summary>
        /// Check if two instances of the export settings are equal.
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
        /// Get the hash code for this instance of the export model settings.
        /// </summary>
        /// <returns>Unique hash code for these settings.</returns>
        public override int GetHashCode()
        {
            return (animatedSkinnedMesh ? 1 : 0) | ((mayaCompatibleNaming ? 1 : 0) << 1) | ((int)exportFormat << 2);
        }
    }

    /// <summary>
    /// Class specifying the settings for exporting to FBX.
    /// </summary>
    [System.Serializable]
    public class ExportModelSettingsSerialize : ExportOptionsSettingsSerializeBase
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
        /// Set the option to export model only, animation only, or both model and animation.
        /// </summary>
        /// <param name="include">model, animation, or model and animation</param>
        public void SetModelAnimIncludeOption(Include include) { this.include = include; }

        /// <inheritdoc/>
        public override LODExportType LODExportType { get { return lodLevel; } }

        /// <summary>
        /// Set the type of LOD to export (options are: All, Highest or Lowest).
        /// </summary>
        /// <param name="lodLevel">All, Highest, or Lowest</param>
        public void SetLODExportType(LODExportType lodLevel){ this.lodLevel = lodLevel; }

        /// <inheritdoc/>
        public override ObjectPosition ObjectPosition { get { return objectPosition; } }

        /// <summary>
        /// Set the position to export the object to (options are: Local centered, World absolute, and Reset (used for converting to prefab)).
        /// </summary>
        /// <param name="objectPosition">Local centered, World absolute, or Reset</param>
        public void SetObjectPosition(ObjectPosition objectPosition){ this.objectPosition = objectPosition; }

        /// <inheritdoc/>
        public override bool ExportUnrendered { get { return exportUnrendered; } }

        /// <summary>
        /// Set whether objects that do not have a renderer are exported.
        /// </summary>
        /// <param name="exportUnrendered">True to export unrendered, false otherwise.</param>
        public void SetExportUnredererd(bool exportUnrendered){ this.exportUnrendered = exportUnrendered; }

        /// <inheritdoc/>
        public override bool PreserveImportSettings { get { return preserveImportSettings; } }

        /// <summary>
        /// Set whether the previous export settings will be preserved after export when overwriting
        /// an existing FBX file.
        /// </summary>
        /// <param name="preserveImportSettings">True if previous import settings should be preserved, false otherwise.</param>
        public void SetPreserveImportSettings(bool preserveImportSettings){ this.preserveImportSettings = preserveImportSettings; }

        /// <inheritdoc/>
        public override bool AllowSceneModification { get { return false; } }

        /// <inheritdoc/>
        public override bool KeepInstances { get { return keepInstances; } }

        /// <summary>
        /// Set whether multiple instances of the same mesh are kept as instances on export.
        /// </summary>
        /// <param name="keepInstances">True if instances should be exported as instances, false otherwise.</param>
        public void SetKeepInstances(bool keepInstances){ this.keepInstances = keepInstances; }

        /// <inheritdoc/>
        public override bool EmbedTextures { get { return embedTextures; } }

        /// <summary>
        /// Set whether textures should be embedded on export.
        /// </summary>
        /// <param name="embedTextures">True if textures should be embedded, false otherwise.</param>
        public void SetEmbedTextures(bool embedTextures){ this.embedTextures = embedTextures; }

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
}