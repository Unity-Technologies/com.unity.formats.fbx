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

            /// <summary>
            /// Create instance of example
            /// </summary>
            public static ModelExporter Create () { return new ModelExporter (); }

            /// <summary>
            /// Map Unity material name to FBX material object
            /// </summary>
            Dictionary<string, FbxSurfaceMaterial> MaterialMap = new Dictionary<string, FbxSurfaceMaterial>();

            /// <summary>
            /// Map texture filename name to FBX texture object
            /// </summary>
            Dictionary<string, FbxTexture> TextureMap = new Dictionary<string, FbxTexture>();

            /// <summary>
            /// Export the mesh's UVs using layer 0.
            /// </summary>
            public void ExportUVs (MeshInfo mesh, FbxMesh fbxMesh)
            {
                // Set the normals on Layer 0.
                FbxLayer fbxLayer = fbxMesh.GetLayer (0 /* default layer */);
                if (fbxLayer == null) {
                    fbxMesh.CreateLayer ();
                    fbxLayer = fbxMesh.GetLayer (0 /* default layer */);
                }

                using (var fbxLayerElement = FbxLayerElementUV.Create (fbxMesh, "UVSet"))
                {
                    fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                    fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eIndexToDirect);

                    // set texture coordinates per vertex
                    FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                    for (int n = 0; n < mesh.UV.Length; n++) {
                    fbxElementArray.Add (new FbxVector2 (mesh.UV [n] [0],
                                                      mesh.UV [n] [1]));
                    }

                    // For each face index, point to a texture uv
                    var unityTriangles = mesh.Triangles;
                    FbxLayerElementArray fbxIndexArray = fbxLayerElement.GetIndexArray ();
                    fbxIndexArray.SetCount (unityTriangles.Length);

                    for (int i = 0, n = unityTriangles.Length; i < n; ++i) {
                        fbxIndexArray.SetAt (i, unityTriangles[i]);
                    }
                    fbxLayer.SetUVs (fbxLayerElement, FbxLayerElement.EType.eTextureDiffuse);
                }
            }

            /// <summary>
            /// Export an Unity Texture
            /// </summary>
            public void ExportTexture (Material unityMaterial, string unityPropName,
                FbxSurfaceMaterial fbxMaterial, string fbxPropName)
            {
                if (!unityMaterial) { return; }

                // Get the texture on this property, if any.
                if (!unityMaterial.HasProperty(unityPropName)) { return; }
                var unityTexture = unityMaterial.GetTexture (unityPropName);
                if (!unityTexture) { return; }

                // Find its filename
                var textureSourceFullPath = AssetDatabase.GetAssetPath(unityTexture);
                if (textureSourceFullPath == "") { return; }

                // get absolute filepath to texture
                textureSourceFullPath  = Path.GetFullPath(textureSourceFullPath);

                if (Verbose)
                    Debug.Log(string.Format("{2}.{1} setting texture path {0}", textureSourceFullPath, fbxPropName, fbxMaterial.GetName()));

                // Find the corresponding property on the fbx material.
                var fbxMaterialProperty = fbxMaterial.FindProperty (fbxPropName);
                if (fbxMaterialProperty == null || !fbxMaterialProperty.IsValid()) { Debug.Log("property not found"); return; }

                // Find or create an fbx texture and link it up to the fbx material.
                if (!TextureMap.ContainsKey(textureSourceFullPath)) {
                    var fbxTexture = FbxFileTexture.Create (fbxMaterial, fbxPropName + "_Texture");
                    fbxTexture.SetFileName (textureSourceFullPath);
                    fbxTexture.SetTextureUse (FbxTexture.ETextureUse.eStandard);
                    fbxTexture.SetMappingType (FbxTexture.EMappingType.eUV);
                    TextureMap.Add(textureSourceFullPath, fbxTexture);
                }
                TextureMap[textureSourceFullPath].ConnectDstProperty(fbxMaterialProperty);
            }

            /// <summary>
            /// Get the color of a material, or grey if we can't find it.
            /// </summary>
            public FbxDouble3 GetMaterialColor(Material unityMaterial, string unityPropName)
            {
                if (!unityMaterial) { return new FbxDouble3(0.5); }
                if (!unityMaterial.HasProperty(unityPropName)) { return new FbxDouble3(0.5); }
                var unityColor = unityMaterial.GetColor (unityPropName);
                return new FbxDouble3 (unityColor.r, unityColor.g, unityColor.b);
            }

            /// <summary>
            /// Export (and map) a Unity PBS material to FBX classic material
            /// </summary>
            public FbxSurfaceMaterial ExportMaterial (Material unityMaterial, FbxScene fbxScene)
            {
                if (Verbose)
                    Debug.Log(string.Format("exporting material {0}",unityMaterial.name));
                              
                var materialName = unityMaterial ? unityMaterial.name : "DefaultMaterial";
                if (MaterialMap.ContainsKey (materialName)) {
                    return MaterialMap [materialName];
                }

                // We'll export either Phong or Lambert. Phong if it calls
                // itself specular, Lambert otherwise.
                var shader = unityMaterial ? unityMaterial.shader : null;
                bool specular = shader && shader.name.ToLower().Contains("specular");

                var fbxMaterial = specular
                    ? FbxSurfacePhong.Create(fbxScene, materialName)
                    : FbxSurfaceLambert.Create(fbxScene, materialName);

                // Copy the flat colours over from Unity standard materials to FBX.
                fbxMaterial.Diffuse.Set(GetMaterialColor(unityMaterial, "_Color"));
                fbxMaterial.Emissive.Set(GetMaterialColor(unityMaterial, "_EmissionColor"));
                fbxMaterial.Ambient.Set(new FbxDouble3 ());
                fbxMaterial.BumpFactor.Set (unityMaterial ? unityMaterial.GetFloat ("_BumpScale") : 0);
                if (specular) {
                    (fbxMaterial as FbxSurfacePhong).Specular.Set(GetMaterialColor(unityMaterial, "_SpecColor"));
                }

                // Export the textures from Unity standard materials to FBX.
                ExportTexture (unityMaterial, "_MainTex", fbxMaterial, FbxSurfaceMaterial.sDiffuse);
                ExportTexture (unityMaterial, "_EmissionMap", fbxMaterial, "emissive");
                ExportTexture (unityMaterial, "_BumpMap", fbxMaterial, FbxSurfaceMaterial.sNormalMap);
                if (specular) {
                    ExportTexture (unityMaterial, "_SpecGlosMap", fbxMaterial, FbxSurfaceMaterial.sSpecular);
                }

                MaterialMap.Add(materialName, fbxMaterial);
                return fbxMaterial;
            }

            /// <summary>
            /// Unconditionally export this mesh object to the file.
            /// We have decided; this mesh is definitely getting exported.
            /// </summary>
            public void ExportMesh (MeshInfo meshInfo, FbxNode fbxNode, FbxScene fbxScene)
            {
                if (!meshInfo.IsValid)
                    return;

                NumMeshes++;
                NumTriangles += meshInfo.Triangles.Length / 3;
                NumVertices += meshInfo.VertexCount;

                // create the mesh structure.
                FbxMesh fbxMesh = FbxMesh.Create (fbxScene, "Scene");

                // Create control points.
                int NumControlPoints = meshInfo.VertexCount;

                fbxMesh.InitControlPoints (NumControlPoints);

                // copy control point data from Unity to FBX
                for (int v = 0; v < NumControlPoints; v++)
                {
                    fbxMesh.SetControlPointAt(new FbxVector4 (meshInfo.Vertices [v].x, meshInfo.Vertices [v].y, meshInfo.Vertices [v].z), v);
                }

                ExportUVs (meshInfo, fbxMesh);

                var fbxMaterial = ExportMaterial (meshInfo.Material, fbxScene);
                fbxNode.AddMaterial (fbxMaterial);

                for (int f = 0; f < meshInfo.Triangles.Length / 3; f++)
                {
                    fbxMesh.BeginPolygon ();
                    fbxMesh.AddPolygon (meshInfo.Triangles [3 * f]);
                    fbxMesh.AddPolygon (meshInfo.Triangles [3 * f + 1]);
                    fbxMesh.AddPolygon (meshInfo.Triangles [3 * f + 2]);
                    fbxMesh.EndPolygon ();
                }

                // set the fbxNode containing the mesh
                fbxNode.SetNodeAttribute (fbxMesh);
                fbxNode.SetShadingMode (FbxNode.EShadingMode.eWireFrame);
            }

            // get a fbxNode's global default position.
            protected void ExportTransform (UnityEngine.Transform unityTransform, FbxNode fbxNode)
            {
                // get local position of fbxNode (from Unity)
                UnityEngine.Vector3 unityTranslate = unityTransform.localPosition;
                UnityEngine.Vector3 unityRotate = unityTransform.localRotation.eulerAngles;
                UnityEngine.Vector3 unityScale = unityTransform.localScale;

                // transfer transform data from Unity to Fbx
                var fbxTranslate = new FbxDouble3 (unityTranslate.x, unityTranslate.y, unityTranslate.z);
                var fbxRotate = new FbxDouble3 (unityRotate.x, unityRotate.y, unityRotate.z);
                var fbxScale = new FbxDouble3 (unityScale.x, unityScale.y, unityScale.z);

                // set the local position of fbxNode
                fbxNode.LclTranslation.Set(fbxTranslate);
                fbxNode.LclRotation.Set(fbxRotate);
                fbxNode.LclScaling.Set(fbxScale);

                return;
            }

            /// <summary>
            /// Unconditionally export components on this game object
            /// </summary>
            protected int ExportComponents (GameObject  unityGo, FbxScene fbxScene, FbxNode fbxNodeParent, int currentIndex, int objectCount)
            {
                int i = currentIndex;

                // create an FbxNode and add it as a child of parent
                FbxNode fbxNode = FbxNode.Create (fbxScene,  unityGo.name);
                NumNodes++;

                i++;
                if (EditorUtility.DisplayCancelableProgressBar(
                    ProgressBarTitle,
                    string.Format("Creating FbxNode {0}/{1}", i, objectCount),
                    (i/(float)objectCount)*0.5f)) {
                    // cancel silently
                    return -1;
                }

                ExportTransform ( unityGo.transform, fbxNode);
                ExportMesh (GetMeshInfo( unityGo ), fbxNode, fbxScene);

                if (Verbose)
                    Debug.Log (string.Format ("exporting {0}", fbxNode.GetName ()));

                fbxNodeParent.AddChild (fbxNode);

                // now  unityGo  through our children and recurse
                foreach (Transform childT in  unityGo.transform)
                {
                    i = ExportComponents (childT.gameObject, fbxScene, fbxNode, i, objectCount);
                }
                return i;
            }

            /// <summary>
            /// A count of how many GameObjects we are exporting, to have a rough
            /// idea of how long creating the scene will take.
            /// </summary>
            /// <returns>The object count.</returns>
            /// <param name="exportSet">Export set.</param>
            public int GetGameObjectCount(IEnumerable<UnityEngine.Object> exportSet)
            {
                int count = 0;
                foreach (var obj in exportSet) {
                    var unityGo = GetGameObject (obj);
                    if (unityGo) {
                        count += unityGo.transform.hierarchyCount;
                    }
                }
                return count;
            }

            /// <summary>
            /// Export all the objects in the set.
            /// Return the number of objects in the set that we exported.
            /// </summary>
            public int ExportAll (IEnumerable<UnityEngine.Object> unityExportSet)
            {
                Verbose = true;
                try{
                    // Create the FBX manager
                    using (var fbxManager = FbxManager.Create ())
                    {
                        // Configure fbx IO settings.
                        fbxManager.SetIOSettings (FbxIOSettings.Create (fbxManager, Globals.IOSROOT));

                        // Export texture as embedded
                        fbxManager.GetIOSettings ().SetBoolProp (Globals.EXP_FBX_EMBEDDED, true);

                        // Create the exporter
                        var fbxExporter = FbxExporter.Create (fbxManager, "Exporter");

                        // Initialize the exporter.
                        int fileFormat = fbxManager.GetIOPluginRegistry ().FindWriterIDByDescription ("FBX ascii (*.fbx)");
                        bool status = fbxExporter.Initialize (LastFilePath, fileFormat, fbxManager.GetIOSettings ());
                        // Check that initialization of the fbxExporter was successful
                        if (!status)
                            return 0;

                        // Set compatibility to 2014
                        fbxExporter.SetFileExportVersion("FBX201400");

                        // Set the progress callback.
                        fbxExporter.SetProgressCallback(ExportProgressCallback);

                        // Create a scene
                        var fbxScene = FbxScene.Create (fbxManager, "Scene");

                        // set up the scene info
                        FbxDocumentInfo fbxSceneInfo = FbxDocumentInfo.Create (fbxManager, "SceneInfo");
                        fbxSceneInfo.mTitle     = Title;
                        fbxSceneInfo.mSubject   = Subject;
                        fbxSceneInfo.mAuthor    = "Unity Technologies";
                        fbxSceneInfo.mRevision  = "1.0";
                        fbxSceneInfo.mKeywords  = Keywords;
                        fbxSceneInfo.mComment   = Comments;
                        fbxScene.SetSceneInfo (fbxSceneInfo);

                        // Set up the axes (Y up, Z forward, X to the right) and units (meters)
                        var fbxSettings = fbxScene.GetGlobalSettings();
                        fbxSettings.SetSystemUnit(FbxSystemUnit.m);

                        // The Unity axis system has Y up, Z forward, X to the right (left handed system with odd parity).
                        // The Maya axis system has Y up, Z forward, X to the left (right handed system with odd parity).
                        // We need to export right-handed for Maya because ConvertScene can't switch handedness:
                        // https://forums.autodesk.com/t5/fbx-forum/get-confused-with-fbxaxissystem-convertscene/td-p/4265472
                        fbxSettings.SetAxisSystem (FbxAxisSystem.MayaYUp);

                        // export set of object
                        FbxNode fbxRootNode = fbxScene.GetRootNode ();
                        int i = 0;
                        int count = GetGameObjectCount(unityExportSet);
                        foreach (var obj in unityExportSet)
                        {
                            var  unityGo  = GetGameObject (obj);

                            if ( unityGo )
                            {
                                i = this.ExportComponents (unityGo, fbxScene, fbxRootNode, i, count);
                                if(i < 0){
                                    Debug.LogWarning("Export Cancelled");
                                    return 0;
                                }
                            }
                        }

                        // Export the scene to the file.
                        status = fbxExporter.Export (fbxScene);

                        // cleanup
                        fbxScene.Destroy ();
                        fbxExporter.Destroy ();

                        if(exportCancelled){
                            Debug.LogWarning ("Export Cancelled");
                            // delete the file that got created
                            EditorApplication.update += DeleteFile;
                            return 0;
                        }

                        return status == true ? NumNodes : 0;
                    }
                }
                finally {
                    // You must clear the progress bar when you're done,
                    // otherwise it never goes away and many actions in Unity
                    // are blocked (e.g. you can't quit).
                    EditorUtility.ClearProgressBar();
                }
            }

            static bool exportCancelled = false;

            static bool ExportProgressCallback(float percentage, string status) {
                // Convert from percentage to [0,1].
                // Then convert from that to [0.5,1] because the first half of
                // the progress bar was for creating the scene.
                var progress01 = 0.5f * (1f + (percentage / 100.0f));

                bool cancel = EditorUtility.DisplayCancelableProgressBar(ProgressBarTitle, "Exporting Scene...", progress01);

                if (cancel) {
                    exportCancelled = true;
                }

                // Unity says "true" for "cancel"; FBX wants "true" for "continue"
                return !cancel;
            }

            static void DeleteFile()
            {
                if (File.Exists (LastFilePath)) {
                    try {
                        File.Delete (LastFilePath);
                    } catch (IOException) {}

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
                OnExport();
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
            [MenuItem ("GameObject/Export Model... %e", false, 30)]
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
                private Vector3 [] m_Binormals;
                public Vector3 [] Binormals
                {
                    get
                    {
                        /// NOTE: LINQ
                        ///    return mesh.normals.Zip (mesh.tangents, (first, second)
                        ///    => Math.cross (normal, tangent.xyz) * tangent.w
                        if (m_Binormals.Length == 0)
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
                public Color [] VertexColors { get { return mesh.colors; } }

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
                        if (!unityObject) { return null; }
                        var renderer = unityObject.GetComponent<Renderer>();
                        if (!renderer) { return null; }
                        // .material instantiates a new material, which is bad
                        // most of the time.
                        return renderer.sharedMaterial;
                    }
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="MeshInfo"/> struct.
                /// </summary>
                /// <param name="mesh">A mesh we want to export</param>
                public MeshInfo(Mesh mesh) {
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
                public MeshInfo(GameObject gameObject, Mesh mesh) {
                    this.mesh = mesh;
                    this.xform = gameObject.transform.localToWorldMatrix;
                    this.unityObject = gameObject;
                    this.m_Binormals = null;
                }
            }

            /// <summary>
            /// Get the GameObject
            /// </summary>
            private GameObject GetGameObject (Object obj)
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
                    var renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
                    if (!renderer) {
                        mesh = null;
                    } else {
                        mesh = new Mesh();
                        renderer.BakeMesh(mesh);
                    }
                }
                if (!mesh) {
                    return new MeshInfo();
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
            /// Number of vertices
            /// </summary>
            public int NumVertices { private set; get; }

            /// <summary>
            /// Clean up this class on garbage collection
            /// </summary>
            public void Dispose () { }

            public bool Verbose { private set; get; }

            /// <summary>
            /// manage the selection of a filename
            /// </summary>
            static string LastFilePath { get; set; }
            const string Extension = "fbx";

            private static string MakeFileName(string basename = "test", string extension = "fbx")
            {
                return basename + "." + extension;
            }

            private static void OnExport ()
            {
                // Now that we know we have stuff to export, get the user-desired path.
                var directory = string.IsNullOrEmpty (LastFilePath)
                					  ? Application.dataPath
                					  : System.IO.Path.GetDirectoryName (LastFilePath);

                var filename = string.IsNullOrEmpty (LastFilePath)
                					 ? MakeFileName (basename: FileBaseName, extension: Extension)
                					 : System.IO.Path.GetFileName (LastFilePath);

                var title = string.Format ("Export Model FBX ({0})", FileBaseName);

                var filePath = EditorUtility.SaveFilePanel (title, directory, filename, "");

                if (string.IsNullOrEmpty (filePath)) {
                    return ;
                }

                if (ExportObjects (filePath)!=null)
                {
                    // refresh the asset database so that the file appears in the
                    // asset folder view.
                    AssetDatabase.Refresh ();
                }
            }

            /// <summary>
            /// Export a list of (Game) objects to FBX file. 
            /// Use the SaveFile panel to allow user to enter a file name.
            /// <summary>
            public static object ExportObjects(string filePath, UnityEngine.Object[] objects = null)
            {
                LastFilePath = filePath;

                using (var fbxExporter = Create())
                {
                    // ensure output directory exists
                    EnsureDirectory (filePath);

                    if (objects == null)
                    {
                        objects = Selection.objects;
                    }

                    if (fbxExporter.ExportAll(objects) > 0)
                    {
                        string message = string.Format ("Successfully exported: {0}", filePath);
                        UnityEngine.Debug.Log (message);

                        return filePath;
                    }
                }
                return null;
            }

            private static void EnsureDirectory(string path)
            {
                //check to make sure the path exists, and if it doesn't then
                //create all the missing directories.
                FileInfo fileInfo = new FileInfo (path);

                if (!fileInfo.Exists) {
                    Directory.CreateDirectory (fileInfo.Directory.FullName);
                }
            }
        }
    }
}
