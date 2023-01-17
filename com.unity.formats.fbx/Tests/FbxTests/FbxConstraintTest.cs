using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations;

namespace FbxExporter.UnitTests
{
    public class ConstraintAnimationTestDataClass
    {
        public static IEnumerable TestCases
        {
            get
            {
                /* Test Weight */
                yield return new TestCaseData(typeof(RotationConstraint), new float[] { 1f, 20f, 30f }, new float[] { 0f, 0.5f, 1f }, "m_Weight").Returns(1);
                yield return new TestCaseData(typeof(RotationConstraint), new float[] { 2, 9, 33 }, new float[] { 0.1f, 0.67f, 0.2f }, "m_Sources.Array.data[0].weight").Returns(1);

                /* Test Rotation */
                yield return new TestCaseData(typeof(RotationConstraint), new float[] { 1, 0.3f, 10f }, new float[] { -4, 39, 45 }, "m_RotationOffset.x").Returns(1);
                yield return new TestCaseData(typeof(RotationConstraint), new float[] { 1, 4f, 3f }, new float[] { -4, -39, 145 }, "m_RotationOffset.y").Returns(1);
                yield return new TestCaseData(typeof(RotationConstraint), new float[] { 1.2f, 0.8f, 10.3f }, new float[] { 194, 9, 195 }, "m_RotationOffset.z").Returns(1);

                /* Test Aim */
                yield return new TestCaseData(typeof(AimConstraint), new float[] { 1f, 30f, 10f }, new float[] { 10f, 180f, 10f }, "m_AimVector.x").Returns(1)
                    .Ignore("UT-3734 (fogbug 1205373): Test fails due to constraint import issues.");
                yield return new TestCaseData(typeof(AimConstraint), new float[] { 1f, 30f, 10f }, new float[] { 90f, 45f, 60f }, "m_AimVector.y").Returns(1);
                yield return new TestCaseData(typeof(AimConstraint), new float[] { 1f, 30f, 10f }, new float[] { 10f, 180f, 10f }, "m_AimVector.z").Returns(1);
                yield return new TestCaseData(typeof(AimConstraint), new float[] { 1, 0.3f, 10f }, new float[] { -4, 39, 45 }, "m_RotationOffset.x").Returns(1);
                yield return new TestCaseData(typeof(AimConstraint), new float[] { 1, 4f, 3f }, new float[] { -4, -39, 145 }, "m_RotationOffset.y").Returns(1);
                yield return new TestCaseData(typeof(AimConstraint), new float[] { 1.2f, 0.8f, 10.3f }, new float[] { 194, 9, 195 }, "m_RotationOffset.z").Returns(1);

                yield return new TestCaseData(typeof(AimConstraint), new float[] { 1f, 2f, 4f }, new float[] { 100f, 80f, 19f }, "m_UpVector.x").Returns(1)
                    .Ignore("UT-3734 (fogbug 1205373): Test fails due to constraint import issues.");;
                yield return new TestCaseData(typeof(AimConstraint), new float[] { 1f, 2f, 4f }, new float[] { 4f, 154f, 454f }, "m_UpVector.y").Returns(1);
                yield return new TestCaseData(typeof(AimConstraint), new float[] { 1f, 2f, 4f }, new float[] { 8f, 14f, 6f }, "m_UpVector.z").Returns(1);

                yield return new TestCaseData(typeof(AimConstraint), new float[] { 2f, 30f, 77f }, new float[] { 29.3f, 322f, -190f }, "m_WorldUpVector.x").Returns(1)
                    .Ignore("UT-3734 (fogbug 1205373): Test fails due to constraint import issues.");
                yield return new TestCaseData(typeof(AimConstraint), new float[] { 2f, 30f, 77f }, new float[] { 145f, 180f, 40f }, "m_WorldUpVector.y").Returns(1);
                yield return new TestCaseData(typeof(AimConstraint), new float[] { 2f, 30f, 77f }, new float[] { 10f, 180f, 10f }, "m_WorldUpVector.z").Returns(1);

                /* Test Translation Offset */
                yield return new TestCaseData(typeof(PositionConstraint), new float[] { 1f, 2f, 3f }, new float[] { 10f, 180f, 10f }, "m_TranslationOffset.x").Returns(1);
                yield return new TestCaseData(typeof(PositionConstraint), new float[] { 4f, 5f, 6f }, new float[] { 1f, -18f, 50f }, "m_TranslationOffset.y").Returns(1);
                yield return new TestCaseData(typeof(PositionConstraint), new float[] { 1f, 2f, 3f }, new float[] { 12f, 23f, 4f }, "m_TranslationOffset.z").Returns(1);

                /* Test Scale Offset */
                yield return new TestCaseData(typeof(ScaleConstraint), new float[] { 1f, 2f, 3f }, new float[] { 10f, 180f, 10f }, "m_ScaleOffset.x").Returns(1);
                yield return new TestCaseData(typeof(ScaleConstraint), new float[] { 4f, 5f, 6f }, new float[] { 1f, 18f, 50f }, "m_ScaleOffset.y").Returns(1);
                yield return new TestCaseData(typeof(ScaleConstraint), new float[] { 1f, 2f, 3f }, new float[] { 2f, 23f, 4f }, "m_ScaleOffset.z").Returns(1);

                /* Test Parent */
                yield return new TestCaseData(typeof(ParentConstraint), new float[] { 1, 3, 35 }, new float[] { 45f, 34.7f, 56f }, "m_TranslationOffsets.Array.data[0].x").Returns(1);
                yield return new TestCaseData(typeof(ParentConstraint), new float[] { 4, 7, 75 }, new float[] { 3f, 65.67f, -34f }, "m_TranslationOffsets.Array.data[0].y").Returns(1);
                yield return new TestCaseData(typeof(ParentConstraint), new float[] { 3, 4, 32 }, new float[] { -223f, 56.7f, 0.2f }, "m_TranslationOffsets.Array.data[0].z").Returns(1);

                yield return new TestCaseData(typeof(ParentConstraint), new float[] { 1, 3, 35 }, new float[] { 45f, 34.7f, -56f }, "m_RotationOffsets.Array.data[0].x").Returns(1);
                yield return new TestCaseData(typeof(ParentConstraint), new float[] { 4, 7, 75 }, new float[] { 3f, 65.67f, 180f }, "m_RotationOffsets.Array.data[0].y").Returns(1);
                yield return new TestCaseData(typeof(ParentConstraint), new float[] { 3, 4, 32 }, new float[] { -223f, 56.7f, 0.2f }, "m_RotationOffsets.Array.data[0].z").Returns(1);
            }
        }
    }

    public class FbxConstraintTest : ExporterTestBase
    {
        /// <summary>
        /// Create and begin setting up constraint of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toExport"></param>
        /// <returns></returns>
        protected T CreateConstraint<T>(out List<Object> toExport) where T : Component, IConstraint
        {
            // setup constrained object and sources
            var constrainedGO = new GameObject("constrained");
            var sourceGO = new GameObject("source");

            toExport = new List<Object>();
            toExport.Add(constrainedGO);
            toExport.Add(sourceGO);

            sourceGO.transform.localPosition = new Vector3(1, 2, 3);
            sourceGO.transform.localRotation = Quaternion.Euler(20, 45, 90);
            sourceGO.transform.localScale = new Vector3(1, 1.5f, 2);

            var uniConstraint = SetupConstraintWithSources<T>(constrainedGO, new List<GameObject>() { sourceGO });

            uniConstraint.constraintActive = true;
            uniConstraint.locked = false;
            uniConstraint.weight = 0.75f;

            return uniConstraint;
        }

        /// <summary>
        /// Export constraint of type T and check some of its properties on import.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uniConstraint"></param>
        /// <param name="toExport"></param>
        /// <returns>The imported constraint</returns>
        protected T ExportAndCheckConstraint<T>(T uniConstraint, Object[] toExport) where T : Component, IConstraint
        {
            // export and compare
            var exportedGO = ExportConstraints(toExport);

            // get exported constraint
            var expConstraint = exportedGO.GetComponentInChildren<T>();
            Assert.That(expConstraint, Is.Not.Null);

            TestSourcesMatch(uniConstraint, expConstraint);

            Assert.That(expConstraint.constraintActive, Is.EqualTo(uniConstraint.constraintActive));
            Assert.That(expConstraint.locked, Is.EqualTo(true)); // locked always imports as true
            Assert.That(expConstraint.weight, Is.EqualTo(uniConstraint.weight).Within(0.0001f));

            return expConstraint;
        }

        [Test]
        public void TestPositionConstraintExport()
        {
            List<Object> toExport;
            var posConstraint = CreateConstraint<PositionConstraint>(out toExport);

            // setup position specific properties
            posConstraint.translationAtRest = new Vector3(0.5f, 0.5f, 0.5f);
            posConstraint.translationAxis = Axis.X | Axis.Z;
            posConstraint.translationOffset = new Vector3(0.25f, 0.5f, 0.75f);

            var expConstraint = ExportAndCheckConstraint(posConstraint, toExport.ToArray());

            Assert.That(expConstraint.translationAtRest, Is.EqualTo(posConstraint.translationAtRest));
            Assert.That(expConstraint.translationAxis, Is.EqualTo(posConstraint.translationAxis));
            Assert.That(expConstraint.translationOffset, Is.EqualTo(posConstraint.translationOffset));
        }

        [Test]
        public void TestRotationConstraint()
        {
            List<Object> toExport;
            var rotConstraint = CreateConstraint<RotationConstraint>(out toExport);

            // setup rotation specific properties
            rotConstraint.rotationAtRest = new Vector3(30, 20, 10);
            rotConstraint.rotationOffset = new Vector3(45, 60, 180);
            rotConstraint.rotationAxis = Axis.Y;

            var expConstraint = ExportAndCheckConstraint(rotConstraint, toExport.ToArray());

            Assert.That(AreRotationEqual(expConstraint.rotationAtRest, rotConstraint.rotationAtRest, 0.001), Is.True);
            Assert.That(expConstraint.rotationAxis, Is.EqualTo(rotConstraint.rotationAxis));
            Assert.That(AreRotationEqual(expConstraint.rotationOffset, rotConstraint.rotationOffset, 0.001), Is.True);
        }

        [Test]
        public void TestScaleConstraint()
        {
            // setup constrained object and sources
            List<Object> toExport;
            var scaleConstraint = CreateConstraint<ScaleConstraint>(out toExport);

            scaleConstraint.scaleAtRest = new Vector3(2, 2, 2);
            scaleConstraint.scaleOffset = new Vector3(1, 0.3f, 0.7f);
            scaleConstraint.scalingAxis = Axis.X | Axis.Y | Axis.Z;

            // export and compare
            var expConstraint = ExportAndCheckConstraint(scaleConstraint, toExport.ToArray());

            Assert.That(expConstraint.scalingAxis, Is.EqualTo(scaleConstraint.scalingAxis));
            Assert.That(expConstraint.scaleAtRest, Is.EqualTo(scaleConstraint.scaleAtRest));
            Assert.That(expConstraint.scaleOffset, Is.EqualTo(scaleConstraint.scaleOffset));
        }

        [Test]
        [Ignore("UT-3734 (fogbug 1205373): Test fails due to constraint import issues.")]
        public void TestParentConstraintExport()
        {
            List<Object> toExport;
            var parentConstraint = CreateConstraint<ParentConstraint>(out toExport);

            parentConstraint.rotationAtRest = new Vector3(50, 120, 34);
            parentConstraint.rotationAxis = Axis.None;
            parentConstraint.translationAtRest = new Vector3(5, 6, 7);
            parentConstraint.translationAxis = Axis.X;

            parentConstraint.SetTranslationOffset(0, new Vector3(10, 2.4f, 3));
            parentConstraint.SetRotationOffset(0, new Vector3(80, 200, 19));

            // export and compare
            var expConstraint = ExportAndCheckConstraint(parentConstraint, toExport.ToArray());

            Assert.That(AreRotationEqual(expConstraint.rotationAtRest, parentConstraint.rotationAtRest, 0.001), Is.True);
            Assert.That(expConstraint.rotationAxis, Is.EqualTo(parentConstraint.rotationAxis));
            Assert.That(expConstraint.translationAtRest, Is.EqualTo(parentConstraint.translationAtRest));
            Assert.That(expConstraint.translationAxis, Is.EqualTo(parentConstraint.translationAxis));
            Assert.That(AreRotationEqual(expConstraint.GetRotationOffset(0), parentConstraint.GetRotationOffset(0)), Is.True);
            Assert.That(expConstraint.GetTranslationOffset(0), Is.EqualTo(parentConstraint.GetTranslationOffset(0)));
        }

        [Test]
        [Ignore("UT-3734 (fogbug 1205373): Test fails due to constraint import issues.")]
        public void TestAimConstraintExport()
        {
            List<Object> toExport;
            var aimConstraint = CreateConstraint<AimConstraint>(out toExport);

            GameObject worldUpObject = new GameObject("worldUpObject");
            worldUpObject.transform.localEulerAngles = new Vector3(29, 190, 34);
            toExport.Add(worldUpObject);

            aimConstraint.aimVector = new Vector3(20, 170, 5);
            aimConstraint.rotationAtRest = new Vector3(230, 29, 49);
            aimConstraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;
            aimConstraint.rotationOffset = new Vector3(190, 120, 30);
            aimConstraint.upVector = new Vector3(50, 280, 10);
            aimConstraint.worldUpType = AimConstraint.WorldUpType.ObjectRotationUp;
            aimConstraint.worldUpObject = worldUpObject.transform;
            aimConstraint.worldUpVector = new Vector3(94, 38, 299);

            // export and compare
            var expConstraint = ExportAndCheckConstraint(aimConstraint, toExport.ToArray());

            Assert.That(expConstraint.aimVector, Is.EqualTo(aimConstraint.aimVector));
            Assert.That(AreRotationEqual(expConstraint.rotationAtRest, aimConstraint.rotationAtRest), Is.True);
            Assert.That(expConstraint.rotationAxis, Is.EqualTo(aimConstraint.rotationAxis));
            Assert.That(AreRotationEqual(expConstraint.rotationOffset, aimConstraint.rotationOffset), Is.True);
            Assert.That(expConstraint.upVector, Is.EqualTo(aimConstraint.upVector));
            Assert.That(expConstraint.worldUpType, Is.EqualTo(aimConstraint.worldUpType));
            Assert.That(expConstraint.worldUpObject.transform, Is.EqualTo(aimConstraint.worldUpObject.transform));
            Assert.That(expConstraint.worldUpVector, Is.EqualTo(aimConstraint.worldUpVector));
        }

        [Test, TestCaseSource(typeof(ConstraintAnimationTestDataClass), "TestCases")]
        public int TestConstraintAnimation(System.Type componentType, float[] keyTimes, float[] keyValues, string propertyName)
        {
            var go = new GameObject("root");
            var source = new GameObject("source");

            source.transform.parent = go.transform;

            var constraint = go.AddComponent(componentType) as IConstraint;
            Assert.That(constraint, Is.Not.Null);

            var cSource = new ConstraintSource();
            cSource.sourceTransform = source.transform;

            int index = constraint.AddSource(cSource);
            Assert.That(index, Is.EqualTo(0));

            var keyData = new FbxAnimationTest.PropertyKeyData
            {
                targetObject = go,
                componentType = componentType,
                propertyName = propertyName,
                keyTimes = keyTimes,
                keyFloatValues = keyValues
            };
            var tester = new FbxAnimationTest.AnimTester { keyData = keyData, testName = "ConstraintAnim_" + propertyName, path = GetRandomFbxFilePath() };

            return tester.DoIt();
        }

        [Test]
        public void TestDontExportSource()
        {
            // test that no error occurs if a source is not exported
            var constrained = new GameObject("constrained");
            var source1 = new GameObject("source1");
            var source2 = new GameObject("source2");
            var source3 = new GameObject("source3");

            var constraint = SetupConstraintWithSources<PositionConstraint>(constrained, new List<GameObject>() { source1, source2, source3 });

            // don't export source2
            var exportedGO = ExportConstraints(new Object[] { constrained, source1, source3 });

            // get exported constraint
            var expConstraint = exportedGO.GetComponentInChildren<PositionConstraint>();
            Assert.That(expConstraint, Is.Not.Null);

            var origSources = new List<ConstraintSource>();
            constraint.GetSources(origSources);

            var expSources = new List<ConstraintSource>();
            expConstraint.GetSources(expSources);

            Assert.That(expSources.Count, Is.EqualTo(origSources.Count - 1));

            Assert.That(expSources[0].sourceTransform, Is.EqualTo(origSources[0].sourceTransform));
            Assert.That(expSources[0].weight, Is.EqualTo(origSources[0].weight));
            Assert.That(expSources[1].sourceTransform, Is.EqualTo(origSources[2].sourceTransform));
            Assert.That(expSources[1].weight, Is.EqualTo(origSources[2].weight));
        }

        public bool AreRotationEqual(Vector3 a, Vector3 b, double epsilon = 0.0001)
        {
            Quaternion c = Quaternion.Euler(a.x, a.y, a.z);
            Quaternion d = Quaternion.Euler(b.x, b.y, b.z);

            float angle = Quaternion.Angle(c, d);

            return Mathf.Abs(angle) < epsilon;
        }

        /// <summary>
        /// Setup the constraint component on the constrained object with the given sources.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constrained"></param>
        /// <param name="sources"></param>
        /// <returns></returns>
        protected T SetupConstraintWithSources<T>(GameObject constrained, List<GameObject> sources) where T : Component, IConstraint
        {
            var constraint = constrained.AddComponent<T>();
            Assert.That(constraint, Is.Not.Null);

            int sourceCount = sources.Count;
            for (int i = 0; i < sourceCount; i++)
            {
                var source = sources[i];
                var cSource = new ConstraintSource();
                cSource.sourceTransform = source.transform;
                cSource.weight = i / ((float)sourceCount);

                int index = constraint.AddSource(cSource);
                Assert.That(index, Is.EqualTo(i));
            }
            Assert.That(constraint.sourceCount, Is.EqualTo(sourceCount));
            return constraint;
        }

        protected void TestSourcesMatch(IConstraint original, IConstraint exported)
        {
            var origSources = new List<ConstraintSource>();
            original.GetSources(origSources);

            var expSources = new List<ConstraintSource>();
            exported.GetSources(expSources);

            Assert.That(expSources.Count, Is.EqualTo(origSources.Count));

            for (int i = 0; i < origSources.Count; i++)
            {
                var origSource = origSources[i];
                var expSource = expSources[i];

                Assert.That(expSource.sourceTransform, Is.EqualTo(origSource.sourceTransform));
                Assert.That(expSource.weight, Is.EqualTo(origSource.weight));
            }
        }

        protected GameObject ExportConstraints(Object[] toExport)
        {
            var filename = GetRandomFileNamePath();
            var exportedGO = ExportSelection(filename, toExport);
            ImportConstraints(filename);
            return exportedGO;
        }

        protected void ImportConstraints(string filename)
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(filename) as ModelImporter;
            modelImporter.importConstraints = true;
            AssetDatabase.ImportAsset(filename);
        }
    }
}
