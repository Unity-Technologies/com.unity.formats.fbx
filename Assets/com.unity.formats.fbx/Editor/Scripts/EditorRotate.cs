using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FbxExporters.Review
{
    [CustomEditor (typeof(FbxTurnTableBase))]
    public class EditorRotate : UnityEditor.Editor
    {
        FbxTurnTableBase model;

        public void OnEnable ()
        {
            model = (FbxTurnTableBase)target;
            EditorApplication.update += Update;
        }

        public void OnDisable ()
        {
            EditorApplication.update -= Update;
        }

        void Update ()
        {
            // don't do anything in play mode
            if (model == null || EditorApplication.isPlaying) {
                return;
            }
            model.Rotate ();
        }
    }
}