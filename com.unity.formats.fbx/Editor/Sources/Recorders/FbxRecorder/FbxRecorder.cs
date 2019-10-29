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
        public override void RecordFrame(RecordingSession ctx)
        {

        }

        public override void EndRecording(RecordingSession session)
        {
            var ars = (FbxRecorderSettings)session.settings;

            foreach (var input in m_Inputs)
            {

                var aInput = (FbxInput)input;

                var recorders = aInput.gameObjectRecorder;
                var gameObjects = ((FbxInputSettings)aInput.settings).gameObjects;
                if (recorders == null || recorders.Length <= 0)
                    continue;

                var clips = new AnimationClip[recorders.Length];
                for (int i = 0; i < recorders.Length; i++)
                {
                    clips[i] = new AnimationClip();

                    ars.fileNameGenerator.CreateDirectory(session);

                    var absolutePath = FileNameGenerator.SanitizePath(ars.fileNameGenerator.BuildAbsolutePath(session));
                    var clipName = absolutePath.Replace(FileNameGenerator.SanitizePath(Application.dataPath), "Assets");

                    //AssetDatabase.CreateAsset(clip, clipName);
    #if UNITY_2018_3_OR_NEWER
                        recorders[i].SaveToClip(clips[i], ars.frameRate);
#else
                        recorder.SaveToClip(clip);
#endif
                    var root = gameObjects[i];
                    clips[i].name = "recorded_clip";
                    clips[i].legacy = true;
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

                    AnimationUtility.SetAnimationClips(animator, new AnimationClip[] { clips[i] });
                    var exportSettings = new ExportModelSettingsSerialize();
                    var toInclude = ExportSettings.Include.ModelAndAnim;
                    if (!ars.ExportGeometry)
                    {
                        toInclude = ExportSettings.Include.Anim;
                    } 
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
                    recorders[i].ResetRecording();
                }
            }
            base.EndRecording(session);
        }
    }
}
