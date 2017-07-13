// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.
//
// Licensed under the ##LICENSENAME##.
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using FbxSdk;

namespace FbxExporters
{
    namespace Editor
    {
        public class ModelExporter : System.IDisposable
        {
            const string Title =
                "exports static meshes with materials and textures";

            const string Subject = 
                "";

            const string Keywords =
                "export mesh materials textures uvs";

            const string Comments =
                @"";

            const string MenuItemName = "Assets/Export Model...";

            const string FileBaseName = "Untitled";

            const string ProgressBarTitle = "Fbx Export";

            const char MayaNamespaceSeparator = ':';

            // replace invalid chars with this one
            const char InvalidCharReplacement = '_';

            const string RegexCharStart = "[";
            const string RegexCharEnd = "]";

            const int UnitScaleFactor = 100;

            /// <summary>
            /// Create instance of example
            /// </summary>
            public static ModelExporter Create ()
            {
                return new ModelExporter ();
            }

            /// <summary>
            /// Map Unity material name to FBX material object
            /// </summary>
            Dictionary<string, FbxSurfaceMaterial> MaterialMap = new Dictionary<string, FbxSurfaceMaterial> ();

            /// <summary>
            /// Map texture filename name to FBX texture object
            /// </summary>
            Dictionary<string, FbxTexture> TextureMap = new Dictionary<string, FbxTexture> ();

            /// <summary>
            /// Export the mesh's attributes using layer 0.
            /// </summary>
            public void ExportComponentAttributes (MeshInfo mesh, FbxMesh fbxMesh, int[] unmergedTriangles)
            {
                // Set the normals on Layer 0.
                FbxLayer fbxLayer = fbxMesh.GetLayer (0 /* default layer */);
                if (fbxLayer == null) {
                    fbxMesh.CreateLayer ();
                    fbxLayer = fbxMesh.GetLayer (0 /* default layer */);
                }

                using (var fbxLayerElement = FbxLayerElementNormal.Create (fbxMesh, "Normals")) {
                    fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                    fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eDirect);

                    // Add one normal per each vertex face index (3 per triangle)
                    FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                    for (int n = 0; n < unmergedTriangles.Length; n++) {
                        int unityTriangle = unmergedTriangles [n];
                        fbxElementArray.Add (CreateRightHandedFbxVector4 (mesh.Normals [unityTriangle]));
                    }

					fbxLayer.SetNormals (fbxLayerElement);
				}

                /// Set the binormals on Layer 0. 
                using (var fbxLayerElement = FbxLayerElementBinormal.Create (fbxMesh, "Binormals")) 
                {
                    fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                    fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eDirect);

                    // Add one normal per each vertex face index (3 per triangle)
                    FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                    for (int n = 0; n < unmergedTriangles.Length; n++) {
                        int unityTriangle = unmergedTriangles [n];
                        fbxElementArray.Add (CreateRightHandedFbxVector4 (mesh.Binormals [unityTriangle]));
                    }
                    fbxLayer.SetBinormals (fbxLayerElement);
                }

                /// Set the tangents on Layer 0.
                using (var fbxLayerElement = FbxLayerElementTangent.Create (fbxMesh, "Tangents")) 
                {
                    fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                    fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eDirect);

                    // Add one normal per each vertex face index (3 per triangle)
                    FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                    for (int n = 0; n < unmergedTriangles.Length; n++) {
                        int unityTriangle = unmergedTriangles [n];
                        fbxElementArray.Add (CreateRightHandedFbxVector4(
                            new Vector3(
                                mesh.Tangents[unityTriangle][0],
                                mesh.Tangents[unityTriangle][1],
                                mesh.Tangents[unityTriangle][2]
                            )));
                    }
                    fbxLayer.SetTangents (fbxLayerElement);
                }

                using (var fbxLayerElement = FbxLayerElementUV.Create (fbxMesh, "UVSet"))
                {
                    fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                    fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eIndexToDirect);

                    // set texture coordinates per vertex
                    FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                    // TODO: only copy unique UVs into this array, and index appropriately
                    for (int n = 0; n < mesh.UV.Length; n++) {
                        fbxElementArray.Add (new FbxVector2 (mesh.UV [n] [0],
                            mesh.UV [n] [1]));
                    }

                    // For each face index, point to a texture uv
                    FbxLayerElementArray fbxIndexArray = fbxLayerElement.GetIndexArray ();
                    fbxIndexArray.SetCount (unmergedTriangles.Length);

                    for(int i = 0; i < unmergedTriangles.Length; i++){
                        fbxIndexArray.SetAt (i, unmergedTriangles [i]);
                    }
                    fbxLayer.SetUVs (fbxLayerElement, FbxLayerElement.EType.eTextureDiffuse);
                }

                using (var fbxLayerElement = FbxLayerElementVertexColor.Create (fbxMesh, "VertexColors")) 
                {
                    fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                    fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eIndexToDirect);

                    // set texture coordinates per vertex
                    FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                    // TODO: only copy unique UVs into this array, and index appropriately
                    for (int n = 0; n < mesh.VertexColors.Length; n++) {
                        // Converting to Color from Color32, as Color32 stores the colors
                        // as ints between 0-255, while FbxColor and Color
                        // use doubles between 0-1
                        Color color = mesh.VertexColors [n];
                        fbxElementArray.Add (new FbxColor (color.r,
                            color.g,
                            color.b,
                            color.a));
                    }

                    // For each face index, point to a texture uv
                    FbxLayerElementArray fbxIndexArray = fbxLayerElement.GetIndexArray ();
                    fbxIndexArray.SetCount (unmergedTriangles.Length);

                    for(int i = 0; i < unmergedTriangles.Length; i++){
                        fbxIndexArray.SetAt (i, unmergedTriangles [i]);
                    }
                    fbxLayer.SetVertexColors (fbxLayerElement);
                }
            }

            /// <summary>
            /// Takes in a left-handed Vector3, and returns a right-handed FbxVector4.
            /// Helper for ExportComponentAttributes()
            /// </summary>
            /// <returns>The right-handed FbxVector4.</returns>
            private FbxVector4 CreateRightHandedFbxVector4(Vector3 leftHandedVector)
            {
                // negating the x component of the vector converts it from left to right handed coordinates
                return new FbxVector4 (
                    -leftHandedVector[0],
                    leftHandedVector[1],
                    leftHandedVector[2]);
            }

            /// <summary>
            /// Export an Unity Texture
            /// </summary>
            public void ExportTexture (Material unityMaterial, string unityPropName,
                                       FbxSurfaceMaterial fbxMaterial, string fbxPropName)
            {
                if (!unityMaterial) {
                    return;
                }

                // Get the texture on this property, if any.
                if (!unityMaterial.HasProperty (unityPropName)) {
                    return;
                }
                var unityTexture = unityMaterial.GetTexture (unityPropName);
                if (!unityTexture) {
                    return;
                }

                // Find its filename
                var textureSourceFullPath = AssetDatabase.GetAssetPath (unityTexture);
                if (textureSourceFullPath == "") {
                    return;
                }

                // get absolute filepath to texture
                textureSourceFullPath = Path.GetFullPath (textureSourceFullPath);

                if (Verbose)
                    Debug.Log (string.Format ("{2}.{1} setting texture path {0}", textureSourceFullPath, fbxPropName, fbxMaterial.GetName ()));

                // Find the corresponding property on the fbx material.
                var fbxMaterialProperty = fbxMaterial.FindProperty (fbxPropName);
                if (fbxMaterialProperty == null || !fbxMaterialProperty.IsValid ()) {
                    Debug.Log ("property not found");
                    return;
                }

                // Find or create an fbx texture and link it up to the fbx material.
                if (!TextureMap.ContainsKey (textureSourceFullPath)) {
                    var fbxTexture = FbxFileTexture.Create (fbxMaterial, fbxPropName + "_Texture");
                    fbxTexture.SetFileName (textureSourceFullPath);
                    fbxTexture.SetTextureUse (FbxTexture.ETextureUse.eStandard);
                    fbxTexture.SetMappingType (FbxTexture.EMappingType.eUV);
                    TextureMap.Add (textureSourceFullPath, fbxTexture);
                }
                TextureMap [textureSourceFullPath].ConnectDstProperty (fbxMaterialProperty);
            }

            /// <summary>
            /// Get the color of a material, or grey if we can't find it.
            /// </summary>
            public FbxDouble3 GetMaterialColor (Material unityMaterial, string unityPropName, float defaultValue = 1)
            {
                if (!unityMaterial) {
                    return new FbxDouble3(defaultValue);
                }
                if (!unityMaterial.HasProperty (unityPropName)) {
                    return new FbxDouble3(defaultValue);
                }
                var unityColor = unityMaterial.GetColor (unityPropName);
                return new FbxDouble3 (unityColor.r, unityColor.g, unityColor.b);
            }

            /// <summary>
            /// Export (and map) a Unity PBS material to FBX classic material
            /// </summary>
            public FbxSurfaceMaterial ExportMaterial (Material unityMaterial, FbxScene fbxScene, FbxMesh fbxMesh)
            {
                if (Verbose)
                    Debug.Log (string.Format ("exporting material {0}", unityMaterial.name));
                              
                var materialName = unityMaterial ? unityMaterial.name : "DefaultMaterial";
                if (MaterialMap.ContainsKey (materialName)) {
                    AssignLayerElementMaterial (fbxMesh);
                    return MaterialMap [materialName];
                }

                // We'll export either Phong or Lambert. Phong if it calls
                // itself specular, Lambert otherwise.
                var shader = unityMaterial ? unityMaterial.shader : null;
                bool specular = shader && shader.name.ToLower ().Contains ("specular");

                var fbxMaterial = specular
                    ? FbxSurfacePhong.Create (fbxScene, materialName)
                    : FbxSurfaceLambert.Create (fbxScene, materialName);

                // Copy the flat colours over from Unity standard materials to FBX.
                fbxMaterial.Diffuse.Set (GetMaterialColor (unityMaterial, "_Color"));
                fbxMaterial.Emissive.Set (GetMaterialColor (unityMaterial, "_EmissionColor"));
                fbxMaterial.Ambient.Set (new FbxDouble3 ());

                fbxMaterial.BumpFactor.Set (unityMaterial && unityMaterial.HasProperty ("_BumpScale") ? unityMaterial.GetFloat ("_BumpScale") : 0);

                if (specular) {
                    (fbxMaterial as FbxSurfacePhong).Specular.Set (GetMaterialColor (unityMaterial, "_SpecColor"));
                }

                // Export the textures from Unity standard materials to FBX.
                ExportTexture (unityMaterial, "_MainTex", fbxMaterial, FbxSurfaceMaterial.sDiffuse);
                ExportTexture (unityMaterial, "_EmissionMap", fbxMaterial, "emissive");
                ExportTexture (unityMaterial, "_BumpMap", fbxMaterial, FbxSurfaceMaterial.sNormalMap);
                if (specular) {
                    ExportTexture (unityMaterial, "_SpecGlosMap", fbxMaterial, FbxSurfaceMaterial.sSpecular);
                }

                AssignLayerElementMaterial (fbxMesh);

                MaterialMap.Add (materialName, fbxMaterial);
                return fbxMaterial;
            }

            private void AssignLayerElementMaterial(FbxMesh fbxMesh)
            {
                // Add FbxLayerElementMaterial to layer 0 of the node
                FbxLayer fbxLayer = fbxMesh.GetLayer (0 /* default layer */);
                if (fbxLayer == null) {
                    fbxMesh.CreateLayer ();
                    fbxLayer = fbxMesh.GetLayer (0 /* default layer */);
                }

                using (var fbxLayerElement = FbxLayerElementMaterial.Create (fbxMesh, "Material")) {
                    // Using all same means that the entire mesh uses the same material
                    fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eAllSame);
                    fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eIndexToDirect);

                    FbxLayerElementArray fbxElementArray = fbxLayerElement.GetIndexArray ();

                    // Map the entire geometry to the FbxNode material at index 0
                    fbxElementArray.Add (0);
                    fbxLayer.SetMaterials (fbxLayerElement);
                }
            }

            /// <summary>
            /// Unconditionally export this mesh object to the file.
            /// We have decided; this mesh is definitely getting exported.
            /// </summary>
            public void ExportMesh (MeshInfo meshInfo, FbxNode fbxNode, FbxScene fbxScene, bool weldVertices = true)
            {
                if (!meshInfo.IsValid)
                    return;

                NumMeshes++;
                NumTriangles += meshInfo.Triangles.Length / 3;

                // create the mesh structure.
                FbxMesh fbxMesh = FbxMesh.Create (fbxScene, "Scene");

                // Create control points.
                Dictionary<Vector3, int> ControlPointToIndex = new Dictionary<Vector3, int> ();

                int NumControlPoints = 0;
                if (weldVertices) {
                    for (int v = 0; v < meshInfo.VertexCount; v++) {
                        if (ControlPointToIndex.ContainsKey (meshInfo.Vertices [v])) {
                            continue;
                        }
                        ControlPointToIndex [meshInfo.Vertices [v]] = NumControlPoints;

                        NumControlPoints++;
                    }
                    fbxMesh.InitControlPoints (NumControlPoints);

                    // Copy control point data from Unity to FBX.
                    // As we do so, scale the points by 100 to convert
                    // from m to cm.
                    foreach (var controlPoint in ControlPointToIndex.Keys) {
                        fbxMesh.SetControlPointAt (new FbxVector4 (
                            -controlPoint.x*UnitScaleFactor,
                            controlPoint.y*UnitScaleFactor,
                            controlPoint.z*UnitScaleFactor
                        ), ControlPointToIndex [controlPoint]);
                    }
                } else {
                    NumControlPoints = meshInfo.VertexCount;
                    fbxMesh.InitControlPoints (NumControlPoints);

                    // copy control point data from Unity to FBX
                    for (int v = 0; v < NumControlPoints; v++)
                    {
                        // convert from left to right-handed by negating x (Unity negates x again on import)
                        fbxMesh.SetControlPointAt(new FbxVector4 (
                            -meshInfo.Vertices [v].x*UnitScaleFactor,
                            meshInfo.Vertices [v].y*UnitScaleFactor,
                            meshInfo.Vertices [v].z*UnitScaleFactor
                        ), v);
                    }
                }

                var fbxMaterial = ExportMaterial (meshInfo.Material, fbxScene, fbxMesh);
                fbxNode.AddMaterial (fbxMaterial);

                /*
                 * Triangles have to be added in reverse order, 
                 * or else they will be inverted on import 
                 * (due to the conversion from left to right handed coords)
                 */
                int[] unmergedTriangles = new int[meshInfo.Triangles.Length];
                int current = 0;
                for (int f = 0; f < meshInfo.Triangles.Length / 3; f++) {
                    fbxMesh.BeginPolygon ();

                    // triangle vertices have to be reordered to be 0,2,1 instead
                    // of 0,1,2, as this gets flipped back during import
                    foreach (int val in new int[]{0,2,1}) {
                        int tri = meshInfo.Triangles [3 * f + val];

                        // Save the triangle order (without merging vertices) so we
                        // properly export UVs, normals, binormals, etc.
                        unmergedTriangles [current] = tri;

                        if (weldVertices) {
                            tri = ControlPointToIndex [meshInfo.Vertices [tri]];
                        }
                        fbxMesh.AddPolygon (tri);

                        current++;
                    }
                    fbxMesh.EndPolygon ();
                }

                ExportComponentAttributes (meshInfo, fbxMesh, unmergedTriangles);

                // set the fbxNode containing the mesh
                fbxNode.SetNodeAttribute (fbxMesh);
                fbxNode.SetShadingMode (FbxNode.EShadingMode.eWireFrame);
            }

            // get a fbxNode's global default position.
            protected void ExportTransform (UnityEngine.Transform unityTransform, FbxNode fbxNode, TransformExportType exportType)
            {
                // Fbx rotation order is XYZ, but Unity rotation order is ZXY.
                // This causes issues when converting euler to quaternion, causing the final
                // rotation to be slighlty off.
                // Fixed if we set the rotation order to the Unity rotation order in the FBX.
                fbxNode.SetRotationOrder (FbxNode.EPivotSet.eSourcePivot, FbxEuler.EOrder.eOrderZXY);

                UnityEngine.Vector3 unityTranslate;
                UnityEngine.Vector3 unityRotate;
                UnityEngine.Vector3 unityScale;

                switch (exportType) {
                case TransformExportType.Zeroed:
                    unityTranslate = Vector3.zero;
                    unityRotate = Vector3.zero;
                    unityScale = Vector3.one;
                    break;
                case TransformExportType.Global:
                    unityTranslate = unityTransform.position;
                    unityRotate = unityTransform.rotation.eulerAngles;
                    unityScale = unityTransform.lossyScale;
                    break;
                default: /*case TransformExportType.Local*/
                    unityTranslate = unityTransform.localPosition;
                    unityRotate = unityTransform.localRotation.eulerAngles;
                    unityScale = unityTransform.localScale;
                    break;
                }

                // transfer transform data from Unity to Fbx
                // Negating the x value of the translation, and the y and z values of the rotation
                // to convert from Unity to Maya coordinates (left to righthanded).
                // Scaling the translation by 100 to convert from m to cm.
                var fbxTranslate = new FbxDouble3 (
                    -unityTranslate.x*UnitScaleFactor,
                    unityTranslate.y*UnitScaleFactor,
                    unityTranslate.z*UnitScaleFactor
                );
                var fbxRotate = new FbxDouble3 (unityRotate.x, -unityRotate.y, -unityRotate.z);
                var fbxScale = new FbxDouble3 (unityScale.x, unityScale.y, unityScale.z);

                // set the local position of fbxNode
                fbxNode.LclTranslation.Set (fbxTranslate);
                fbxNode.LclRotation.Set (fbxRotate);
                fbxNode.LclScaling.Set (fbxScale);

                return;
            }

            /// <summary>
            /// Unconditionally export components on this game object
            /// </summary>
            protected int ExportComponents (
                GameObject  unityGo, FbxScene fbxScene, FbxNode fbxNodeParent,
                int exportProgress, int objectCount, TransformExportType exportType = TransformExportType.Local)
            {
                int numObjectsExported = exportProgress;

                if (FbxExporters.EditorTools.ExportSettings.instance.mayaCompatibleNames) {
                    unityGo.name = ConvertToMayaCompatibleName (unityGo.name);
                }

                // create an FbxNode and add it as a child of parent
                FbxNode fbxNode = FbxNode.Create (fbxScene, unityGo.name);
                NumNodes++;

                numObjectsExported++;
                if (EditorUtility.DisplayCancelableProgressBar (
                        ProgressBarTitle,
                        string.Format ("Creating FbxNode {0}/{1}", numObjectsExported, objectCount),
                        (numObjectsExported / (float)objectCount) * 0.5f)) {
                    // cancel silently
                    return -1;
                }

                ExportTransform ( unityGo.transform, fbxNode, exportType);

                bool weldVertices = FbxExporters.EditorTools.ExportSettings.instance.weldVertices;
                ExportMesh (GetMeshInfo( unityGo ), fbxNode, fbxScene, weldVertices);

                if (Verbose)
                    Debug.Log (string.Format ("exporting {0}", fbxNode.GetName ()));

                fbxNodeParent.AddChild (fbxNode);

                // now  unityGo  through our children and recurse
                foreach (Transform childT in  unityGo.transform) {
                    numObjectsExported = ExportComponents (childT.gameObject, fbxScene, fbxNode, numObjectsExported, objectCount);
                }
                return numObjectsExported;
            }

            /// <summary>
            /// A count of how many GameObjects we are exporting, to have a rough
            /// idea of how long creating the scene will take.
            /// </summary>
            /// <returns>The hierarchy count.</returns>
            /// <param name="exportSet">Export set.</param>
            public int GetHierarchyCount (HashSet<GameObject> exportSet)
            {
                int count = 0;
                Queue<GameObject> queue = new Queue<GameObject> (exportSet);
                while (queue.Count > 0) {
                    var obj = queue.Dequeue ();
                    var objTransform = obj.transform;
                    foreach (Transform child in objTransform) {
                        queue.Enqueue (child.gameObject);
                    }
                    count++;
                }
                return count;
            }

            /// <summary>
            /// Removes objects that will already be exported anyway.
            /// E.g. if a parent and its child are both selected, then the child
            ///      will be removed from the export set.
            /// </summary>
            /// <returns>The revised export set</returns>
            /// <param name="unityExportSet">Unity export set.</param>
            public static HashSet<GameObject> RemoveRedundantObjects(IEnumerable<UnityEngine.Object> unityExportSet)
            {
                // basically just remove the descendents from the unity export set
                HashSet<GameObject> toExport = new HashSet<GameObject> ();
                HashSet<UnityEngine.Object> hashedExportSet = new HashSet<Object> (unityExportSet);

                foreach(var obj in unityExportSet){
                    var unityGo = GetGameObject (obj);

                    if (unityGo) {
                        // if any of this nodes ancestors is already in the export set,
                        // then ignore it, it will get exported already
                        bool parentInSet = false;
                        var parent = unityGo.transform.parent;
                        while (parent != null) {
                            if (hashedExportSet.Contains (parent.gameObject)) {
                                parentInSet = true;
                                break;
                            }
                            parent = parent.parent;
                        }

                        if (!parentInSet) {
                            toExport.Add (unityGo);
                        }
                    }
                }
                return toExport;
            }

            public enum TransformExportType { Local, Global, Zeroed };

            /// <summary>
            /// Export all the objects in the set.
            /// Return the number of objects in the set that we exported.
            /// </summary>
            public int ExportAll (IEnumerable<UnityEngine.Object> unityExportSet)
            {
                Verbose = true;
                try {
                    // Create the FBX manager
                    using (var fbxManager = FbxManager.Create ()) {
                        // Configure fbx IO settings.
                        fbxManager.SetIOSettings (FbxIOSettings.Create (fbxManager, Globals.IOSROOT));

                        // Export texture as embedded
                        if(EditorTools.ExportSettings.instance.embedTextures){
                            fbxManager.GetIOSettings ().SetBoolProp (Globals.EXP_FBX_EMBEDDED, true);
                        }

                        // Create the exporter
                        var fbxExporter = FbxExporter.Create (fbxManager, "Exporter");

                        // Initialize the exporter.
                        // fileFormat must be binary if we are embedding textures
                        int fileFormat = EditorTools.ExportSettings.instance.embedTextures? -1 :
                            fbxManager.GetIOPluginRegistry ().FindWriterIDByDescription ("FBX ascii (*.fbx)");

                        bool status = fbxExporter.Initialize (LastFilePath, fileFormat, fbxManager.GetIOSettings ());
                        // Check that initialization of the fbxExporter was successful
                        if (!status)
                            return 0;

                        // Set compatibility to 2014
                        fbxExporter.SetFileExportVersion ("FBX201400");

                        // Set the progress callback.
                        fbxExporter.SetProgressCallback (ExportProgressCallback);

                        // Create a scene
                        var fbxScene = FbxScene.Create (fbxManager, "Scene");

                        // set up the scene info
                        FbxDocumentInfo fbxSceneInfo = FbxDocumentInfo.Create (fbxManager, "SceneInfo");
                        fbxSceneInfo.mTitle = Title;
                        fbxSceneInfo.mSubject = Subject;
                        fbxSceneInfo.mAuthor = "Unity Technologies";
                        fbxSceneInfo.mRevision = "1.0";
                        fbxSceneInfo.mKeywords = Keywords;
                        fbxSceneInfo.mComment = Comments;
                        fbxScene.SetSceneInfo (fbxSceneInfo);

                        // Set up the axes (Y up, Z forward, X to the right) and units (centimeters)
                        // Exporting in centimeters as this is the default unit for FBX files, and easiest
                        // to work with when importing into Maya or Max
                        var fbxSettings = fbxScene.GetGlobalSettings ();
                        fbxSettings.SetSystemUnit (FbxSystemUnit.cm);

                        // The Unity axis system has Y up, Z forward, X to the right (left handed system with odd parity).
                        // The Maya axis system has Y up, Z forward, X to the left (right handed system with odd parity).
                        // We need to export right-handed for Maya because ConvertScene can't switch handedness:
                        // https://forums.autodesk.com/t5/fbx-forum/get-confused-with-fbxaxissystem-convertscene/td-p/4265472
                        fbxSettings.SetAxisSystem (FbxAxisSystem.MayaYUp);

                        // export set of object
                        FbxNode fbxRootNode = fbxScene.GetRootNode ();
                        // stores how many objects we have exported, -1 if export was cancelled
                        int exportProgress = 0;
                        var revisedExportSet = RemoveRedundantObjects(unityExportSet);
                        int count = GetHierarchyCount (revisedExportSet);

                        if(revisedExportSet.Count == 1){
                            foreach(var unityGo in revisedExportSet){
                                exportProgress = this.ExportComponents (unityGo, fbxScene, fbxRootNode, exportProgress, count, TransformExportType.Zeroed);
                                if (exportProgress < 0) {
                                    Debug.LogWarning ("Export Cancelled");
                                    return 0;
                                }
                            }
                        }
                        else{
                            foreach (var unityGo in revisedExportSet) {
                                exportProgress = this.ExportComponents (unityGo, fbxScene, fbxRootNode, exportProgress, count,
                                    unityGo.transform.parent == null? TransformExportType.Local : TransformExportType.Global);
                                if (exportProgress < 0) {
                                    Debug.LogWarning ("Export Cancelled");
                                    return 0;
                                }
                            }
                        }

                        // Export the scene to the file.
                        status = fbxExporter.Export (fbxScene);

                        // cleanup
                        fbxScene.Destroy ();
                        fbxExporter.Destroy ();

                        if (exportCancelled) {
                            Debug.LogWarning ("Export Cancelled");
                            // delete the file that got created
                            EditorApplication.update += DeleteFile;
                            return 0;
                        }

                        return status == true ? NumNodes : 0;
                    }
                } finally {
                    // You must clear the progress bar when you're done,
                    // otherwise it never goes away and many actions in Unity
                    // are blocked (e.g. you can't quit).
                    EditorUtility.ClearProgressBar ();
                }
            }

            static bool exportCancelled = false;

            static bool ExportProgressCallback (float percentage, string status)
            {
                // Convert from percentage to [0,1].
                // Then convert from that to [0.5,1] because the first half of
                // the progress bar was for creating the scene.
                var progress01 = 0.5f * (1f + (percentage / 100.0f));

                bool cancel = EditorUtility.DisplayCancelableProgressBar (ProgressBarTitle, "Exporting Scene...", progress01);

                if (cancel) {
                    exportCancelled = true;
                }

                // Unity says "true" for "cancel"; FBX wants "true" for "continue"
                return !cancel;
            }

            static void DeleteFile ()
            {
                if (File.Exists (LastFilePath)) {
                    try {
                        File.Delete (LastFilePath);
                    } catch (IOException) {
                    }

                    if (File.Exists (LastFilePath)) {
                        Debug.LogWarning ("Failed to delete file: " + LastFilePath);
                    }
                } else {
                    EditorApplication.update -= DeleteFile;
                    AssetDatabase.Refresh ();
                }
            }

            /// <summary>
            /// create menu item in the File menu
            /// </summary>
            [MenuItem (MenuItemName, false)]
            public static void OnMenuItem ()
            {
                OnExport ();
            }

            /// <summary>
            // Validate the menu item defined by the function above.
            /// </summary>
            [MenuItem (MenuItemName, true)]
            public static bool OnValidateMenuItem ()
            {
                return true;
            }

            // Add a menu item called "Export Model..." to a GameObject's context menu.
            // NOTE: The ellipsis at the end of the Menu Item name prevents the context
            //       from being passed to command, thus resulting in OnContextItem()
            //       being called only once regardless of what is selected.
            [MenuItem ("GameObject/Export Model...", false, 30)]
            static void OnContextItem (MenuCommand command)
            {
                OnExport ();
            }

            //
            // export mesh info from Unity
            //
            ///<summary>
            ///Information about the mesh that is important for exporting.
            ///</summary>
            public struct MeshInfo
            {
                /// <summary>
                /// The transform of the mesh.
                /// </summary>
                public Matrix4x4 xform;
                public Mesh mesh;

                /// <summary>
                /// The gameobject in the scene to which this mesh is attached.
                /// This can be null: don't rely on it existing!
                /// </summary>
                public GameObject unityObject;

                /// <summary>
                /// Return true if there's a valid mesh information
                /// </summary>
                /// <value>The vertex count.</value>
                public bool IsValid { get { return mesh != null; } }

                /// <summary>
                /// Gets the vertex count.
                /// </summary>
                /// <value>The vertex count.</value>
                public int VertexCount { get { return mesh.vertexCount; } }

                /// <summary>
                /// Gets the triangles. Each triangle is represented as 3 indices from the vertices array.
                /// Ex: if triangles = [3,4,2], then we have one triangle with vertices vertices[3], vertices[4], and vertices[2]
                /// </summary>
                /// <value>The triangles.</value>
                public int [] Triangles { get { return mesh.triangles; } }

                /// <summary>
                /// Gets the vertices, represented in local coordinates.
                /// </summary>
                /// <value>The vertices.</value>
                public Vector3 [] Vertices { get { return mesh.vertices; } }

                /// <summary>
                /// Gets the normals for the vertices.
                /// </summary>
                /// <value>The normals.</value>
                public Vector3 [] Normals { get { return mesh.normals; } }

                /// <summary>
                /// TODO: Gets the binormals for the vertices.
                /// </summary>
                /// <value>The normals.</value>
                private Vector3[] m_Binormals;

                public Vector3 [] Binormals {
                    get {
                        /// NOTE: LINQ
                        ///    return mesh.normals.Zip (mesh.tangents, (first, second)
                        ///    => Math.cross (normal, tangent.xyz) * tangent.w
                        if (m_Binormals == null || m_Binormals.Length == 0) 
                        {
                            m_Binormals = new Vector3 [mesh.normals.Length];

                            for (int i = 0; i < mesh.normals.Length; i++)
                                m_Binormals [i] = Vector3.Cross (mesh.normals [i],
                                    mesh.tangents [i])
                                * mesh.tangents [i].w;

                        }
                        return m_Binormals;
                    }
                }

                /// <summary>
                /// TODO: Gets the tangents for the vertices.
                /// </summary>
                /// <value>The tangents.</value>
                public Vector4 [] Tangents { get { return mesh.tangents; } }

                /// <summary>
                /// TODO: Gets the tangents for the vertices.
                /// </summary>
                /// <value>The tangents.</value>
                public Color32 [] VertexColors { get { return mesh.colors32; } }

                /// <summary>
                /// Gets the uvs.
                /// </summary>
                /// <value>The uv.</value>
                public Vector2 [] UV { get { return mesh.uv; } }

                /// <summary>
                /// The material used, if any; otherwise null.
                /// We don't support multiple materials on one gameobject.
                /// </summary>
                public Material Material {
                    get {
                        if (!unityObject) {
                            return null;
                        }
                        var renderer = unityObject.GetComponent<Renderer> ();
                        if (!renderer) {
                            return null;
                        }

                        if (FbxExporters.EditorTools.ExportSettings.instance.mayaCompatibleNames) {
                            renderer.sharedMaterial.name = ConvertToMayaCompatibleName (renderer.sharedMaterial.name);
                        }

                        // .material instantiates a new material, which is bad
                        // most of the time.
                        return renderer.sharedMaterial;
                    }
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="MeshInfo"/> struct.
                /// </summary>
                /// <param name="mesh">A mesh we want to export</param>
                public MeshInfo (Mesh mesh)
                {
                    this.mesh = mesh;
                    this.xform = Matrix4x4.identity;
                    this.unityObject = null;
                    this.m_Binormals = null;
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="MeshInfo"/> struct.
                /// </summary>
                /// <param name="gameObject">The GameObject the mesh is attached to.</param>
                /// <param name="mesh">A mesh we want to export</param>
                public MeshInfo (GameObject gameObject, Mesh mesh)
                {
                    this.mesh = mesh;
                    this.xform = gameObject.transform.localToWorldMatrix;
                    this.unityObject = gameObject;
                    this.m_Binormals = null;
                }
            }

            /// <summary>
            /// Get the GameObject
            /// </summary>
            private static GameObject GetGameObject (Object obj)
            {
                if (obj is UnityEngine.Transform) {
                    var xform = obj as UnityEngine.Transform;
                    return xform.gameObject;
                } else if (obj is UnityEngine.GameObject) {
                    return obj as UnityEngine.GameObject;
                } else if (obj is MonoBehaviour) {
                    var mono = obj as MonoBehaviour;
                    return mono.gameObject;
                }

                return null;
            }

            /// <summary>
            /// Get a mesh renderer's mesh.
            /// </summary>
            private MeshInfo GetMeshInfo (GameObject gameObject, bool requireRenderer = true)
            {
                // Two possibilities: it's a skinned mesh, or we have a mesh filter.
                Mesh mesh;
                var meshFilter = gameObject.GetComponent<MeshFilter> ();
                if (meshFilter) {
                    mesh = meshFilter.sharedMesh;
                } else {
                    var renderer = gameObject.GetComponent<SkinnedMeshRenderer> ();
                    if (!renderer) {
                        mesh = null;
                    } else {
                        mesh = new Mesh ();
                        renderer.BakeMesh (mesh);
                    }
                }
                if (!mesh) {
                    return new MeshInfo ();
                }
                return new MeshInfo (gameObject, mesh);
            }

            /// <summary>
            /// Number of nodes exported including siblings and decendents
            /// </summary>
            public int NumNodes { private set; get; }

            /// <summary>
            /// Number of meshes exported
            /// </summary>
            public int NumMeshes { private set; get; }

            /// <summary>
            /// Number of triangles exported
            /// </summary>
            public int NumTriangles { private set; get; }

            /// <summary>
            /// Clean up this class on garbage collection
            /// </summary>
            public void Dispose ()
            {
            }

            public bool Verbose { private set; get; }

            /// <summary>
            /// manage the selection of a filename
            /// </summary>
            static string LastFilePath { get; set; }

            const string Extension = "fbx";

            private static string MakeFileName (string basename = "test", string extension = "fbx")
            {
                return basename + "." + extension;
            }

            private static void OnExport ()
            {
                // Now that we know we have stuff to export, get the user-desired path.
                var directory = string.IsNullOrEmpty (LastFilePath)
                					  ? Application.dataPath
                					  : System.IO.Path.GetDirectoryName (LastFilePath);

                GameObject [] selectedGOs = Selection.GetFiltered<GameObject> (SelectionMode.TopLevel);
                string filename = null;
                if (selectedGOs.Length == 1) {
                    filename = ConvertToValidFilename (selectedGOs [0].name + ".fbx");
                } else {
                    filename = string.IsNullOrEmpty (LastFilePath)
                        ? MakeFileName (basename: FileBaseName, extension: Extension)
                        : System.IO.Path.GetFileName (LastFilePath);
                }

                var title = string.Format ("Export Model FBX ({0})", FileBaseName);

                var filePath = EditorUtility.SaveFilePanel (title, directory, filename, "fbx");

                if (string.IsNullOrEmpty (filePath)) {
                    return;
                }

                if (ExportObjects (filePath) != null) {
                    // refresh the asset database so that the file appears in the
                    // asset folder view.
                    AssetDatabase.Refresh ();
                }
            }

            /// <summary>
            /// Export a list of (Game) objects to FBX file. 
            /// Use the SaveFile panel to allow user to enter a file name.
            /// <summary>
            public static object ExportObjects (string filePath, UnityEngine.Object[] objects = null)
            {
                LastFilePath = filePath;

                using (var fbxExporter = Create ()) {
                    // ensure output directory exists
                    EnsureDirectory (filePath);

                    if (objects == null) {
                        objects = Selection.objects;
                    }

                    if (fbxExporter.ExportAll (objects) > 0) {
                        string message = string.Format ("Successfully exported: {0}", filePath);
                        UnityEngine.Debug.Log (message);

                        return filePath;
                    }
                }
                return null;
            }

            private static void EnsureDirectory (string path)
            {
                //check to make sure the path exists, and if it doesn't then
                //create all the missing directories.
                FileInfo fileInfo = new FileInfo (path);

                if (!fileInfo.Exists) {
                    Directory.CreateDirectory (fileInfo.Directory.FullName);
                }
            }

            /// <summary>
            /// Removes the diacritics (i.e. accents) from letters.
            /// e.g. é becomes e
            /// </summary>
            /// <returns>Text with accents removed.</returns>
            /// <param name="text">Text.</param>
            private static string RemoveDiacritics(string text) 
            {
                var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
                var stringBuilder = new System.Text.StringBuilder();

                foreach (var c in normalizedString)
                {
                    var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                    if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                    {
                        stringBuilder.Append(c);
                    }
                }

                return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
            }

            private static string ConvertToMayaCompatibleName(string name)
            {
                string newName = RemoveDiacritics (name);

                if (char.IsDigit (newName [0])) {
                    newName = newName.Insert (0, InvalidCharReplacement.ToString());
                }

                for (int i = 0; i < newName.Length; i++) {
                    if (!char.IsLetterOrDigit (newName, i)) {
                        if (i < newName.Length-1 && newName [i] == MayaNamespaceSeparator) {
                            continue;
                        }
                        newName = newName.Replace (newName [i], InvalidCharReplacement);
                    }
                }
                return newName;
            }

            public static string ConvertToValidFilename(string filename)
            {
                return System.Text.RegularExpressions.Regex.Replace (filename, 
                    RegexCharStart + new string(Path.GetInvalidFileNameChars()) + RegexCharEnd,
                    InvalidCharReplacement.ToString()
                );
            }
        }
    }
}
