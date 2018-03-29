using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using FbxExporters.Editor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

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
            string filePath = null;
            var exportData = new Dictionary<GameObject, ModelExporter.IExportData>();

            PlayableDirector pd = myCube.GetComponent<PlayableDirector> ();
            if (pd != null) {
                foreach (PlayableBinding output in pd.playableAsset.outputs) {
                    AnimationTrack at = output.sourceObject as AnimationTrack;

                    GameObject atObject = pd.GetGenericBinding (output.sourceObject) as GameObject;
                    Assert.That (atObject, Is.Not.Null);

                    // One file by animation clip
                    foreach (TimelineClip timeLineClip in at.GetClips()) {
                        Assert.That (timeLineClip.animationClip, Is.Not.Null);

                        filePath = string.Format ("{0}/{1}@{2}", folderPath, atObject.name, "Recorded.fbx");
                        exportData[atObject] = ModelExporter.GetExportData(atObject, timeLineClip.animationClip);
                        break;
                    }
                }
            }
            Assert.That (filePath, Is.Not.Null);
            Assert.That (exportData, Is.Not.Null);
            ModelExporter.ExportObjects(filePath, new Object[1]{myCube}, null, exportData);
            FileAssert.Exists (filePath);
        }
    }
}
