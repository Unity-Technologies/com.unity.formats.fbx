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

            var posConstraint = constrainedGO.AddComponent<PositionConstraint>();
            Assert.That(posConstraint, Is.Not.Null);

            var cSource1 = new ConstraintSource();
            cSource1.sourceTransform = sourceGO.transform;
            cSource1.weight = 0.5f;

            var cSource2 = new ConstraintSource();
            cSource2.sourceTransform = sourceGO2.transform;
            cSource2.weight = 0.37f;

            int index = posConstraint.AddSource(cSource1);
            Assert.That(index, Is.EqualTo(0));
            index = posConstraint.AddSource(cSource2);
            Assert.That(index, Is.EqualTo(1));
            Assert.That(posConstraint.sourceCount, Is.EqualTo(2));

            posConstraint.constraintActive = true;
            posConstraint.locked = true;
            posConstraint.translationAtRest = new Vector3(0.5f, 0.5f, 0.5f);
            posConstraint.translationAxis = Axis.X | Axis.Z;
            posConstraint.translationOffset = new Vector3(0.25f, 0.5f, 0.75f);
            posConstraint.weight = 0.8f;

            // export and compare
            var filename = GetRandomFileNamePath();
            var exportedGO = ExportSelection(filename, new Object[] { constrainedGO, sourceGO, sourceGO2 });
            ImportConstraints(filename);

            // get exported constraint
            var expConstraint = exportedGO.GetComponentInChildren<PositionConstraint>();
            Assert.That(expConstraint, Is.Not.Null);

            TestSourcesMatch(posConstraint, expConstraint);

            Assert.That(expConstraint.constraintActive, Is.EqualTo(posConstraint.constraintActive));
            Assert.That(expConstraint.locked, Is.EqualTo(posConstraint.locked));
            Assert.That(expConstraint.translationAtRest, Is.EqualTo(posConstraint.translationAtRest));
            Assert.That(expConstraint.translationAxis, Is.EqualTo(posConstraint.translationAxis));
            Assert.That(expConstraint.translationOffset, Is.EqualTo(posConstraint.translationOffset));
            Assert.That(expConstraint.weight, Is.EqualTo(posConstraint.weight).Within(0.0001f));
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

        protected void ImportConstraints(string filename)
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(filename) as ModelImporter;
            modelImporter.importConstraints = true;
            AssetDatabase.ImportAsset(filename);
        }
    }
}