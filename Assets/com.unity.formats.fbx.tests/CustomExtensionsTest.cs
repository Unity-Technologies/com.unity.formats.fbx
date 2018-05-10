// NOTE: uncomment the next line to leave temporary FBX files on disk
// and create a imported object in the scene.
//#define DEBUG_UNITTEST

using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using FbxExporters.CustomExtensions;

namespace FbxExporters.UnitTests
{
    public class CustomExtensionsTest : ExporterTestBase
    {
        private float Value1 { get { return 1f; } }

        [TearDown]
        public override void Term ()
        {
            #if (!DEBUG_UNITTEST)
            base.Term ();
            #endif
        }

        [Test]
        public void MetricDistanceTest()
        {
            Assert.That(Value1.Meters().ToCentimeters(), Is.EqualTo(100f));
                
            Assert.That(1f.Meters(), Is.EqualTo(100f.Centimeters()));
            Assert.That(10f.Millimeters().ToMeters(), Is.EqualTo(1f.Centimeters().ToMeters()).Within(0.00001f));
            Assert.That(1f.Centimeters().ToMeters(), Is.EqualTo(10f.Millimeters().ToMeters()).Within(0.00001f));
            Assert.That(0.0254f.Meters(), Is.EqualTo(1f.Inches().ToMetric()));
            Assert.That(1f.Meters().ToImperial(), Is.EqualTo(39.3701f.Inches()));
        }
    }
}