#if ENABLE_FBX_RECORDER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor;
using UnityEditor.Animations;
using System;

namespace UnityEditor.Formats.Fbx.Exporter
{
    class FbxRecorder : GenericRecorder<FbxRecorderSettings>
    {
        static readonly CurveFilterOptions DefaultCurveFilterOptions = new CurveFilterOptions()
        {
            keyframeReduction = true,
            positionError = 0.5f,
            rotationError = 0.5f,
            scaleError = 0.5f,
            floatError = 0.5f
        };

        static readonly CurveFilterOptions RegularCurveFilterOptions = new CurveFilterOptions
        {
            keyframeReduction = true
        };

        static readonly CurveFilterOptions NoCurveFilterOptions = new CurveFilterOptions
        {
            keyframeReduction = false
        };

        internal CurveFilterOptions GetCurveFilterOptions(AnimationInputSettings.CurveSimplificationOptions options)
        {
            switch (options)
            {
                case AnimationInputSettings.CurveSimplificationOptions.Lossy:
                    return DefaultCurveFilterOptions;
                case AnimationInputSettings.CurveSimplificationOptions.Lossless:
                    return RegularCurveFilterOptions;
                case AnimationInputSettings.CurveSimplificationOptions.Disabled:
                    return NoCurveFilterOptions;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override void RecordFrame(RecordingSession ctx)
        {
        }

        protected override void EndRecording(RecordingSession session)
        {
            if (session == null)
            {
                throw new System.ArgumentNullException("session");
            }

            if (Recording)
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
                    var options = GetCurveFilterOptions(settings.AnimationInputSettings.SimplyCurves);
                    aInput.GameObjectRecorder.SaveToClip(clip, settings.FrameRate, options);
#else
                    aInput.GameObjectRecorder.SaveToClip(clip, settings.FrameRate);
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
            }
            base.EndRecording(session);
        }
    }
}
#endif // ENABLE_FBX_RECORDER
