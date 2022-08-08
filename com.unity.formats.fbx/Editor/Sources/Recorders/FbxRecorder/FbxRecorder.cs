#if ENABLE_FBX_RECORDER
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace UnityEditor.Formats.Fbx.Exporter
{
    class FbxRecorder : GenericRecorder<FbxRecorderSettings>
    {
        protected override void RecordFrame(RecordingSession ctx)
        {
        }

        private void EndRecordingInternal(RecordingSession session)
        {
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
                var options = settings.GetCurveFilterOptions(settings.AnimationInputSettings.SimplyCurves);
                aInput.GameObjectRecorder.SaveToClip(clip, settings.FrameRate, options);
#else
                    aInput.GameObjectRecorder.SaveToClip(clip, settings.FrameRate);
#endif
                if (settings.AnimationInputSettings.ClampedTangents)
                {
                    FilterClip(clip);
                }

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
        }

        protected override void EndRecording(RecordingSession session)
        {
            if (session == null)
            {
                throw new System.ArgumentNullException("session");
            }

            if (Recording)
            {
                EndRecordingInternal(session);
            }
            base.EndRecording(session);
        }

        void FilterClip(AnimationClip clip)
        {
            foreach (var bind in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, bind);
                for (var i = 0; i < curve.keys.Length; ++i)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                }
                AnimationUtility.SetEditorCurve(clip, bind, curve);
            }
        }
    }
}
#endif // ENABLE_FBX_RECORDER