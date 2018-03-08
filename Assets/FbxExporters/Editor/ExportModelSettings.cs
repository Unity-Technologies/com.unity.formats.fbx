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

            // TODO: add implementation for these options, grey out in the meantime
            EditorGUI.BeginDisabledGroup (true);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Transfer Root Motion To", "Select bone to transfer root motion animation to."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUILayout.Popup(0, new string[]{"<None>"});
            GUILayout.EndHorizontal();

            exportSettings.animatedSkinnedMesh = EditorGUILayout.Toggle ("Animated Skinned Mesh", exportSettings.animatedSkinnedMesh);
            EditorGUI.EndDisabledGroup ();

            exportSettings.mayaCompatibleNaming = EditorGUILayout.Toggle (
                new GUIContent ("Compatible Naming:",
                    "In Maya some symbols such as spaces and accents get replaced when importing an FBX " +
                    "(e.g. \"foo bar\" becomes \"fooFBXASC032bar\"). " +
                    "On export, convert the names of GameObjects so they are Maya compatible." +
                    (exportSettings.mayaCompatibleNaming ? "" :
                        "\n\nWARNING: Disabling this feature may result in lost material connections," +
                        " and unexpected character replacements in Maya.")
                ),
                exportSettings.mayaCompatibleNaming);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Unrendered:",
                "If checked, meshes will be exported even if they don't have a Renderer component."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.include == ExportSettings.Include.Anim);
            exportSettings.exportUnrendered = EditorGUILayout.Toggle(exportSettings.exportUnrendered);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal ();
        }
    }

    public interface IExportOptions {
        ExportSettings.ExportFormat ExportFormat { get; set; }
        ExportSettings.Include ModelAnimIncludeOption { get; set; }
        ExportSettings.LODExportType LODExportType { get; set; }
        ExportSettings.ObjectPosition ObjectPosition { get; set; }
        bool AnimateSkinnedMesh { get; set; }
        bool UseMayaCompatibleNames { get; set; }
        bool ExportUnrendered { get; set; }
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
        public bool animatedSkinnedMesh = true;
        public bool mayaCompatibleNaming = true;

        public ExportSettings.ExportFormat ExportFormat { get { return exportFormat; } set { exportFormat = value; } }
        public bool AnimateSkinnedMesh { get { return animatedSkinnedMesh; } set { animatedSkinnedMesh = value; } }
        public bool UseMayaCompatibleNames { get { return mayaCompatibleNaming; } set { mayaCompatibleNaming = value; } }
        public abstract ExportSettings.Include ModelAnimIncludeOption { get; set; }
        public abstract ExportSettings.LODExportType LODExportType { get; set; }
        public abstract ExportSettings.ObjectPosition ObjectPosition { get; set; }
        public abstract bool ExportUnrendered { get; set; }
    }

    [System.Serializable]
    public class ExportModelSettingsSerialize : ExportOptionsSettingsSerializeBase
    {
        public ExportSettings.Include include = ExportSettings.Include.ModelAndAnim;
        public ExportSettings.LODExportType lodLevel = ExportSettings.LODExportType.All;
        public ExportSettings.ObjectPosition objectPosition = ExportSettings.ObjectPosition.LocalCentered;
        public bool exportUnrendered = true;

        public override ExportSettings.Include ModelAnimIncludeOption { get { return include; } set { include = value; } }
        public override ExportSettings.LODExportType LODExportType { get { return lodLevel; } set { lodLevel = value; } }
        public override ExportSettings.ObjectPosition ObjectPosition { get { return objectPosition; } set { objectPosition = value; } }
        public override bool ExportUnrendered { get { return exportUnrendered; } set { exportUnrendered = value; } }
    }
}