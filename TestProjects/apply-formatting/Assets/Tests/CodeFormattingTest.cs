using Unity.Coding.Editor.Formatting;
using UnityEditor;


namespace Automation
{
    static class Format
    {
        public static void ApplyFormatting()
        {
            Formatting.Format("Packages/com.unity.formats.fbx");
            EditorApplication.Exit(0);
        }
    }
}
