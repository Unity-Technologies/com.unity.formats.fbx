using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Timeline.Actions;
using UnityEngine.Timeline;
using System.Linq;
using UnityEngine.Playables;
using UnityEditor.Timeline;

namespace UnityEditor.Formats.Fbx.Exporter
{
    [MenuEntry("Export Clip To FBX...", MenuPriority.CustomClipActionSection.start + MenuPriority.separatorAt), UsedImplicitly]
    class FbxExportTimelineAction : ClipAction
    {
        public override bool Execute(IEnumerable<TimelineClip> clips)
        {
            PlayableDirector director = TimelineEditor.inspectedDirector;
            ModelExporter.ExportSingleTimelineClip(clips.First(), director);
            return true;
        }

        public override ActionValidity Validate(IEnumerable<TimelineClip> clips)
        {
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