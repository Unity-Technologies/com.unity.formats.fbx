using UnityEngine;
using NUnit.Framework;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor.SceneManagement;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests
{
    public class ExportTimelineClipTest : ExporterTestBase
    {
        [SetUp]
        public override void Init()
        {
            base.Init ();
            EditorSceneManager.OpenScene(FindPathInUnitTests("Scene/TestScene.unity"));
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
                        var filePath = string.Format ("{0}/{1}@{2}", folderPath, atObject.name, "Recorded.fbx");
                        ModelExporterReflection.ExportSingleTimelineClip (timeLineClip, atObject, filePath);
                        FileAssert.Exists (filePath);
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
                ModelExporterReflection.ExportAllTimelineClips(obj, folderPath);
                FileAssert.Exists(string.Format("{0}/{1}@{2}", folderPath, obj.name, "Recorded.fbx"));
            }
        }
    }
}
