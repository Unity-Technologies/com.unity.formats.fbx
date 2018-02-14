using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using FbxExporters.Editor;

namespace FbxExporters.UnitTests
{
    public class RotationCurveTest : ExporterTestBase {

        private void TestRotationCurveBasics<T>() where T : RotationCurve {

        }

        [Test]
        public void TestQuaternionBasics(){

        }

    	[Test]
    	public void TestEulerBasics() {
            // Test get euler index
            var eulerCurve = new EulerCurve();
            Assert.That(EulerCurve.GetEulerIndex ("localEulerAnglesRaw.y"), Is.EqualTo(1));
            Assert.That(EulerCurve.GetEulerIndex ("localEulerAnglesRaw."), Is.EqualTo(-1));
            Assert.That(EulerCurve.GetEulerIndex ("Quaternion.x"), Is.EqualTo(-1));

            // Test get quaternion index

            // Test SetCurve


    	}
    }
}