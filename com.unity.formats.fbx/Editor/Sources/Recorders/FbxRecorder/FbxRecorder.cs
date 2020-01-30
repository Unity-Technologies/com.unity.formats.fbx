using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor;

namespace UnityEditor.Formats.Fbx.Exporter
{
    class FbxRecorder : GenericRecorder<FbxRecorderSettings>
    {
        protected override void RecordFrame(RecordingSession ctx)
        {

        }

        protected override void EndRecording(RecordingSession session)
        {
            if(session == null)
            {
                throw new System.ArgumentNullException("session");
            }

            var settings = (FbxRecorderSettings)session.settings;

            foreach (var input in m_Inputs)
            {

                var aInput = (AnimationInput)input;

                if (aInput.GameObjectRecorder == null)
                    continue;

                var clip = new AnimationClip();

                settings.FileNameGenerator.CreateDirectory(session);

                var absolutePath = FileNameGenerator.SanitizePath(settings.FileNameGenerator.BuildAbsolutePath(session));
                var clipName = absolutePath.Replace(FileNameGenerator.SanitizePath(Application.dataPath), "Assets");
                
#if UNITY_2018_3_OR_NEWER
                aInput.GameObjectRecorder.SaveToClip(clip, settings.FrameRate);
#else
                aInput.gameObjectRecorder.SaveToClip(clip);
#endif
                var root = ((AnimationInputSettings)aInput.settings).gameObject;
                clip.name = "recorded_clip";

                var exportSettings = new ExportModelSettingsSerialize();
                exportSettings.SetAnimationSource(settings.TransferAnimationSource);
                exportSettings.SetAnimationDest(settings.TransferAnimationDest);
                exportSettings.SetObjectPosition(ExportSettings.ObjectPosition.WorldAbsolute);
                var toInclude = ExportSettings.Include.ModelAndAnim;
                if (!settings.ExportGeometry)
                {
                    toInclude = ExportSettings.Include.Anim;
                } 
                exportSettings.SetModelAnimIncludeOption(toInclude);

                var exportData = new AnimationOnlyExportData();
                exportData.CollectDependencies(clip, root, exportSettings);
                var exportDataContainer = new Dictionary<GameObject, IExportData>();
                exportDataContainer.Add(root, exportData);

                ModelExporter.ExportObjects(clipName, new UnityEngine.Object[] { root }, exportSettings, exportDataContainer);

                aInput.GameObjectRecorder.ResetRecording();
            }
            base.EndRecording(session);
        }
    }
}
