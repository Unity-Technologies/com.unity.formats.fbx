using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Presets;

public class FbxExportPresetSelectorReceiver : PresetSelectorReceiver
{
    UnityEngine.Object m_Target;
    Preset m_InitialValue;

    public delegate void SelectionChangedDelegate();
    public event SelectionChangedDelegate SelectionChanged;

    public delegate void DialogClosedDelegate();
    public event DialogClosedDelegate DialogClosed;

    public override void OnSelectionClosed(Preset selection)
    {
        OnSelectionChanged(selection);
        if (DialogClosed != null) {
            DialogClosed ();
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
        if (SelectionChanged != null) {
            SelectionChanged ();
        }
    }

    public void SetTarget(UnityEngine.Object target){
        m_Target = target;
    }

    public void SetInitialValue(Preset initialValue){
        m_InitialValue = initialValue;
    }
}