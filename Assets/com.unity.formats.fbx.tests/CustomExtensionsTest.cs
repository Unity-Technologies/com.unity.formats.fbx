// NOTE: uncomment the next line to leave temporary FBX files on disk
// and create a imported object in the scene.
//#define DEBUG_UNITTEST

using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using FbxExporters.CustomExtensions;
using UnityEngine.Formats.FbxSdk;

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
        public void Vector3ExtensionTest()
        {
            var v3 = new Vector3(1f,2f,3f);
            Assert.That(v3.RightHanded(), Is.EqualTo(new Vector3(-1f,2f,3f)));
            Assert.That(v3.FbxVector4(), Is.EqualTo(new FbxVector4(1f,2f,3f)));
        }

        [Test]
        public void AnimationCurveExtensionTest()
        {
            var ac = new AnimationCurve();
            ac.Dump();
            ac.Dump("hello world");
            ac.Dump(keyTimesExpected:new float[]{});
            ac.Dump(keyValuesExpected:new float[]{});
            ac.Dump(keyTimesExpected:new float[]{}, keyValuesExpected:new float[]{});
        }

        [Test]
        public void FloatExtensionTest()
        {
            Assert.That(Value1.Meters().ToCentimeters(), Is.EqualTo(100f));

            Assert.That(1f.Meters(), Is.EqualTo(100f.Centimeters()));
            Assert.That(10f.Millimeters().ToMeters(), Is.EqualTo(1f.Centimeters().ToMeters()).Within(0.00001f));
            Assert.That(1f.Centimeters().ToMeters(), Is.EqualTo(10f.Millimeters().ToMeters()).Within(0.00001f));
            Assert.That(0.0254f.Meters(), Is.EqualTo(1f.Inches().ToMetric()));
            Assert.That(1f.Meters().ToImperial(), Is.EqualTo(39.3701f.Inches()));
        }

        [Test]
        public void ImperialDistanceTest()
        {
            var one_inch = new ImperialDistance(1f); // 1 inch
            var one_foot = new ImperialDistance(12f); // 12 inches

            Assert.That(ImperialDistance.Inch, Is.Not.EqualTo(new MetricDistance(1f)));
            Assert.That(ImperialDistance.Inch, Is.EqualTo(new ImperialDistance(1f)));
            Assert.That(ImperialDistance.Foot, Is.EqualTo(new ImperialDistance(12f)));

            Assert.That(one_inch, Is.EqualTo(ImperialDistance.Inch));
            Assert.That(one_inch.ToInches(), Is.EqualTo(ImperialDistance.Inch.ToInches()));
            Assert.That(one_inch.ToMetric().ToMeters(), Is.EqualTo(0.0254f));
            Assert.That(one_inch.ToMeters(), Is.EqualTo(0.0254f));
            Assert.That(one_inch.ToInches(), Is.EqualTo(1f));
            Assert.That(one_inch.GetHashCode(), Is.GreaterThan(0));
            Assert.That(one_inch ==  ImperialDistance.Inch, Is.True);
            Assert.That(one_inch ==  ImperialDistance.Inch, Is.True);

            Assert.That(one_foot, Is.EqualTo(ImperialDistance.Foot));
            Assert.That(one_foot!=one_inch, Is.True);

            Assert.That(one_inch+one_inch, Is.EqualTo(new ImperialDistance(2f)));
            Assert.That(one_inch-one_inch, Is.EqualTo(new ImperialDistance(0f)));
            Assert.That(new ImperialDistance(2f)*new ImperialDistance(6f), Is.EqualTo(ImperialDistance.Foot));
            Assert.That(ImperialDistance.Foot / ImperialDistance.Inch, Is.EqualTo(ImperialDistance.Foot));
        }

        [Test]
        public void MetricDistanceTest()
        {
            var one_cm = new MetricDistance(0.01f); // 1 cm
            var one_m = new MetricDistance(1f); // 100 cm

            Assert.That(MetricDistance.Millimeter, Is.EqualTo(new MetricDistance(0.001f)));
            Assert.That(MetricDistance.Centimeter, Is.EqualTo(new MetricDistance(0.01f)));
            Assert.That(MetricDistance.Meter, Is.EqualTo(new MetricDistance(1f)));

            Assert.That(one_m, Is.EqualTo(MetricDistance.Meter));
            Assert.That(one_m.ToMeters(), Is.EqualTo(MetricDistance.Meter.ToMeters()));
            Assert.That(one_m.ToImperial().ToInches(), Is.EqualTo(39.3701f));
            Assert.That(one_m.ToMeters(), Is.EqualTo(1f));
            Assert.That(one_m.GetHashCode(), Is.GreaterThan(0));
            Assert.That(one_m ==  MetricDistance.Meter, Is.True);
            Assert.That(one_cm ==  MetricDistance.Centimeter, Is.True);

            Assert.That(one_m, Is.EqualTo(MetricDistance.Meter));
            Assert.That(one_m!=one_cm, Is.True);

            Assert.That(one_m+one_cm, Is.EqualTo(new MetricDistance(1.01f)));
            Assert.That(one_m-one_cm, Is.EqualTo(new MetricDistance(0.99f)));
            Assert.That(MetricDistance.Centimeter*MetricDistance.Meter, Is.EqualTo(new MetricDistance(0.01f)));
            Assert.That(MetricDistance.Meter / MetricDistance.Centimeter, Is.EqualTo(new MetricDistance(100f)));
        }
    }
}