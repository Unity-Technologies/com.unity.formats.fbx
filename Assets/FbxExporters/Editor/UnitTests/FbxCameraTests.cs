// NOTE: uncomment the next line to leave temporary FBX files on disk
// and create a imported object in the scene.
//#define DEBUG_UNITTEST

using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;

namespace FbxExporters.UnitTests
{
    public class FbxCameraTest : ExporterTestBase
    {
        [TearDown]
        public override void Term ()
        {
            #if (!DEBUG_UNITTEST)
            base.Term ();
            #endif
        }

        [Test]
        public void AnimationWithCameraFOVTest()
        {
            var keyData = new FbxAnimationTest.PropertyKeyData
            {
                componentType=typeof(Camera), 
                propertyName="field of view", 
                keyTimes=new float[]{0f,1f,2f}, 
                keyFloatValues=new float[]{1f,2f,3f}
            };
            var tester = new FbxAnimationTest.AnimTester{keyData=keyData, testName="CameraFOV", path=GetRandomFbxFilePath()};

            tester.DoIt();
        }
    }
}