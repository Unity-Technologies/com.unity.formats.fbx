using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using FbxExporters.Editor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System;

namespace FbxExporters.UnitTests
{
    public class ExportTimelineClipTest : ExporterTestBase
    {
        [SetUp]
        public void Init()
        {

        }

        [Test]
        public void ExportClip()
        {
            GameObject timelineGO = new GameObject();
            PlayableDirector playableDirector = timelineGO.AddComponent<PlayableDirector>();
            Animator animator = timelineGO.AddComponent<Animator>();
            TimelineAsset timelineAsset = (TimelineAsset)playableDirector.playableAsset;
            AnimationTrack newTrack = timelineAsset.CreateTrack<AnimationTrack>(null, "Animation Track " + timelineGO.name);

            //bind object to which the animation shell be assigned to the created animationTrack
            playableDirector.SetGenericBinding(newTrack, timelineGO);

            AnimationClip animationClip = new AnimationClip();
            //create a timelineClip for the animationClip on the AnimationTrack
            TimelineClip timelineClip = newTrack.CreateClip(animationClip);


            /*Type timelineWindowType = Type.GetType("UnityEditor.Timeline.TimelineWindow,UnityEditor.Timeline");
            var timelineWindow = EditorWindow.GetWindow(timelineWindowType);
            timelineWindow.Repaint();​*/


            //New GameObject
            //New Playable Director
            //New Timeline
            //New Animation Track
            //New Animation Clip
            //Create clip (timeline) with Animation Clip
            //UnityEngine.Timeline.TimelineClip timeLineClip = new UnityEngine.Timeline.TimelineClip();

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

           // Assert.IsTrue(FbxPrefabAutoUpdater.OnValidateMenuItem());
        }

        [Test]
        public void ExportAllTimelineClip()
        {
            GameObject myCube = GameObject.Find("CubeSpecial");
            Selection.objects = new UnityEngine.GameObject[] { myCube };
            string folderPath = Application.dataPath + "/UnitTest/";
            Debug.Log(folderPath);
            foreach(GameObject obj in Selection.objects)
            {
                ModelExporter.ExportAllTimelineClips(obj, folderPath);
                FileAssert.Exists(folderPath + obj.name + "@Animation Track.fbx");
            }
        }

        [TearDown]
        public void StopTest()
        {

        }
    }
}
