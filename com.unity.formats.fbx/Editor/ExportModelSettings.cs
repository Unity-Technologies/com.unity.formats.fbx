using UnityEditor;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
        private string hierarchyDepOption = "";
        private string[] objPositionOptions { get { return new string[]{hierarchyDepOption, "World Absolute"}; }}

        private bool disableIncludeDropdown = false;

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
            exportSettings.SetExportFormat((ExportSettings.ExportFormat)EditorGUILayout.Popup((int)exportSettings.ExportFormat, exportFormatOptions) );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Include", "Select whether to export models, animation or both."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUI.BeginDisabledGroup(disableIncludeDropdown);
            exportSettings.SetModelAnimIncludeOption((ExportSettings.Include)EditorGUILayout.Popup((int)exportSettings.ModelAnimIncludeOption, includeOptions));
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == ExportSettings.Include.Anim);
            exportSettings.SetLODExportType((ExportSettings.LODExportType)EditorGUILayout.Popup((int)exportSettings.LODExportType, lodOptions));
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == ExportSettings.Include.Anim);
            exportSettings.SetObjectPosition((ExportSettings.ObjectPosition)EditorGUILayout.Popup((int)exportSettings.ObjectPosition, objPositionOptions));
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Animated Skinned Mesh",
                "If checked, animation on objects with skinned meshes will be exported"), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if model
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == ExportSettings.Include.Model);
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
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == ExportSettings.Include.Anim);
            exportSettings.SetExportUnredererd(EditorGUILayout.Toggle(exportSettings.ExportUnrendered));
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal ();
            
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Preserve Import Settings",
                "If checked, the import settings from the overwritten FBX will be carried over to the new version."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if exporting outside assets folder
            EditorGUI.BeginDisabledGroup(ExportSettings.instance.ExportOutsideProject);
            exportSettings.SetPreserveImportSettings(EditorGUILayout.Toggle(exportSettings.PreserveImportSettings));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
        }
    }

    internal interface IExportOptions {
        ExportSettings.ExportFormat ExportFormat { get; }
        ExportSettings.Include ModelAnimIncludeOption { get; }
        ExportSettings.LODExportType LODExportType { get; }
        ExportSettings.ObjectPosition ObjectPosition { get; }
        bool AnimateSkinnedMesh { get; }
        bool UseMayaCompatibleNames { get; }
        bool AllowSceneModification { get; }
        bool ExportUnrendered { get; }
        bool PreserveImportSettings { get; }
        Transform AnimationSource { get; }
        Transform AnimationDest { get; }
    }

    internal abstract class ExportOptionsSettingsBase<T> : ScriptableObject where T : ExportOptionsSettingsSerializeBase, new()
    {
        private T m_info = new T();
        public T info {
            get { return m_info; }
            set { m_info = value; }
        }
    }

    internal class ExportModelSettings : ExportOptionsSettingsBase<ExportModelSettingsSerialize>
    {}

    [System.Serializable]
    internal abstract class ExportOptionsSettingsSerializeBase : IExportOptions
    {
        [SerializeField]
        private ExportSettings.ExportFormat exportFormat = ExportSettings.ExportFormat.ASCII;
        [SerializeField]
        private bool animatedSkinnedMesh = false;
        [SerializeField]
        private bool mayaCompatibleNaming = true;

        [System.NonSerialized]
        private Transform animSource;
        [System.NonSerialized]
        private Transform animDest;

        protected const string k_sessionStoragePrefix = "FbxExporterOptions_{0}";

        public ExportSettings.ExportFormat ExportFormat { get { return exportFormat; } }
        public void SetExportFormat(ExportSettings.ExportFormat format){ this.exportFormat = format; }
        public bool AnimateSkinnedMesh { get { return animatedSkinnedMesh; } }
        public void SetAnimatedSkinnedMesh(bool animatedSkinnedMesh){ this.animatedSkinnedMesh = animatedSkinnedMesh; }
        public bool UseMayaCompatibleNames { get { return mayaCompatibleNaming; } }
        public void SetUseMayaCompatibleNames(bool useMayaCompNames){ this.mayaCompatibleNaming = useMayaCompNames; }
        public Transform AnimationSource { get { return animSource; } }
        public void SetAnimationSource(Transform source) { this.animSource = source; }
        public Transform AnimationDest { get { return animDest; } }
        public void SetAnimationDest(Transform dest) { this.animDest = dest; }
        public abstract ExportSettings.Include ModelAnimIncludeOption { get; }
        public abstract ExportSettings.LODExportType LODExportType { get; }
        public abstract ExportSettings.ObjectPosition ObjectPosition { get; }
        public abstract bool ExportUnrendered { get; }
        public virtual bool PreserveImportSettings { get;  }
        public abstract bool AllowSceneModification { get; }

        public virtual void StoreInSession()
        {
            SessionState.SetInt(string.Format(k_sessionStoragePrefix, nameof(exportFormat)), (int)exportFormat);
            SessionState.SetBool(string.Format(k_sessionStoragePrefix, nameof(animatedSkinnedMesh)), animatedSkinnedMesh);
            SessionState.SetBool(string.Format(k_sessionStoragePrefix, nameof(mayaCompatibleNaming)), mayaCompatibleNaming);
        }

        public virtual void RestoreFromSession(ExportOptionsSettingsSerializeBase defaults)
        {
            exportFormat = (ExportSettings.ExportFormat)SessionState.GetInt(string.Format(k_sessionStoragePrefix, nameof(exportFormat)), (int)defaults.ExportFormat);
            animatedSkinnedMesh = SessionState.GetBool(string.Format(k_sessionStoragePrefix, nameof(animatedSkinnedMesh)), defaults.AnimateSkinnedMesh);
            mayaCompatibleNaming = SessionState.GetBool(string.Format(k_sessionStoragePrefix, nameof(mayaCompatibleNaming)), defaults.UseMayaCompatibleNames);
        }
    }

    [System.Serializable]
    internal class ExportModelSettingsSerialize : ExportOptionsSettingsSerializeBase
    {
        [SerializeField]
        private ExportSettings.Include include = ExportSettings.Include.ModelAndAnim;
        [SerializeField]
        private ExportSettings.LODExportType lodLevel = ExportSettings.LODExportType.All;
        [SerializeField]
        private ExportSettings.ObjectPosition objectPosition = ExportSettings.ObjectPosition.LocalCentered;
        [SerializeField]
        private bool exportUnrendered = true;
        private bool preserveImportSettings = false;

        public override ExportSettings.Include ModelAnimIncludeOption { get { return include; } }
        public void SetModelAnimIncludeOption(ExportSettings.Include include) { this.include = include; }
        public override ExportSettings.LODExportType LODExportType { get { return lodLevel; } }
        public void SetLODExportType(ExportSettings.LODExportType lodLevel){ this.lodLevel = lodLevel; }
        public override ExportSettings.ObjectPosition ObjectPosition { get { return objectPosition; } }
        public void SetObjectPosition(ExportSettings.ObjectPosition objPos){ this.objectPosition = objPos; }
        public override bool ExportUnrendered { get { return exportUnrendered; } }
        public void SetExportUnredererd(bool exportUnrendered){ this.exportUnrendered = exportUnrendered; }
        public override bool PreserveImportSettings { get { return preserveImportSettings; } }
        public void SetPreserveImportSettings(bool preserveImportSettings){ this.preserveImportSettings = preserveImportSettings && !ExportSettings.instance.ExportOutsideProject; }
        public override bool AllowSceneModification { get { return false; } }

        public override void StoreInSession()
        {
            base.StoreInSession();
            SessionState.SetInt(string.Format(k_sessionStoragePrefix, nameof(include)), (int)include);
            SessionState.SetInt(string.Format(k_sessionStoragePrefix, nameof(lodLevel)), (int)lodLevel);
            SessionState.SetInt(string.Format(k_sessionStoragePrefix, nameof(objectPosition)), (int)objectPosition);
            SessionState.SetBool(string.Format(k_sessionStoragePrefix, nameof(exportUnrendered)), exportUnrendered);
            SessionState.SetBool(string.Format(k_sessionStoragePrefix, nameof(preserveImportSettings)), preserveImportSettings);
        }

        public override void RestoreFromSession(ExportOptionsSettingsSerializeBase defaults)
        {
            base.RestoreFromSession(defaults);
            include = (ExportSettings.Include)SessionState.GetInt(string.Format(k_sessionStoragePrefix, nameof(include)), (int)defaults.ModelAnimIncludeOption);
            lodLevel = (ExportSettings.LODExportType)SessionState.GetInt(string.Format(k_sessionStoragePrefix, nameof(lodLevel)), (int)defaults.LODExportType);
            objectPosition = (ExportSettings.ObjectPosition)SessionState.GetInt(string.Format(k_sessionStoragePrefix, nameof(objectPosition)), (int)defaults.ObjectPosition);
            exportUnrendered = SessionState.GetBool(string.Format(k_sessionStoragePrefix, nameof(exportUnrendered)), defaults.ExportUnrendered);
            preserveImportSettings = SessionState.GetBool(string.Format(k_sessionStoragePrefix, nameof(preserveImportSettings)), defaults.PreserveImportSettings);
        }
    }
}