#if ENABLE_FBX_RECORDER
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
            if (session == null)
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

#if UNITY_2019_3_OR_NEWER
                var options = new Animations.CurveFilterOptions();
                options.keyframeReduction = false;
                aInput.GameObjectRecorder.SaveToClip(clip, settings.FrameRate, options);
#else
                aInput.GameObjectRecorder.SaveToClip(clip, settings.FrameRate);
#endif
                var root = ((AnimationInputSettings)aInput.settings).gameObject;
                clip.name = "recorded_clip";

                var exportSettings = new ExportModelSettingsSerialize();
                exportSettings.SetAnimationSource(settings.TransferAnimationSource);
                exportSettings.SetAnimationDest(settings.TransferAnimationDest);
                exportSettings.SetObjectPosition(ObjectPosition.WorldAbsolute);
                var toInclude = Include.ModelAndAnim;
                if (!settings.ExportGeometry)
                {
                    toInclude = Include.Anim;
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
#endif // ENABLE_FBX_RECORDER