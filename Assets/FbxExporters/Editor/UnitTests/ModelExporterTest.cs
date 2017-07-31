// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.
//
// Licensed under the ##LICENSENAME##.
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************

using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections.Generic;

namespace FbxExporters.UnitTests
{
    public class ModelExporterTest
    {
        // add any GameObject that gets created to this list
        // so that it gets deleted in the TearDown
        private List<GameObject> m_createdObjects;

        [SetUp]
        public void Init()
        {
            m_createdObjects = new List<GameObject> ();
        }

        [TearDown]
        public void Term()
        {
            foreach (var obj in m_createdObjects) {
                GameObject.DestroyImmediate (obj);
            }
        }


        [Test]
        public void TestFindCenter ()
        {
            // Create 3 objects
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cube1 = GameObject.CreatePrimitive (PrimitiveType.Cube);
            var cube2 = GameObject.CreatePrimitive (PrimitiveType.Cube);

            m_createdObjects.Add (cube);
            m_createdObjects.Add (cube1);
            m_createdObjects.Add (cube2);

            // Set their transforms
            cube.transform.localPosition = new Vector3 (23, -5, 10);
            cube1.transform.localPosition = new Vector3 (23, -5, 4);
            cube1.transform.localScale = new Vector3 (1, 1, 2);
            cube2.transform.localPosition = new Vector3 (28, 0, 10);
            cube2.transform.localScale = new Vector3 (3, 1, 1);

            // Find the center
            var center = FbxExporters.Editor.ModelExporter.FindCenter(new GameObject[]{cube,cube1,cube2});

            // Check that it is what we expect
            Assert.AreEqual(center, new Vector3(26, -2.5f, 6.75f));
        }
    }
}