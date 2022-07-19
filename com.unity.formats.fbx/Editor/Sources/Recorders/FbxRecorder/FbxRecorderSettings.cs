#if ENABLE_FBX_RECORDER
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace UnityEditor.Formats.Fbx.Exporter
{
    /// <summary>
    /// Class describing the settings for the FBX Recorder.
    /// </summary>
    [RecorderSettings(typeof(FbxRecorder), "FBX")]
    public class FbxRecorderSettings : RecorderSettings
    {
        [SerializeField] bool m_exportGeometry = true;

        /// <summary>
        /// Option to export the geometry/meshes of the recorded hierarchy to FBX.
        /// </summary>
        public bool ExportGeometry
        {
            get
            {
                return m_exportGeometry;
            }
            set
            {
                m_exportGeometry = value;
            }
        }

        [SerializeField]
        private string m_animSourceBindingId;
        [SerializeField]
        private string m_animDestBindingId;

        /// <summary>
        /// Option to transfer the transform animation from this transform to the destination.
        /// This also transfers to the destination any animation on GameObjects between the source and the destination.
        /// </summary>
        public Transform TransferAnimationSource
        {
            get
            {
                if (string.IsNullOrEmpty(m_animSourceBindingId))
                    return null;

                return GetBinding(m_animSourceBindingId);
            }

            set
            {
                if (!TransferAnimationSourceIsValid(value))
                {
                    return;
                }
                if (string.IsNullOrEmpty(m_animSourceBindingId))
                    m_animSourceBindingId = GenerateBindingId();

                SetBinding(m_animSourceBindingId, value);
            }
        }

        /// <summary>
        /// Option to transfer the transform animation from the source to this transform.
        /// This also transfers to the destination any animation on GameObjects between the source and the destination.
        /// </summary>
        public Transform TransferAnimationDest
        {
            get
            {
                if (string.IsNullOrEmpty(m_animDestBindingId))
                    return null;

                return GetBinding(m_animDestBindingId);
            }

            set
            {
                if (!TransferAnimationDestIsValid(value))
                {
                    return;
                }
                if (string.IsNullOrEmpty(m_animDestBindingId))
                    m_animDestBindingId = GenerateBindingId();

                SetBinding(m_animDestBindingId, value);
            }
        }

        /// <summary>
        /// Get a binding ID for the transform, so that the reference survives domain reload. Maintaining a direct reference would not work
        /// as all scene objects are destroyed and recreated on reload (e.g. when entering/exiting playmode).
        /// </summary>
        /// <returns>Binding ID</returns>
        static string GenerateBindingId()
        {
            return GUID.Generate().ToString();
        }

        /// <summary>
        /// Get the Unity object (in this case transform), associated with the given binding ID.
        /// </summary>
        /// <param name="id">Binding ID</param>
        /// <returns>Transform associated with binding ID</returns>
        static Transform GetBinding(string id)
        {
            return BindingManager.Get(id) as Transform;
        }

        /// <summary>
        /// Set the binding ID to be associated with the given Unity object.
        /// This information will be saved on domain reload, so that the object can still be found
        /// with the binding ID.
        /// </summary>
        /// <param name="id">Binding ID</param>
        /// <param name="obj">Unity Object</param>
        static void SetBinding(string id, UnityEngine.Object obj)
        {
            BindingManager.Set(id, obj);
        }

        /// <summary>
        /// Determines whether p is an ancestor to t.
        /// </summary>
        /// <returns><c>true</c> if p is ancestor to t; otherwise, <c>false</c>.</returns>
        /// <param name="p">P.</param>
        /// <param name="t">T.</param>
        internal bool IsAncestor(Transform p, Transform t)
        {
            var curr = t;
            while (curr != null)
            {
                if (curr == p)
                {
                    return true;
                }
                curr = curr.parent;
            }
            return false;
        }

        /// <summary>
        /// Determines whether t1 and t2 are in the same hierarchy.
        /// </summary>
        /// <returns><c>true</c> if t1 is in same hierarchy as t2; otherwise, <c>false</c>.</returns>
        /// <param name="t1">T1.</param>
        /// <param name="t2">T2.</param>
        internal bool IsInSameHierarchy(Transform t1, Transform t2)
        {
            return (IsAncestor(t1, t2) || IsAncestor(t2, t1));
        }

        internal bool TransferAnimationSourceIsValid(Transform newValue)
        {
            if (!newValue)
            {
                return true;
            }

            var selectedGO = m_AnimationInputSettings.gameObject;

            if (!selectedGO)
            {
                Debug.LogWarning("FbxRecorderSettings: no Objects selected for export, can't transfer animation");
                return false;
            }

            // source must be ancestor to dest
            if (TransferAnimationDest && !IsAncestor(newValue, TransferAnimationDest))
            {
                Debug.LogWarningFormat("FbxRecorderSettings: Source {0} must be an ancestor of {1}", newValue.name, TransferAnimationDest.name);
                return false;
            }
            // must be in same hierarchy as selected GO
            if (!selectedGO || !IsInSameHierarchy(newValue, selectedGO.transform))
            {
                Debug.LogWarningFormat("FbxRecorderSettings: Source {0} must be in the same hierarchy as {1}", newValue.name, selectedGO ? selectedGO.name : "the selected object");
                return false;
            }
            return true;
        }

        internal bool TransferAnimationDestIsValid(Transform newValue)
        {
            if (!newValue)
            {
                return true;
            }

            var selectedGO = m_AnimationInputSettings.gameObject;

            if (!selectedGO)
            {
                Debug.LogWarning("FbxRecorderSettings: no Objects selected for export, can't transfer animation");
                return false;
            }

            // source must be ancestor to dest
            if (TransferAnimationSource && !IsAncestor(TransferAnimationSource, newValue))
            {
                Debug.LogWarningFormat("FbxRecorderSettings: Destination {0} must be a descendant of {1}", newValue.name, TransferAnimationSource.name);
                return false;
            }
            // must be in same hierarchy as selected GO
            if (!selectedGO || !IsInSameHierarchy(newValue, selectedGO.transform))
            {
                Debug.LogWarningFormat("FbxRecorderSettings: Destination {0} must be in the same hierarchy as {1}", newValue.name, selectedGO ? selectedGO.name : "the selected object");
                return false;
            }
            return true;
        }

        [SerializeField] AnimationInputSettings m_AnimationInputSettings = new AnimationInputSettings();

        /// <summary>
        /// Stores the reference to the current FBX Recorder's input settings.
        /// </summary>
        public AnimationInputSettings AnimationInputSettings
        {
            get { return m_AnimationInputSettings; }
            set { m_AnimationInputSettings = value; }
        }

        /// <summary>
        /// Default constructor for FbxRecorderSettings.
        /// </summary>
        public FbxRecorderSettings()
        {
            var goWildcard = DefaultWildcard.GeneratePattern("GameObject");

            FileNameGenerator.AddWildcard(goWildcard, GameObjectNameResolver);
            FileNameGenerator.AddWildcard(DefaultWildcard.GeneratePattern("GameObjectScene"), GameObjectSceneNameResolver);

            FileNameGenerator.ForceAssetsFolder = false;
            FileNameGenerator.Root = OutputPath.Root.AssetsFolder;
            FileNameGenerator.FileName = "animation_" + goWildcard + "_" + DefaultWildcard.Take;
        }

        string GameObjectNameResolver(RecordingSession session)
        {
            var go = m_AnimationInputSettings.gameObject;
            return go != null ? go.name : "None";
        }

        string GameObjectSceneNameResolver(RecordingSession session)
        {
            var go = m_AnimationInputSettings.gameObject;
            return go != null ? go.scene.name : "None";
        }

        /// <summary>
        /// Indicates if the current platform is supported (True) or not (False).
        /// </summary>
        /// <remarks>
        /// FBX Recorder currently supports the following platforms: LinuxEditor, OSXEditor, WindowsEditor.
        /// </remarks>
        public override bool IsPlatformSupported
        {
            get
            {
                return Application.platform == RuntimePlatform.LinuxEditor ||
                    Application.platform == RuntimePlatform.OSXEditor ||
                    Application.platform == RuntimePlatform.WindowsEditor;
            }
        }

        /// <summary>
        /// Stores the list of Input settings required by this Recorder.
        /// </summary>
        public override IEnumerable<RecorderInputSettings> InputsSettings
        {
            get { yield return m_AnimationInputSettings; }
        }

        /// <summary>
        /// Tests if the Recorder has any errors.
        /// </summary>
        /// <param name="errors">List of errors encountered.</param>
        protected override void GetErrors(List<string> errors)
        {
            base.GetErrors(errors);

            if (m_AnimationInputSettings.gameObject == null)
            {
                if (errors == null)
                {
                    throw new System.ArgumentNullException("errors");
                }
                errors.Add("No input object set");
            }
        }

        /// <summary>
        /// Override this method if you need to do any post treatment after duplicating this Recorder in the Recorder Window.
        /// </summary>
        public override void OnAfterDuplicate()
        {
            m_AnimationInputSettings.DuplicateExposedReference();
        }

        void OnDestroy()
        {
            m_AnimationInputSettings.ClearExposedReference();
        }

        /// <summary>
        /// Stores the file extension used by this Recorder (without the dot).
        /// </summary>
        protected override string Extension
        {
            get { return "fbx"; }
        }
    }
}
#endif // ENABLE_FBX_RECORDER
