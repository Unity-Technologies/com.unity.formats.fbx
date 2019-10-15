using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder.Input;


namespace UnityEditor.Recorder
{
    class FbxInput : RecorderInput
    {
        public FbxInput()
        {
        }

        public override void BeginRecording(RecordingSession session)
        {
            base.BeginRecording(session);
        }

        public override void EndRecording(RecordingSession session)
        {
            base.EndRecording(session);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override void FrameDone(RecordingSession session)
        {
            base.FrameDone(session);
            Debug.LogWarning("frame " + session.frameIndex + ": " + ((FbxInputSettings)settings).gameObject.transform.localEulerAngles);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void NewFrameReady(RecordingSession session)
        {
            base.NewFrameReady(session);
        }

        public override void NewFrameStarting(RecordingSession session)
        {
            base.NewFrameStarting(session);
        }

        public override void SessionCreated(RecordingSession session)
        {
            base.SessionCreated(session);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}