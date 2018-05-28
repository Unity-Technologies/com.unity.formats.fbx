﻿using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using System.Collections.Generic;

namespace UnityEditor.Formats.Fbx.Exporter
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
    internal class AnimationOnlyExportData : IExportData
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

        public AnimationOnlyExportData()
        {
            this.animationClips = new Dictionary<AnimationClip, GameObject>();
            this.goExportSet = new HashSet<GameObject>();
            this.exportComponent = new Dictionary<GameObject, System.Type>();
            this.defaultClip = null;
        }

        /// <summary>
        /// collect all object dependencies for given animation clip
        /// </summary>
        public void CollectDependencies(
            AnimationClip animClip,
            GameObject rootObject,
            IExportOptions exportOptions
        )
        {
            Debug.Assert(rootObject != null);
            Debug.Assert(exportOptions != null);

            if (this.animationClips.ContainsKey(animClip))
            {
                // we have already exported gameobjects for this clip
                return;
            }

            // NOTE: the object (animationRootObject) containing the animation is not necessarily animated
            // when driven by an animator or animation component.
            this.animationClips.Add(animClip, rootObject);

            foreach (EditorCurveBinding uniCurveBinding in AnimationUtility.GetCurveBindings(animClip))
            {
                Object uniObj = AnimationUtility.GetAnimatedObject(rootObject, uniCurveBinding);
                if (!uniObj)
                {
                    return;
                }

                GameObject unityGo = ModelExporter.GetGameObject(uniObj);
                if (!unityGo)
                {
                    return;
                }

                if (!exportOptions.AnimateSkinnedMesh && unityGo.GetComponent<SkinnedMeshRenderer>())
                {
                    return;
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
            
            foreach (var animClip in animClips)
            {
                CollectDependencies(animClip, rootObject, exportOptions);
            }
        }

        /// <summary>
        /// Get the property propertyName from object obj using reflection.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <returns>property propertyName as an object</returns>
        private static object GetPropertyReflection(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName).GetValue(obj, null);
        }

        /// <summary>
        /// Get the timeline clip from the given editor clip using reflection.
        /// </summary>
        /// <param name="editorClip"></param>
        /// <returns>the timeline clip or null if none</returns>
        private static TimelineClip GetTimelineClipFromEditorClip(object editorClip)
        {
            object clip = GetPropertyReflection(editorClip, "clip");
            return clip as TimelineClip;
        }

        /// <summary>
        /// Get the GameObject that the editor clip is bound to in the timeline using reflection.
        /// </summary>
        /// <param name="editorClip"></param>
        /// <returns>The GameObject bound to the editor clip or null if none.</returns>
        private static GameObject GetGameObjectBoundToEditorClip(object editorClip)
        {
            object clipItem = GetPropertyReflection(editorClip, "item");
            object parentTrack = GetPropertyReflection(clipItem, "parentTrack");
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

        /// <summary>
        /// Get the GameObject and it's corresponding animation clip from the given editor clip.
        /// </summary>
        /// <param name="editorClip"></param>
        /// <returns>KeyValuePair containing GameObject and corresponding AnimationClip</returns>
        public static KeyValuePair<GameObject, AnimationClip> GetGameObjectAndAnimationClip(Object editorClip)
        {
            if (!ModelExporter.IsEditorClip(editorClip))
                return new KeyValuePair<GameObject, AnimationClip>();
            
            TimelineClip timeLineClip = GetTimelineClipFromEditorClip(editorClip);

            var animationTrackGO = GetGameObjectBoundToEditorClip(editorClip);
            if (animationTrackGO == null)
            {
                return new KeyValuePair<GameObject, AnimationClip>();
            }

            return new KeyValuePair<GameObject, AnimationClip>(animationTrackGO, timeLineClip.animationClip);
        }

        /// <summary>
        /// Get the filename of the format {model}@{anim}.fbx from the given object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>filename for use for exporting animation clip</returns>
        public static string GetFileName(Object obj)
        {
            if (!ModelExporter.IsEditorClip(obj))
            {
                // not an editor clip so just return the name of the object
                return obj.name;
            }

            TimelineClip timeLineClip = GetTimelineClipFromEditorClip(obj);

            // if the timeline clip name already contains an @, then take this as the
            // filename to avoid duplicate @
            if (timeLineClip.displayName.Contains("@"))
            {
                return timeLineClip.displayName;
            }

            var goBound = GetGameObjectBoundToEditorClip(obj);
            if (goBound == null)
            {
                return obj.name;
            }
            return string.Format("{0}@{1}", goBound.name, timeLineClip.displayName);
        }
    }
}