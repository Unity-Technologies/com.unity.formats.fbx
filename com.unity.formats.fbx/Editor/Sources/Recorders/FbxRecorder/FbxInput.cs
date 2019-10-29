#if UNITY_2018_3_OR_NEWER
using UnityEditor.Animations;
#else
using UnityEditor.Experimental.Animations;
#endif
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace UnityEditor.Formats.Fbx.Exporter
{
    class FbxInput : RecorderInput
    {
        public GameObjectRecorder[] gameObjectRecorder { get; private set; }
        float m_Time;

        public override void BeginRecording(RecordingSession session)
        {
            var aniSettings = (FbxInputSettings)settings;

            var srcGOs = aniSettings.gameObjects;

            if (srcGOs == null || srcGOs.Length <= 0)
                return;

            gameObjectRecorder = new GameObjectRecorder[srcGOs.Length];
            for (int i = 0; i < srcGOs.Length; i++)
            {
                gameObjectRecorder[i] = new GameObjectRecorder(srcGOs[i]);
            }

            /*foreach (var binding in aniSettings.bindingType)
            {
                gameObjectRecorder.BindComponentsOfType(srcGOs, binding, aniSettings.recursive);
            }*/

            m_Time = session.recorderTime;
        }

        public override void NewFrameReady(RecordingSession session)
        {
            if (gameObjectRecorder != null && gameObjectRecorder.Length > 0 && session.isRecording)
            {
                foreach(var goRecorder in gameObjectRecorder) {
                    goRecorder.TakeSnapshot(session.recorderTime - m_Time);
                }
                m_Time = session.recorderTime;
            }
        }
    }
}