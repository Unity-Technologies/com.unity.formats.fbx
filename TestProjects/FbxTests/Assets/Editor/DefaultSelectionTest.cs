using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;
using Autodesk.Fbx;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests
{
    /// <summary>
    /// Tests the default selection export behavior.
    /// Tests that the right GameObjects are exported and
    /// that they have the expected transforms.
    /// </summary>
    public class DefaultSelectionTest : ExporterTestBase
    {
        protected GameObject m_root;
        internal ExportModelSettingsSerialize m_centerObjectsSetting;

        [SetUp]
        public override void Init ()
        {
            base.Init();
            m_centerObjectsSetting = new ExportModelSettingsSerialize ();
        }

        [TearDown]
        public override void Term ()
        {
            base.Term ();
            if (m_root) {
                UnityEngine.Object.DestroyImmediate (m_root);
            }
        }

        [Test]
        public void TestDefaultSelection ()
        {
            // Default selection behavior:
            //  - Export descendants
            //  - Don't export siblings
            //  - Don't export parents
            //  - If both a parent and descendant are selected,
            //    then result will be the same as if just the parent
            //    were selected
            //
            // Default transform export:
            //  - if there is only one root GameObject being exported
            //    then zero out the root transform, leave all descendants
            //    with local transform
            //  - if there are multiple root GameObjects, export
            //    the global transform of root GameObjects, and local transform
            //    of descendants.
            //    if Center Objects is checked in the preferences,
            //    then export the translations so they are centered
            //    around the center of the union of the bounding boxes.

            m_root = CreateHierarchy ();
            Assert.IsNotNull (m_root);

            // test without centered objects
            m_centerObjectsSetting.SetObjectPosition(ExportSettings.ObjectPosition.WorldAbsolute);

            // test Export Root
            // Expected result: everything gets exported
            // Expected transform: all transforms unchanged
            var exportedRoot = ExportSelection (m_root, m_centerObjectsSetting);
            CompareHierarchies (m_root, exportedRoot, true, false);
            CompareGlobalTransform (exportedRoot.transform, m_root.transform);

            // test Export Parent1, Child1
            // Expected result: Parent1, Child1, Child2
            // Expected transform: all transforms unchanged
            var parent1 = m_root.transform.Find ("Parent1");
            var child1 = parent1.Find ("Child1");
            exportedRoot = ExportSelection (new Object[]{ parent1.gameObject, child1.gameObject }, m_centerObjectsSetting);
            CompareHierarchies (parent1.gameObject, exportedRoot, true, false);
            CompareGlobalTransform (exportedRoot.transform, parent1);

            // test Export Child2
            // Expected result: Child2
            // Expected transform: Child2 unchanged
            var child2 = parent1.Find ("Child2").gameObject;
            exportedRoot = ExportSelection (child2, m_centerObjectsSetting);
            CompareHierarchies (child2, exportedRoot, true, false);
            CompareGlobalTransform (exportedRoot.transform, child2.transform);

            // test Export Child2, Parent2
            // Expected result: Parent2, Child3, Child2
            // Expected transform: Child2 and Parent2 maintain global transform
            var parent2 = m_root.transform.Find ("Parent2");
            var exportSet = new Object[]{ child2, parent2 };
            // for passing to FindCenter()
            var goExportSet = new GameObject[]{ child2.gameObject, parent2.gameObject };

            // test without centering objects
            m_centerObjectsSetting.SetObjectPosition(ExportSettings.ObjectPosition.WorldAbsolute);

            exportedRoot = ExportSelection (exportSet, m_centerObjectsSetting);
            List<GameObject> children = new List<GameObject> ();
            foreach (Transform child in exportedRoot.transform) {
                children.Add (child.gameObject);
            }
            CompareHierarchies (new GameObject[]{ child2, parent2.gameObject }, children.ToArray ());

            // test with centered objects
            m_centerObjectsSetting.SetObjectPosition(ExportSettings.ObjectPosition.LocalCentered);
            var newCenter = ModelExporter.FindCenter (goExportSet);

            exportedRoot = ExportSelection (exportSet, m_centerObjectsSetting);
            children = new List<GameObject> ();
            foreach (Transform child in exportedRoot.transform) {
                children.Add (child.gameObject);
            }
            CompareHierarchies (new GameObject[]{ child2, parent2.gameObject }, children.ToArray (), newCenter);
        }

        /// <summary>
        /// Compares the global transform of expected
        /// to the local transform of actual.
        /// If expected is null, then compare to the identity matrix.
        /// </summary>
        /// <param name="actual">Actual.</param>
        /// <param name="expected">Expected.</param>
        /// <param name="center">New center for expected transform, if present.</param>
        private void CompareGlobalTransform (Transform actual, Transform expected = null, Vector3 center = default(Vector3))
        {
            var actualMatrix = ConstructTRSMatrix (actual);
            var expectedMatrix = expected == null ? new FbxAMatrix () : ConstructTRSMatrix (expected, false, center);

            for (int i = 0; i < 4; i++) {
                for (int j = 0; j < 4; j++) {
                    Assert.That (actualMatrix [i] [j], Is.EqualTo (expectedMatrix [i] [j]).Within(0.0001));
                }
            }
        }

        /// <summary>
        /// Constructs a TRS matrix (as an FbxAMatrix) from a tranform.
        /// </summary>
        /// <returns>The TRS matrix.</returns>
        /// <param name="t">Transform.</param>
        /// <param name="local">If set to <c>true</c> use local transform.</param>
        /// <param name="center">New center for global transform.</param>
        private FbxAMatrix ConstructTRSMatrix (Transform t, bool local = true, Vector3 center = default(Vector3))
        {
            var translation = local ? t.localPosition : ModelExporter.GetRecenteredTranslation (t, center);
            var rotation = local ? t.localEulerAngles : t.eulerAngles;
            var scale = local ? t.localScale : t.lossyScale;
            return new FbxAMatrix (
                new FbxVector4 (translation.x, translation.y, translation.z),
                new FbxVector4 (rotation.x, rotation.y, rotation.z),
                new FbxVector4 (scale.x, scale.y, scale.z)
            );
        }

        /// <summary>
        /// Sets the transform.
        /// </summary>
        /// <param name="t">Transform.</param>
        /// <param name="pos">Position.</param>
        /// <param name="rot">Rotation.</param>
        /// <param name="scale">Scale.</param>
        private void SetTransform (Transform t, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            t.localPosition = pos;
            t.localEulerAngles = rot;
            t.localScale = scale;
        }

        /// <summary>
        /// Compares the hierarchies.
        /// </summary>
        /// <param name="expectedHierarchy">Expected hierarchy.</param>
        /// <param name="actualHierarchy">Actual hierarchy.</param>
        /// <param name="ignoreName">If set to <c>true</c> ignore name.</param>
        /// <param name="compareTransform">If set to <c>true</c> compare transform.</param>
        private void CompareHierarchies (
            GameObject expectedHierarchy, GameObject actualHierarchy,
            bool ignoreName = false, bool compareTransform = true)
        {
            if (!ignoreName) {
                Assert.AreEqual (expectedHierarchy.name, actualHierarchy.name);
            }

            var expectedTransform = expectedHierarchy.transform;
            var actualTransform = actualHierarchy.transform;

            if (compareTransform) {
                Assert.AreEqual (expectedTransform, actualTransform);
            }

            Assert.AreEqual (expectedTransform.childCount, actualTransform.childCount);

            foreach (Transform expectedChild in expectedTransform) {
                var actualChild = actualTransform.Find (expectedChild.name);
                Assert.IsNotNull (actualChild);
                CompareHierarchies (expectedChild.gameObject, actualChild.gameObject);
            }
        }

        /// <summary>
        /// Compares the hierarchies.
        /// </summary>
        /// <param name="expectedHierarchy">Expected hierarchy.</param>
        /// <param name="actualHierarchy">Actual hierarchy.</param>
        /// <param name="center">New center for global transforms.</param>
        private void CompareHierarchies (GameObject[] expectedHierarchy, GameObject[] actualHierarchy, Vector3 center = default(Vector3))
        {
            Assert.AreEqual (expectedHierarchy.Length, actualHierarchy.Length);

            System.Array.Sort (expectedHierarchy, delegate (GameObject x, GameObject y) {
                return x.name.CompareTo (y.name);
            });
            System.Array.Sort (actualHierarchy, delegate (GameObject x, GameObject y) {
                return x.name.CompareTo (y.name);
            });

            for (int i = 0; i < expectedHierarchy.Length; i++) {
                CompareHierarchies (expectedHierarchy [i], actualHierarchy [i], false, false);
                // if we are Comparing lists of hierarchies, that means that the transforms
                // should be the global transform of expected, as there is no zeroed out root
                CompareGlobalTransform (actualHierarchy [i].transform, expectedHierarchy [i].transform, center);
            }
        }
    }
}
