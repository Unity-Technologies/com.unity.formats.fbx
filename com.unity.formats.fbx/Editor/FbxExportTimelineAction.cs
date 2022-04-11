using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Timeline.Actions;
using UnityEngine.Timeline;
using System.Linq;

namespace UnityEditor.Formats.Fbx.Exporter
{
    [MenuEntry("Export Clip To FBX..", MenuPriority.CustomClipActionSection.resetOffset), UsedImplicitly]
    class FbxExportTimelineAction : ClipAction
    {
        public override bool Execute(IEnumerable<TimelineClip> clips)
        {
            ModelExporter.ExportSingleTimelineClip(clips.First());
            return true;
        }

        public override ActionValidity Validate(IEnumerable<TimelineClip> clips)
        {
            //if (!clips.All(TimelineAnimationUtilities.IsAnimationClip))
            //    return ActionValidity.NotApplicable;

            if(clips.Count() != 1)
            {
                return ActionValidity.NotApplicable;
            }

            // has to be an animation clip
            if(clips.Any((clip) => { return clip.animationClip == null; }))
            {
                return ActionValidity.NotApplicable;
            }

            return ActionValidity.Valid;
        }
    }
}