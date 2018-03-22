using UnityEngine;
using NUnit.Framework;
using UnityEngine.Playables;
using UnityEditor.SceneManagement;

namespace FbxExporters.UnitTests
{
    public class ExportModelAnimationCustomPropertyTest : ExporterTestBase
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

            }
        }
    }
}
