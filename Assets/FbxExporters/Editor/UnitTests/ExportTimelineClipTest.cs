using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using FbxExporters.Editor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FbxExporters.UnitTests
{
    public class ExportTimelineClipTest : ExporterTestBase
    {
        [SetUp]
        public void Init()
        {
            EditorSceneManager.OpenScene("Assets/FBXExporters/Editor/UnitTests/Scene/TestScene.unity");
        }

        [Test]
        public void ExportSingleTimelineClipTest()
        {
            GameObject myCube = GameObject.Find("CubeSpecial");
            string folderPath = Application.dataPath + "/UnitTest/";

            PlayableDirector pd = myCube.GetComponent<PlayableDirector>();
            if (pd != null)
            {
                foreach (PlayableBinding output in pd.playableAsset.outputs)
                {
                    AnimationTrack at = output.sourceObject as AnimationTrack;

                    GameObject atObject = pd.GetGenericBinding(output.sourceObject) as GameObject;
			        // One file by animation clip
			        foreach(TimelineClip timeLineClip in at.GetClips())
			        {
                        ModelExporter.ExportSingleTimelineClip(timeLineClip, folderPath, atObject);
                        FileAssert.Exists(folderPath + atObject.name + "@Recorded.fbx");
			        }
                }
            }
        }

        [Test]
        public void ExportAllTimelineClipTest()
        {
            GameObject myCube = GameObject.Find("CubeSpecial");
            Selection.objects = new UnityEngine.GameObject[] { myCube };
            string folderPath = Application.dataPath + "/UnitTest/";
            Debug.Log(folderPath);
            foreach(GameObject obj in Selection.objects)
            {
                ModelExporter.ExportAllTimelineClips(obj, folderPath);
                FileAssert.Exists(folderPath + obj.name + "@Recorded.fbx");
            }
        }

        [TearDown]
        public void StopTest()
        {

        }
    }
}
