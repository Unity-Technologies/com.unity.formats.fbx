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
            exportSettings.exportFormat = (ExportModelSettingsSerialize.ExportFormat)EditorGUILayout.Popup((int)exportSettings.exportFormat, exportFormatOptions);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Include", "Select whether to export models, animation or both."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUI.BeginDisabledGroup(disableIncludeDropdown);
            exportSettings.include = (ExportModelSettingsSerialize.Include)EditorGUILayout.Popup((int)exportSettings.include, includeOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.include == ExportModelSettingsSerialize.Include.Anim);
            exportSettings.lodLevel = (ExportModelSettingsSerialize.LODExportType)EditorGUILayout.Popup((int)exportSettings.lodLevel, lodOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.include == ExportModelSettingsSerialize.Include.Anim);
            exportSettings.objectPosition = (ExportModelSettingsSerialize.ObjectPosition)EditorGUILayout.Popup((int)exportSettings.objectPosition, objPositionOptions);
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
            EditorGUI.BeginDisabledGroup(exportSettings.include == ExportModelSettingsSerialize.Include.Anim);
            exportSettings.exportUnrendered = EditorGUILayout.Toggle(exportSettings.exportUnrendered);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal ();
        }
    }

    public interface IExportOptions {
        ExportModelSettingsSerialize.ExportFormat GetExportFormat();
        ExportModelSettingsSerialize.Include GetModelAnimIncludeOption();
        ExportModelSettingsSerialize.LODExportType GetLODExportType();
        ExportModelSettingsSerialize.ObjectPosition GetObjectPosition();
        void SetObjectPosition(ExportModelSettingsSerialize.ObjectPosition objPos);
        bool AnimateSkinnedMesh();
        bool UseMayaCompatibleNames();
        bool ExportUnrendered();
    }

    public class ExportModelSettings : ScriptableObject, IExportOptions
    {
        public ExportModelSettingsSerialize info;

        public ExportModelSettings ()
        {
            info = new ExportModelSettingsSerialize ();
        }

        public ExportModelSettingsSerialize.ExportFormat GetExportFormat(){
            return info.exportFormat;
        }
        public ExportModelSettingsSerialize.Include GetModelAnimIncludeOption(){
            return info.include;
        }
        public void SetModelAnimIncludeOption(ExportModelSettingsSerialize.Include include){
            info.include = include;
        }
        public ExportModelSettingsSerialize.LODExportType GetLODExportType(){
            return info.lodLevel;
        }
        public void SetLODExportType(ExportModelSettingsSerialize.LODExportType lodType){
            info.lodLevel = lodType;
        }
        public ExportModelSettingsSerialize.ObjectPosition GetObjectPosition(){
            return info.objectPosition;
        }
        public void SetObjectPosition(ExportModelSettingsSerialize.ObjectPosition objPos){
            info.objectPosition = objPos;
        }
        public bool AnimateSkinnedMesh(){
            return info.animatedSkinnedMesh;
        }
        public bool UseMayaCompatibleNames(){
            return info.mayaCompatibleNaming;
        }
        public bool ExportUnrendered(){
            return info.exportUnrendered;
        }
    }

    [System.Serializable]
    public class ExportModelSettingsSerialize
    {
        public enum ExportFormat { ASCII = 0, Binary = 1}

        public enum Include { Model = 0, Anim = 1, ModelAndAnim = 2 }

        public enum ObjectPosition { LocalCentered = 0, WorldAbsolute = 1, Reset = 2 /* For convert to model only, no UI option*/}

        public enum LODExportType { All = 0, Highest = 1, Lowest = 2 }

        public ExportFormat exportFormat = ExportFormat.ASCII;
        public Include include = Include.ModelAndAnim;
        public LODExportType lodLevel = LODExportType.All;
        public ObjectPosition objectPosition = ObjectPosition.LocalCentered;
        public string rootMotionTransfer = "";
        public bool animatedSkinnedMesh = true;
        public bool mayaCompatibleNaming = true;
        public bool exportUnrendered = true;
    }
}