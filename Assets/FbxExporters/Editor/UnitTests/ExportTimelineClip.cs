using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using FbxExporters.Editor;
using UnityEngine.Timeline;

namespace FbxExporters.UnitTests
{
    public class ExportTimelineClip : ExporterTestBase
    {
        [SetUp]
        public void Init()
        {

        }

        [Test]
        public void RemappingTest()
        {
            /*var selectedObjects = Selection.objects;
            foreach (var obj in selectedObjects)
            {
                if (obj.GetType().Name.Contains("EditorClip"))
                {
                    var selClip = obj.GetType().GetProperty("clip").GetValue(obj, null);
					UnityEngine.Timeline.TimelineClip timeLineClip = selClip as UnityEngine.Timeline.TimelineClip;

					var selClipItem = obj.GetType().GetProperty("item").GetValue(obj, null);
					var selClipItemParentTrack = selClipItem.GetType().GetProperty("parentTrack").GetValue(selClipItem, null);
					AnimationTrack editorClipAnimationTrack = selClipItemParentTrack as AnimationTrack;

                    GameObject animationTrackGObject = UnityEditor.Timeline.TimelineEditor.playableDirector.GetGenericBinding (editorClipAnimationTrack) as GameObject;

					Debug.Log("obj name: " + obj.name + " /clip name: " + editorClipAnimationTrack.name + " /timelineAssetName: " + animationTrackGObject.name);

					string filePath = folderPath + "/" + animationTrackGObject.name + "@" + timeLineClip.animationClip.name + ".fbx";
					Debug.Log("filepath: " + filePath);
					UnityEngine.Object[] myArray = new UnityEngine.Object[] { animationTrackGObject, timeLineClip.animationClip };

					FbxExporter.ExportObjects(filePath, myArray, ExportType.timelineAnimationClip);
                } 
            }*/



            Assert.IsTrue(FbxPrefabAutoUpdater.OnValidateMenuItem());
        }


        [TearDown]
        public void stopTest()
        {

        }
    }
}
