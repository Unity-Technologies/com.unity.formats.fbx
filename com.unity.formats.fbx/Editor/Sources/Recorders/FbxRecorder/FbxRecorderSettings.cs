using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace UnityEditor.Recorder
{
    [RecorderSettings(typeof(FbxRecorder), "FBX", "fbx_recorder")]
    public class FbxRecorderSettings : RecorderSettings
    {
        [SerializeField] FbxInputSettings m_AnimationInputSettings = new FbxInputSettings();

        public override IEnumerable<RecorderInputSettings> inputsSettings
        {
            get { yield return m_AnimationInputSettings; }
        }

        public override string extension
        {
            get { return "fbx"; }
        }
    }
}