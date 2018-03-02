using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using FbxExporters.Editor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor.SceneManagement;

namespace FbxExporters.UnitTests
{
    public class ExportTimelineClipTest : ExporterTestBase
    {
        private static string m_testScenePath = "Scene/TestScene.unity";

        [SetUp]
        public override void Init()
        {
            base.Init ();
            string testScenePath = FindPathInUnitTests (m_testScenePath);
            Assert.That (testScenePath, Is.Not.Null);
            EditorSceneManager.OpenScene("Assets/" + testScenePath);
        }

        [Test]
        public void ExportSingleTimelineClipTest()
        {
            GameObject myCube = GameObject.Find("CubeSpecial");
            string folderPath = GetRandomFileNamePath(extName: "");

            PlayableDirector pd = myCube.GetComponent<PlayableDirector> ();
            if (pd != null) {
                foreach (PlayableBinding output in pd.playableAsset.outputs) {
                    AnimationTrack at = output.sourceObject as AnimationTrack;

                    GameObject atObject = pd.GetGenericBinding (output.sourceObject) as GameObject;
                    // One file by animation clip
                    foreach (TimelineClip timeLineClip in at.GetClips()) {
                        ModelExporter.ExportSingleTimelineClip (timeLineClip, folderPath, atObject);
                        FileAssert.Exists (string.Format("{0}/{1}@{2}", folderPath, atObject.name, "Recorded.fbx"));
                    }
                }
            }
        }

        [Test]
        public void ExportAllTimelineClipTest()
        {
            GameObject myCube = GameObject.Find("CubeSpecial");
            Selection.objects = new UnityEngine.GameObject[] { myCube };
            string folderPath = GetRandomFileNamePath(extName: "");

            foreach(GameObject obj in Selection.objects)
            {
                ModelExporter.ExportAllTimelineClips(obj, folderPath);
                FileAssert.Exists(string.Format("{0}/{1}@{2}", folderPath, obj.name, "Recorded.fbx"));
            }
        }
    }
}
