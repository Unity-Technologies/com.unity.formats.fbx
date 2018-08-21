using UnityEngine;
using NUnit.Framework;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Reflection;

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
            string filePath = null;
            var exportData = new Dictionary<GameObject, IExportData>();

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

            // This version of ExportObjects is private. Use reflection
            // ModelExporter.ExportObjects(filePath, new Object[1]{myCube}, null, exportData);
            var internalMethod = typeof(ModelExporter).GetMethod(   "ExportObjects",    
                                                                    BindingFlags.Static | BindingFlags.NonPublic,
                                                                    null,
                                                                    new Type[] { typeof(string), typeof(UnityEngine.Object[]), typeof(IExportOptions), typeof(Dictionary<GameObject, IExportData>) },
                                                                    null);
            internalMethod.Invoke(null, new object[] { filePath, new UnityEngine.Object[1]{myCube}, null, exportData });            
            FileAssert.Exists (filePath);
        }
    }
}
