using UnityEngine;
using NUnit.Framework;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Formats.Fbx.Exporter;

namespace FbxExporter.UnitTests
{
    public class ExportTimelineClipTest : ExporterTestBase
    {
        [Test]
        public void ExportSingleTimelineClipFromExportDataTest()
        {
            string cubeSpecialPath = FindPathInUnitTests("Scene/CubeSpecial.prefab");

            GameObject myCube = AddAssetToScene(cubeSpecialPath);
            string folderPath = GetRandomFileNamePath(extName: "");
            string filePath = null;
            var exportData = new Dictionary<GameObject, IExportData>();

            PlayableDirector pd = myCube.GetComponent<PlayableDirector>();
            if (pd != null)
            {
                foreach (PlayableBinding output in pd.playableAsset.outputs)
                {
                    AnimationTrack at = output.sourceObject as AnimationTrack;

                    var atComponent = pd.GetGenericBinding(at) as Component;
                    Assert.That(atComponent, Is.Not.Null);

                    var atObject = atComponent.gameObject;

                    // One file by animation clip
                    foreach (TimelineClip timeLineClip in at.GetClips())
                    {
                        Assert.That(timeLineClip.animationClip, Is.Not.Null);

                        filePath = $"{folderPath}/{atObject.name}@Recorded.fbx";
                        exportData[atObject] = ModelExporter.GetExportData(atObject, timeLineClip.animationClip);
                        break;
                    }
                }
            }
            Assert.That(filePath, Is.Not.Null);
            Assert.That(exportData, Is.Not.Null);
            ModelExporter.ExportObjects(filePath, new UnityEngine.Object[1] { myCube }, null, exportData);
            FileAssert.Exists(filePath);
        }

        [Test]
        public void ExportSingleTimelineClipTest()
        {
            string cubeSpecialPath = FindPathInUnitTests("Scene/CubeSpecial.prefab");

            GameObject myCube = AddAssetToScene(cubeSpecialPath);
            string folderPath = GetRandomFileNamePath(extName: "");
            string filePath = null;
            TimelineClip timelineClipToExport = null;

            UnityEditor.Selection.activeObject = myCube;

            PlayableDirector pd = myCube.GetComponent<PlayableDirector>();
            if (pd != null)
            {
                foreach (PlayableBinding output in pd.playableAsset.outputs)
                {
                    AnimationTrack at = output.sourceObject as AnimationTrack;

                    var atComponent = pd.GetGenericBinding(at) as Component;
                    Assert.That(atComponent, Is.Not.Null);

                    var atObject = atComponent.gameObject;

                    // One file by animation clip
                    foreach (TimelineClip timeLineClip in at.GetClips())
                    {
                        Assert.That(timeLineClip.animationClip, Is.Not.Null);

                        filePath = $"{folderPath}/{atObject.name}@Recorded.fbx";
                        timelineClipToExport = timeLineClip;
                        break;
                    }
                }
            }
            Assert.That(filePath, Is.Not.Null);

            var exportOptions = new ExportModelSettingsSerialize();
            exportOptions.SetModelAnimIncludeOption(ExportSettings.Include.Anim);

            ModelExporter.ExportTimelineClip(filePath, timelineClipToExport, pd, exportOptions);
            FileAssert.Exists(filePath);
        }
    }
}
