using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.Animations;

namespace FbxExporters.UnitTests
{
    public class FbxConstraintTest : ExporterTestBase
    {

        [Test]
        public void TestParentConstraintExport()
        {

        }

        [Test]
        public void TestAimConstraintExport()
        {

        }

        [Test]
        public void TestPositionConstraintExport()
        {
            // setup constrained object and sources
            var constrainedGO = new GameObject("constrained");
            var sourceGO = new GameObject("source");
            var sourceGO2 = new GameObject("source2");

            sourceGO.transform.localPosition = new Vector3(1, 2, 3);
            sourceGO2.transform.localPosition = new Vector3(4, 5, 6);

            var posConstraint = SetupConstraintWithSources<PositionConstraint>(constrainedGO, new List<GameObject>() { sourceGO, sourceGO2 });

            posConstraint.constraintActive = true;
            posConstraint.locked = true;
            posConstraint.translationAtRest = new Vector3(0.5f, 0.5f, 0.5f);
            posConstraint.translationAxis = Axis.X | Axis.Z;
            posConstraint.translationOffset = new Vector3(0.25f, 0.5f, 0.75f);
            posConstraint.weight = 0.8f;

            // export and compare
            var exportedGO = ExportConstraints( new Object[] { constrainedGO, sourceGO, sourceGO2 });

            // get exported constraint
            var expConstraint = exportedGO.GetComponentInChildren<PositionConstraint>();
            Assert.That(expConstraint, Is.Not.Null);

            TestSourcesMatch(posConstraint, expConstraint);

            Assert.That(expConstraint.constraintActive, Is.EqualTo(posConstraint.constraintActive));
            Assert.That(expConstraint.locked, Is.EqualTo(true)); // locked always imports as true
            Assert.That(expConstraint.translationAtRest, Is.EqualTo(posConstraint.translationAtRest));
            Assert.That(expConstraint.translationAxis, Is.EqualTo(posConstraint.translationAxis));
            Assert.That(expConstraint.translationOffset, Is.EqualTo(posConstraint.translationOffset));
            Assert.That(expConstraint.weight, Is.EqualTo(posConstraint.weight).Within(0.0001f));
        }

        [Test]
        public void TestRotationConstraint()
        {
            // setup constrained object and sources
            var constrainedGO = new GameObject("constrained");
            var sourceGO = new GameObject("source");

            sourceGO.transform.localRotation = Quaternion.Euler(20, 45, 90);

            var rotConstraint = SetupConstraintWithSources<RotationConstraint>(constrainedGO, new List<GameObject>() { sourceGO });

            rotConstraint.constraintActive = true;
            rotConstraint.locked = false;
            rotConstraint.rotationAtRest = new Vector3(30, 20, 10);
            rotConstraint.rotationAxis = Axis.Y;
            rotConstraint.weight = 0.4f;

            // export and compare
            var exportedGO = ExportConstraints(new Object[] { constrainedGO, sourceGO });

            // get exported constraint
            var expConstraint = exportedGO.GetComponentInChildren<RotationConstraint>();
            Assert.That(expConstraint, Is.Not.Null);

            TestSourcesMatch(rotConstraint, expConstraint);

            Assert.That(expConstraint.constraintActive, Is.EqualTo(rotConstraint.constraintActive));
            Assert.That(expConstraint.locked, Is.EqualTo(true)); // locked always imports as true
            Assert.That(AreEqual(expConstraint.rotationAtRest, rotConstraint.rotationAtRest, 0.001), Is.True);
            Assert.That(expConstraint.rotationAxis, Is.EqualTo(rotConstraint.rotationAxis));
            Assert.That(expConstraint.rotationOffset, Is.EqualTo(rotConstraint.rotationOffset));
            Assert.That(expConstraint.weight, Is.EqualTo(rotConstraint.weight).Within(0.0001f));
        }

        [Test]
        public void TestScaleConstraint()
        {

        }

        public bool AreEqual(Vector3 a, Vector3 b, double epsilon = 0.0001)
        {
            return Vector3.SqrMagnitude(a - b) < epsilon;
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
            for(int i = 0;  i < sourceCount; i++)
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

            for(int i = 0; i < origSources.Count; i++)
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