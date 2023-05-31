#if UNITY_2018_1_OR_NEWER
using UnityEditor.Presets;

namespace UnityEditor.Formats.Fbx.Exporter
{
    internal delegate void SelectionChangedDelegate();
    internal delegate void DialogClosedDelegate();

#pragma warning disable CS0618 // Suppress the warning about obsolete class "PresetSelectorReceiver" FBX-452
    internal class FbxExportPresetSelectorReceiver : PresetSelectorReceiver
#pragma warning restore CS0618 // Restore the warning
    {
        UnityEngine.Object m_Target;
        Preset m_InitialValue;

        public event SelectionChangedDelegate SelectionChanged;
        public event DialogClosedDelegate DialogClosed;

        public override void OnSelectionClosed(Preset selection)
        {
            OnSelectionChanged(selection);
            if (DialogClosed != null)
            {
                DialogClosed();
            }
            DestroyImmediate(this);
        }

        public override void OnSelectionChanged(Preset selection)
        {
            if (selection != null)
            {
                selection.ApplyTo(m_Target);
            }
            else
            {
                m_InitialValue.ApplyTo(m_Target);
            }
            if (SelectionChanged != null)
            {
                SelectionChanged();
            }
        }

        public void SetTarget(UnityEngine.Object target)
        {
            m_Target = target;
        }

        public void SetInitialValue(Preset initialValue)
        {
            m_InitialValue = initialValue;
        }
    }
}
#endif
