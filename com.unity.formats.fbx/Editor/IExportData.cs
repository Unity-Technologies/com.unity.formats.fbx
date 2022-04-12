﻿using UnityEngine;
using UnityEngine.Timeline;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace UnityEditor.Formats.Fbx.Exporter
{
    /// <summary>
    /// Export data containing extra information required to export
    /// </summary>
    internal interface IExportData
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
                else if ((uniCurveBinding.type == typeof(SkinnedMeshRenderer)) && unityGo.GetComponent<SkinnedMeshRenderer>())
                {
                    // only export mesh if there are animation keys for it (e.g. for blendshapes)
                    if (FbxPropertyChannelPair.TryGetValue(uniCurveBinding.propertyName, out FbxPropertyChannelPair[] channelPairs))
                    {
                        this.exportComponent[unityGo] = typeof(SkinnedMeshRenderer);
                    }
                }

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
        /// Get the GameObject that the clip is bound to in the timeline.
        /// </summary>
        /// <param name="timelineClip"></param>
        /// <returns>The GameObject bound to the timeline clip or null if none.</returns>
        private static GameObject GetGameObjectBoundToTimelineClip(TimelineClip timelineClip, PlayableDirector director = null)
        {
            object parentTrack = timelineClip.GetParentTrack();
            AnimationTrack animTrack = parentTrack as AnimationTrack;

            var inspectedDirector = director? director : UnityEditor.Timeline.TimelineEditor.inspectedDirector;
            if (!inspectedDirector)
            {
                Debug.LogWarning("No Timeline selected in inspector, cannot retrieve GameObject bound to track");
                return null;
            }

            Object animationTrackObject = inspectedDirector.GetGenericBinding(animTrack);

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
        /// Get the GameObject and it's corresponding animation clip from the given timeline clip.
        /// </summary>
        /// <param name="timelineClip"></param>
        /// <returns>KeyValuePair containing GameObject and corresponding AnimationClip</returns>
        public static KeyValuePair<GameObject, AnimationClip> GetGameObjectAndAnimationClip(TimelineClip timelineClip, PlayableDirector director = null)
        {
            var animationTrackGO = GetGameObjectBoundToTimelineClip(timelineClip, director);
            if (!animationTrackGO)
            {
                return new KeyValuePair<GameObject, AnimationClip>();
            }

            return new KeyValuePair<GameObject, AnimationClip>(animationTrackGO, timelineClip.animationClip);
        }

        /// <summary>
        /// Get the filename of the format {model}@{anim}.fbx from the given timeline clip
        /// </summary>
        /// <param name="timelineClip"></param>
        /// <returns>filename for use for exporting animation clip</returns>
        public static string GetFileName(TimelineClip timelineClip)
        {
            // if the timeline clip name already contains an @, then take this as the
            // filename to avoid duplicate @
            if (timelineClip.displayName.Contains("@"))
            {
                return timelineClip.displayName;
            }

            var goBound = GetGameObjectBoundToTimelineClip(timelineClip);
            if (goBound == null)
            {
                return timelineClip.displayName;
            }
            return string.Format("{0}@{1}", goBound.name, timelineClip.displayName);
        }
    }
}