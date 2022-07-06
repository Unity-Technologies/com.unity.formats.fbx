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
        public abstract Include ModelAnimIncludeOption { get; }
        public abstract LODExportType LODExportType { get; }
        public abstract ObjectPosition ObjectPosition { get; }
        public abstract bool ExportUnrendered { get; }
        public virtual bool PreserveImportSettings { get { return false; } }
        public abstract bool AllowSceneModification { get; }
        public virtual bool KeepInstances { get { return true; } }
        public virtual bool EmbedTextures { get { return false; } }

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

        public override int GetHashCode()
        {
            return (animatedSkinnedMesh ? 1 : 0) | ((mayaCompatibleNaming ? 1 : 0) << 1) | ((int)exportFormat << 2);
        }
    }

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
        
        public override Include ModelAnimIncludeOption { get { return include; } }
        public void SetModelAnimIncludeOption(Include include) { this.include = include; }
        public override LODExportType LODExportType { get { return lodLevel; } }
        public void SetLODExportType(LODExportType lodLevel){ this.lodLevel = lodLevel; }
        public override ObjectPosition ObjectPosition { get { return objectPosition; } }
        public void SetObjectPosition(ObjectPosition objPos){ this.objectPosition = objPos; }
        public override bool ExportUnrendered { get { return exportUnrendered; } }
        public void SetExportUnredererd(bool exportUnrendered){ this.exportUnrendered = exportUnrendered; }
        public override bool PreserveImportSettings { get { return preserveImportSettings; } }
        public void SetPreserveImportSettings(bool preserveImportSettings){ this.preserveImportSettings = preserveImportSettings; }
        public override bool AllowSceneModification { get { return false; } }
        public override bool KeepInstances { get { return keepInstances; } }
        public void SetKeepInstances(bool keepInstances){ this.keepInstances = keepInstances; }
        public override bool EmbedTextures { get { return embedTextures; } }
        public void SetEmbedTextures(bool embedTextures){ this.embedTextures = embedTextures; }

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