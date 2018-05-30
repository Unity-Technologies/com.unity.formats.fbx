using UnityEditor;
using UnityEngine;

namespace UnityEditor.Formats.Fbx.Exporter
{
    [CustomEditor (typeof(ExportModelSettings))]
    public class ExportModelSettingsEditor : UnityEditor.Editor
    {
        private const float LabelWidth = 175;
        private const float FieldOffset = 18;

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

            exportSettings.SetUseMayaCompatibleNames(EditorGUILayout.Toggle (
                new GUIContent ("Compatible Naming",
                    "In Maya some symbols such as spaces and accents get replaced when importing an FBX " +
                    "(e.g. \"foo bar\" becomes \"fooFBXASC032bar\"). " +
                    "On export, convert the names of GameObjects so they are Maya compatible." +
                    (exportSettings.UseMayaCompatibleNames ? "" :
                        "\n\nWARNING: Disabling this feature may result in lost material connections," +
                        " and unexpected character replacements in Maya.")
                ),
                exportSettings.UseMayaCompatibleNames));

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Unrendered",
                "If checked, meshes will be exported even if they don't have a Renderer component."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.ModelAnimIncludeOption == ExportSettings.Include.Anim);
            exportSettings.SetExportUnredererd(EditorGUILayout.Toggle(exportSettings.ExportUnrendered));
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal ();
        }
    }

    public interface IExportOptions {
        ExportSettings.ExportFormat ExportFormat { get; }
        ExportSettings.Include ModelAnimIncludeOption { get; }
        ExportSettings.LODExportType LODExportType { get; }
        ExportSettings.ObjectPosition ObjectPosition { get; }
        bool AnimateSkinnedMesh { get; }
        bool UseMayaCompatibleNames { get; }
        bool AllowSceneModification { get; }
        bool ExportUnrendered { get; }
        Transform AnimationSource { get; }
        Transform AnimationDest { get; }
    }

    public abstract class ExportOptionsSettingsBase<T> : ScriptableObject where T : ExportOptionsSettingsSerializeBase, new()
    {
        private T m_info = new T();
        public T info {
            get { return m_info; }
            set { m_info = value; }
        }
    }

    public class ExportModelSettings : ExportOptionsSettingsBase<ExportModelSettingsSerialize>
    {}

    [System.Serializable]
    public abstract class ExportOptionsSettingsSerializeBase : IExportOptions
    {
        private ExportSettings.ExportFormat exportFormat = ExportSettings.ExportFormat.ASCII;
        private bool animatedSkinnedMesh = false;
        private bool mayaCompatibleNaming = true;

        [System.NonSerialized]
        private Transform animSource;
        [System.NonSerialized]
        private Transform animDest;

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
        public abstract bool AllowSceneModification { get; }
    }

    [System.Serializable]
    public class ExportModelSettingsSerialize : ExportOptionsSettingsSerializeBase
    {
        private ExportSettings.Include include = ExportSettings.Include.ModelAndAnim;
        private ExportSettings.LODExportType lodLevel = ExportSettings.LODExportType.All;
        private ExportSettings.ObjectPosition objectPosition = ExportSettings.ObjectPosition.LocalCentered;
        private bool exportUnrendered = true;

        public override ExportSettings.Include ModelAnimIncludeOption { get { return include; } }
        public void SetModelAnimIncludeOption(ExportSettings.Include include) { this.include = include; }
        public override ExportSettings.LODExportType LODExportType { get { return lodLevel; } }
        public void SetLODExportType(ExportSettings.LODExportType lodLevel){ this.lodLevel = lodLevel; }
        public override ExportSettings.ObjectPosition ObjectPosition { get { return objectPosition; } }
        public void SetObjectPosition(ExportSettings.ObjectPosition objPos){ this.objectPosition = objPos; }
        public override bool ExportUnrendered { get { return exportUnrendered; } }
        public void SetExportUnredererd(bool exportUnrendered){ this.exportUnrendered = exportUnrendered; }
        public override bool AllowSceneModification { get { return false; } }
    }
}