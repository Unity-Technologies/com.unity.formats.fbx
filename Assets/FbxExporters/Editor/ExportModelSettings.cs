using UnityEditor;
using UnityEngine;

namespace FbxExporters.EditorTools
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
            exportSettings.exportFormat = (ExportSettings.ExportFormat)EditorGUILayout.Popup((int)exportSettings.exportFormat, exportFormatOptions);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Include", "Select whether to export models, animation or both."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUI.BeginDisabledGroup(disableIncludeDropdown);
            exportSettings.include = (ExportSettings.Include)EditorGUILayout.Popup((int)exportSettings.include, includeOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.include == ExportSettings.Include.Anim);
            exportSettings.lodLevel = (ExportSettings.LODExportType)EditorGUILayout.Popup((int)exportSettings.lodLevel, lodOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.include == ExportSettings.Include.Anim);
            exportSettings.objectPosition = (ExportSettings.ObjectPosition)EditorGUILayout.Popup((int)exportSettings.objectPosition, objPositionOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Animated Skinned Mesh",
                "If checked, animation on objects with skinned meshes will be exported"), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if model
            EditorGUI.BeginDisabledGroup(exportSettings.include == ExportSettings.Include.Model);
            exportSettings.animatedSkinnedMesh = EditorGUILayout.Toggle(exportSettings.animatedSkinnedMesh);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal ();

            exportSettings.mayaCompatibleNaming = EditorGUILayout.Toggle (
                new GUIContent ("Compatible Naming",
                    "In Maya some symbols such as spaces and accents get replaced when importing an FBX " +
                    "(e.g. \"foo bar\" becomes \"fooFBXASC032bar\"). " +
                    "On export, convert the names of GameObjects so they are Maya compatible." +
                    (exportSettings.mayaCompatibleNaming ? "" :
                        "\n\nWARNING: Disabling this feature may result in lost material connections," +
                        " and unexpected character replacements in Maya.")
                ),
                exportSettings.mayaCompatibleNaming);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Unrendered",
                "If checked, meshes will be exported even if they don't have a Renderer component."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.include == ExportSettings.Include.Anim);
            exportSettings.exportUnrendered = EditorGUILayout.Toggle(exportSettings.exportUnrendered);
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
        bool ExportUnrendered { get; }
        Transform AnimationSource { get; }
        Transform AnimationDest { get; }
    }

    public abstract class ExportOptionsSettingsBase<T> : ScriptableObject where T : ExportOptionsSettingsSerializeBase, new()
    {
        public T info = new T();
    }

    public class ExportModelSettings : ExportOptionsSettingsBase<ExportModelSettingsSerialize>
    {}

    [System.Serializable]
    public abstract class ExportOptionsSettingsSerializeBase : IExportOptions
    {
        public ExportSettings.ExportFormat exportFormat = ExportSettings.ExportFormat.ASCII;
        public string rootMotionTransfer = "";
        public bool animatedSkinnedMesh = false;
        public bool mayaCompatibleNaming = true;

        [System.NonSerialized]
        protected Transform animSource;
        [System.NonSerialized]
        protected Transform animDest;

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
    }

    [System.Serializable]
    public class ExportModelSettingsSerialize : ExportOptionsSettingsSerializeBase
    {
        public ExportSettings.Include include = ExportSettings.Include.ModelAndAnim;
        public ExportSettings.LODExportType lodLevel = ExportSettings.LODExportType.All;
        public ExportSettings.ObjectPosition objectPosition = ExportSettings.ObjectPosition.LocalCentered;
        public bool exportUnrendered = true;

        public override ExportSettings.Include ModelAnimIncludeOption { get { return include; } }
        public void SetModelAnimIncludeOption(ExportSettings.Include include) { this.include = include; }
        public override ExportSettings.LODExportType LODExportType { get { return lodLevel; } }
        public void SetLODExportType(ExportSettings.LODExportType lodLevel){ this.lodLevel = lodLevel; }
        public override ExportSettings.ObjectPosition ObjectPosition { get { return objectPosition; } }
        public void SetObjectPosition(ExportSettings.ObjectPosition objPos){ this.objectPosition = objPos; }
        public override bool ExportUnrendered { get { return exportUnrendered; } }
        public void SetExportUnredererd(bool exportUnrendered){ this.exportUnrendered = exportUnrendered; }
    }
}