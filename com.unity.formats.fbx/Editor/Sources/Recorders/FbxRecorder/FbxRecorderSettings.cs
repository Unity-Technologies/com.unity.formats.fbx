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

        [SerializeField] FbxInputSettings m_AnimationInputSettings = new FbxInputSettings();

        public FbxInputSettings animationInputSettings
        {
            get { return m_AnimationInputSettings; }
            set { m_AnimationInputSettings = value; }
        }

        public FbxRecorderSettings()
        {
            //var goWildcard = DefaultWildcard.GeneratePattern("GameObject");

            //fileNameGenerator.AddWildcard(goWildcard, GameObjectNameResolver);
            fileNameGenerator.AddWildcard(DefaultWildcard.GeneratePattern("GameObjectScene"), GameObjectSceneNameResolver);

            fileNameGenerator.forceAssetsFolder = true;
            fileNameGenerator.root = OutputPath.Root.AssetsFolder;
            fileNameGenerator.fileName = "animation_" /*+ goWildcard*/ + "@" + DefaultWildcard.Take;
        }

        string GameObjectNameResolver(RecordingSession session)
        {
            var go = m_AnimationInputSettings.gameObjects;
            return /*go != null ? go[0].name :*/ "None";
        }

        string GameObjectSceneNameResolver(RecordingSession session)
        {
            var go = m_AnimationInputSettings.gameObjects;
            return go != null && go.Length > 0 ? go[0].scene.name : "None";
        }

        public override bool isPlatformSupported
        {
            get
            {
                return Application.platform == RuntimePlatform.LinuxEditor ||
                       Application.platform == RuntimePlatform.OSXEditor ||
                       Application.platform == RuntimePlatform.WindowsEditor;
            }
        }

        public override IEnumerable<RecorderInputSettings> inputsSettings
        {
            get { yield return m_AnimationInputSettings; }
        }

        internal override bool ValidityCheck(List<string> errors)
        {
            var ok = base.ValidityCheck(errors);

            if (m_AnimationInputSettings.gameObjects == null)
            {
                ok = false;
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

        public override string extension
        {
            get { return "fbx"; }
        }
    }
}