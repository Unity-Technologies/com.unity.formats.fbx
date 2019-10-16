using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor;

namespace UnityEditor.Formats.Fbx.Exporter
{
    class FbxRecorder : GenericRecorder<FbxRecorderSettings>//GenericRecorder<FbxRecorderSettings>
    {
        public override void RecordFrame(RecordingSession ctx)
        {
            //Debug.LogWarning("Frame " + ctx.frameIndex + ": " + ctx.);
        }

        public override void EndRecording(RecordingSession session)
        {
            var ars = (FbxRecorderSettings)session.settings;

            foreach (var input in m_Inputs)
            {

                var aInput = (AnimationInput)input;

                if (aInput.gameObjectRecorder == null)
                    continue;

                var clip = new AnimationClip();

                ars.fileNameGenerator.CreateDirectory(session);

                var absolutePath = FileNameGenerator.SanitizePath(ars.fileNameGenerator.BuildAbsolutePath(session));
                var clipName = absolutePath.Replace(FileNameGenerator.SanitizePath(Application.dataPath), "Assets");

                //var tempClipName = System.IO.Path.ChangeExtension(clipName, ".asset");
                //AssetDatabase.CreateAsset(clip, tempClipName);
#if UNITY_2018_3_OR_NEWER
                aInput.gameObjectRecorder.SaveToClip(clip, ars.frameRate);
#else
                aInput.gameObjectRecorder.SaveToClip(clip);
#endif
                var root = ((AnimationInputSettings)aInput.settings).gameObject;
                clip.name = "recorded_clip";
                Animation animator = root.AddComponent<Animation>();
                AnimationUtility.SetAnimationClips(animator, new AnimationClip[] { clip });
                var exportSettings = new ExportModelSettingsSerialize();
                exportSettings.SetModelAnimIncludeOption(ExportSettings.Include.Anim);
                ModelExporter.ExportObject(clipName, root, exportSettings);


                Object.DestroyImmediate(animator);
                aInput.gameObjectRecorder.ResetRecording();
            }

            base.EndRecording(session);
        }
    }
}
