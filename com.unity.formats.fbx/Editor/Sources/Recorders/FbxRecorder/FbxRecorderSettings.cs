using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace UnityEditor.Formats.Fbx.Exporter
{
    [RecorderSettings(typeof(FbxRecorder), "FBX", "fbx_recorder")]
    public class FbxRecorderSettings : RecorderSettings
    {
        [SerializeField] bool m_exportGeometry = true;
        public bool ExportGeometry
        {
            get
            {
                return m_exportGeometry;
            }
            set
            {
                m_exportGeometry = value;
            }
        }

        [SerializeField] AnimationInputSettings m_AnimationInputSettings = new AnimationInputSettings();

        public AnimationInputSettings animationInputSettings
        {
            get { return m_AnimationInputSettings; }
            set { m_AnimationInputSettings = value; }
        }

        public FbxRecorderSettings()
        {
            var goWildcard = DefaultWildcard.GeneratePattern("GameObject");

            FileNameGenerator.AddWildcard(goWildcard, GameObjectNameResolver);
            FileNameGenerator.AddWildcard(DefaultWildcard.GeneratePattern("GameObjectScene"), GameObjectSceneNameResolver);

            FileNameGenerator.ForceAssetsFolder = false;
            FileNameGenerator.Root = OutputPath.Root.AssetsFolder;
            FileNameGenerator.FileName = "animation_" + goWildcard + "_" + DefaultWildcard.Take;
        }

        string GameObjectNameResolver(RecordingSession session)
        {
            var go = m_AnimationInputSettings.gameObject;
            return go != null ? go.name : "None";
        }

        string GameObjectSceneNameResolver(RecordingSession session)
        {
            var go = m_AnimationInputSettings.gameObject;
            return go != null ? go.scene.name : "None";
        }

        public override bool IsPlatformSupported
        {
            get
            {
                return Application.platform == RuntimePlatform.LinuxEditor ||
                       Application.platform == RuntimePlatform.OSXEditor ||
                       Application.platform == RuntimePlatform.WindowsEditor;
            }
        }

        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get { yield return m_AnimationInputSettings; }
        }

        protected override bool ValidityCheck(List<string> errors)
        {
            var ok = base.ValidityCheck(errors);

            if (m_AnimationInputSettings.gameObject == null)
            {
                ok = false;
                if(errors == null)
                {
                    throw new System.ArgumentNullException("errors");
                }
                errors.Add("No input object set");
            }

            return ok;
        }

        public override void OnAfterDuplicate()
        {
            m_AnimationInputSettings.DuplicateExposedReference();
        }

        void OnDestroy()
        {
            m_AnimationInputSettings.ClearExposedReference();
        }

        protected override string Extension
        {
            get { return "fbx"; }
        }
    }
}