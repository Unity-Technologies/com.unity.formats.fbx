using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Recorder
{
    [Serializable]
    [DisplayName("Fbx")]
    public class FbxInputSettings : RecorderInputSettings
    {

        [SerializeField] string m_BindingId = null;

        static string GenerateBindingId()
        {
            return GUID.Generate().ToString();
        }

        /// <summary>
        /// The gameObject to record from.
        /// </summary>
        public GameObject gameObject
        {
            get
            {
                if (string.IsNullOrEmpty(m_BindingId))
                    return null;

                return BindingManager.Get(m_BindingId) as GameObject;
            }

            set
            {
                if (string.IsNullOrEmpty(m_BindingId))
                    m_BindingId = GenerateBindingId();

                BindingManager.Set(m_BindingId, value);
            }
        }

        internal override bool ValidityCheck(List<string> errors)
        {
            return true;
        }

        internal override Type inputType
        {
            get { return typeof(FbxInput); }
        }
    }

    [CustomPropertyDrawer(typeof(FbxInputSettings))]
    class AnimationInputSettingsPropertyDrawer : InputPropertyDrawer<FbxInputSettings>
    {

        protected override void Initialize(SerializedProperty prop)
        {
            base.Initialize(prop);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);

            EditorGUI.BeginChangeCheck();

            var gameObject = EditorGUILayout.ObjectField("Game Object", target.gameObject, typeof(GameObject), true) as GameObject;

            if (EditorGUI.EndChangeCheck())
            {
                target.gameObject = gameObject;

                //if (gameObject != null)
                //    target.AddComponentToRecord(gameObject.GetComponent<Component>().GetType());
            }
        }
    }
}