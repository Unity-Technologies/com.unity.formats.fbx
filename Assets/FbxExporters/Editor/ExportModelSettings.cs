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
        private string[] lodOptions = new string[]{"All", "Highest", "Lowest"};

        public const string singleHierarchyOption = "Local Pivot";
        public const string multiHerarchyOption = "Local Centered";
        private string hierarchyDepOption = "";
        private string[] objPositionOptions { get { return new string[]{hierarchyDepOption, "World Absolute"}; }}

        public void SetIsSingleHierarchy(bool singleHierarchy){
            if (singleHierarchy) {
                hierarchyDepOption = singleHierarchyOption;
                return;
            }
            hierarchyDepOption = multiHerarchyOption;
        }

        public override void OnInspectorGUI ()
        {
            var exportSettings = ((ExportModelSettings)target).info;

            EditorGUIUtility.labelWidth = LabelWidth;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Format", "Export the FBX file in the standard binary format." +
                " Select ASCII to export the FBX file in ASCII format."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.exportFormat = (ExportModelSettingsSerialize.ExportFormat)EditorGUILayout.Popup((int)exportSettings.exportFormat, exportFormatOptions);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Include", "Select whether to export models, animation or both."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.include = (ExportModelSettingsSerialize.Include)EditorGUILayout.Popup((int)exportSettings.include, includeOptions);
            GUILayout.EndHorizontal();

            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.include == ExportModelSettingsSerialize.Include.Anim);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.lodLevel = (ExportModelSettingsSerialize.LODExportType)EditorGUILayout.Popup((int)exportSettings.lodLevel, lodOptions);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.objectPosition = (ExportModelSettingsSerialize.ObjectPosition)EditorGUILayout.Popup((int)exportSettings.objectPosition, objPositionOptions);
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup ();

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
        }
    }

    public class ExportModelSettings : ScriptableObject
    {
        public ExportModelSettingsSerialize info;

        public ExportModelSettings ()
        {
            info = new ExportModelSettingsSerialize ();
        }
    }

    [System.Serializable]
    public class ExportModelSettingsSerialize
    {
        public enum ExportFormat { ASCII = 0, Binary = 1}

        public enum Include { Model = 0, Anim = 1, ModelAndAnim = 2 }

        public enum ObjectPosition { LocalCentered = 0, WorldAbsolute = 1 }

        public enum LODExportType { All = 0, Highest = 1, Lowest = 2 }

        public ExportFormat exportFormat = ExportFormat.ASCII;
        public Include include = Include.ModelAndAnim;
        public LODExportType lodLevel = LODExportType.All;
        public ObjectPosition objectPosition = ObjectPosition.LocalCentered;
        public string rootMotionTransfer = "";
        public bool animatedSkinnedMesh = true;
        public bool mayaCompatibleNaming = true;
    }
}