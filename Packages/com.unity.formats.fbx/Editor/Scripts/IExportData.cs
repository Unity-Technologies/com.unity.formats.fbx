using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using System.Collections.Generic;
using FbxExporters.EditorTools;

namespace FbxExporters.Editor
{
    /// <summary>
    /// Export data containing extra information required to export
    /// </summary>
    public interface IExportData
    {
        HashSet<GameObject> Objects { get; }
    }

    /// <summary>
    /// Export data containing what to export when
    /// exporting animation only.
    /// </summary>
    internal struct AnimationOnlyExportData : IExportData
    {
        // map from animation clip to GameObject that has Animation/Animator
        // component containing clip
        public Dictionary<AnimationClip, GameObject> animationClips;

        // set of all GameObjects to export
        public HashSet<GameObject> goExportSet;
        public HashSet<GameObject> Objects { get { return goExportSet; } }

        // map from GameObject to component type to export
        public Dictionary<GameObject, System.Type> exportComponent;

        // first clip to export
        public AnimationClip defaultClip;

        public AnimationOnlyExportData(
            Dictionary<AnimationClip, GameObject> animClips,
            HashSet<GameObject> exportSet,
            Dictionary<GameObject, System.Type> exportComponent
        )
        {
            this.animationClips = animClips;
            this.goExportSet = exportSet;
            this.exportComponent = exportComponent;
            this.defaultClip = null;
        }

        /// <summary>
        /// collect all objects dependencies for animation clips.
        /// </summary>
        public void CollectDependencies(
            AnimationClip[] animClips,
            GameObject rootObject,
            IExportOptions exportOptions
        )
        {
            Debug.Assert(rootObject != null);
            Debug.Assert(exportOptions != null);

            // NOTE: the object (animationRootObject) containing the animation is not necessarily animated
            // when driven by an animator or animation component.
            foreach (var animClip in animClips)
            {
                if (this.animationClips.ContainsKey(animClip))
                {
                    // we have already exported gameobjects for this clip
                    continue;
                }

                this.animationClips.Add(animClip, rootObject);

                foreach (EditorCurveBinding uniCurveBinding in AnimationUtility.GetCurveBindings(animClip))
                {
                    Object uniObj = AnimationUtility.GetAnimatedObject(rootObject, uniCurveBinding);
                    if (!uniObj)
                    {
                        continue;
                    }

                    GameObject unityGo = ModelExporter.GetGameObject(uniObj);
                    if (!unityGo)
                    {
                        continue;
                    }

                    if (!exportOptions.AnimateSkinnedMesh && unityGo.GetComponent<SkinnedMeshRenderer>())
                    {
                        continue;
                    }

                    // If we have a clip driving a camera or light then force the export of FbxNodeAttribute
                    // so that they point the right way when imported into Maya.
                    if (unityGo.GetComponent<Light>())
                        this.exportComponent[unityGo] = typeof(Light);
                    else if (unityGo.GetComponent<Camera>())
                        this.exportComponent[unityGo] = typeof(Camera);

                    this.goExportSet.Add(unityGo);
                }
            }
        }

        public static GameObject GetGameObjectBoundToEditorClip(object editorClip)
        {
            object clipItem = editorClip.GetType().GetProperty("item").GetValue(editorClip, null);
            object parentTrack = clipItem.GetType().GetProperty("parentTrack").GetValue(clipItem, null);
            AnimationTrack animTrack = parentTrack as AnimationTrack;

#if UNITY_2018_2_OR_NEWER
            Object animationTrackObject = UnityEditor.Timeline.TimelineEditor.inspectedDirector.GetGenericBinding(animTrack);
#else // UNITY_2018_2_OR_NEWER
                    Object animationTrackObject = UnityEditor.Timeline.TimelineEditor.playableDirector.GetGenericBinding(animTrack);
#endif // UNITY_2018_2_OR_NEWER

            GameObject animationTrackGO = null;
            if (animationTrackObject is GameObject)
            {
                animationTrackGO = animationTrackObject as GameObject;
            }
            else if (animationTrackObject is Animator)
            {
                animationTrackGO = (animationTrackObject as Animator).gameObject;
            }

            if (animationTrackGO == null)
            {
                Debug.LogErrorFormat("Could not export animation track object of type {0}", animationTrackObject.GetType().Name);
                return null;
            }
            return animationTrackGO;
        }

        public static KeyValuePair<GameObject, AnimationClip> GetGameObjectAndAnimationClip(Object obj)
        {
            if (!obj.GetType().Name.Contains("EditorClip"))
                return new KeyValuePair<GameObject, AnimationClip>();

            object clip = obj.GetType().GetProperty("clip").GetValue(obj, null);
            TimelineClip timeLineClip = clip as TimelineClip;

            var animationTrackGO = GetGameObjectBoundToEditorClip(obj);
            if (animationTrackGO == null)
            {
                return new KeyValuePair<GameObject, AnimationClip>();
            }

            return new KeyValuePair<GameObject, AnimationClip>(animationTrackGO, timeLineClip.animationClip);
        }

        public static string GetFileName(Object obj)
        {
            if (obj.GetType().Name.Contains("EditorClip"))
            {
                object clip = obj.GetType().GetProperty("clip").GetValue(obj, null);
                TimelineClip timeLineClip = clip as TimelineClip;

                if (timeLineClip.displayName.Contains("@"))
                {
                    return timeLineClip.displayName;
                }
                else
                {
                    var goBound = GetGameObjectBoundToEditorClip(obj);
                    if (goBound == null)
                    {
                        return null;
                    }
                    return string.Format("{0}@{1}", goBound.name, timeLineClip.displayName);
                }
            }
            else
            {
                return obj.name;
            }
        }
    }
}