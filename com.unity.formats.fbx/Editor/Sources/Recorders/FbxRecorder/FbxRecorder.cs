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
                clip.legacy = true;
                Animation animator = root.GetComponent<Animation>();
                bool hasAnimComponent = true;
                if (!animator)
                {
                    animator = root.AddComponent<Animation>();
                    hasAnimComponent = false;
                }

                AnimationClip[] prevAnimClips = null;
                if (hasAnimComponent)
                {
                    prevAnimClips = AnimationUtility.GetAnimationClips(root);
                }

                AnimationUtility.SetAnimationClips(animator, new AnimationClip[] { clip });
                var exportSettings = new ExportModelSettingsSerialize();
                var toInclude = ExportSettings.Include.ModelAndAnim;
                exportSettings.SetModelAnimIncludeOption(toInclude);
                ModelExporter.ExportObject(clipName, root, exportSettings);


                if (hasAnimComponent)
                {
                    AnimationUtility.SetAnimationClips(animator, prevAnimClips);
                }
                else
                {
                    Object.DestroyImmediate(animator);
                }
                aInput.GameObjectRecorder.ResetRecording();
            }
            base.EndRecording(session);
        }
    }
}
