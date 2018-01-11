using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Unity.FbxSdk;
using System.Linq;

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

            const string ReadmeRelativePath = "FbxExporters/README.txt";

            // NOTE: The ellipsis at the end of the Menu Item name prevents the context
            //       from being passed to command, thus resulting in OnContextItem()
            //       being called only once regardless of what is selected.
            const string MenuItemName = "GameObject/Export Model...";

            const string FileBaseName = "Untitled";

            const string ProgressBarTitle = "Fbx Export";

            const char MayaNamespaceSeparator = ':';

            // replace invalid chars with this one
            const char InvalidCharReplacement = '_';

            const string RegexCharStart = "[";
            const string RegexCharEnd = "]";

            const int UnitScaleFactor = 100;

            public const string PACKAGE_UI_NAME = "FBX Exporter";

            public enum ExportFormat
            {
                Binary = 0,
                ASCII = 1
            }

            /// <summary>
            /// name of the scene's default camera
            /// </summary>
            private static string DefaultCamera = "";

            /// <summary>
            /// name prefix for custom properties
            /// </summary>
            const string NamePrefix = "Unity_";

            private static string MakeName (string basename)
            {
                return NamePrefix + basename;
            }

            /// <summary>
            /// Create instance of exporter.
            /// </summary>
            static ModelExporter Create ()
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
            /// Map the name of a prefab to an FbxMesh (for preserving instances) 
            /// </summary>
            Dictionary<string, FbxMesh> SharedMeshes = new Dictionary<string, FbxMesh>();

            /// <summary>
            /// Map for the Name of an Object to number of objects with this name.
            /// Used for enforcing unique names on export.
            /// </summary>
            Dictionary<string, int> NameToIndexMap = new Dictionary<string, int> ();

            /// <summary>
            /// Format for creating unique names
            /// </summary>
            const string UniqueNameFormat = "{0}_{1}";

            /// <summary>
            /// Gets the export settings.
            /// </summary>
            public static EditorTools.ExportSettings ExportSettings {
                get { return EditorTools.ExportSettings.instance; }
            }

            /// <summary>
            /// Gets the Unity default material.
            /// </summary>
            public static Material DefaultMaterial {
                get {
                    if (!s_defaultMaterial) {
                        var obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        s_defaultMaterial = obj.GetComponent<Renderer>().sharedMaterial;
                        Object.DestroyImmediate(obj);
                    }
                    return s_defaultMaterial;
                }
            }
            static Material s_defaultMaterial = null;

            static Dictionary<UnityEngine.LightType, FbxLight.EType> MapLightType = new Dictionary<UnityEngine.LightType, FbxLight.EType> () {
                { UnityEngine.LightType.Directional,    FbxLight.EType.eDirectional },
                { UnityEngine.LightType.Spot,           FbxLight.EType.eSpot },
                { UnityEngine.LightType.Point,          FbxLight.EType.ePoint },
                { UnityEngine.LightType.Area,           FbxLight.EType.eArea },
            };

            /// <summary>
            /// Gets the version number of the FbxExporters plugin from the readme.
            /// </summary>
            public static string GetVersionFromReadme()
            {
                if (string.IsNullOrEmpty (ReadmeRelativePath)) {
                    Debug.LogWarning ("Missing relative path to README");
                    return null;
                }
                string absPath = Path.Combine (Application.dataPath, ReadmeRelativePath);
                if (!File.Exists (absPath)) {
                    Debug.LogWarning (string.Format("Could not find README.txt at: {0}", absPath));
                    return null;
                }

                try{
                    var versionHeader = "VERSION:";
                    var lines = File.ReadAllLines (absPath);
                    foreach (var line in lines) {
                        if (line.StartsWith(versionHeader)) {
                            var version = line.Replace (versionHeader, "");
                            return version.Trim ();
                        }
                    }
                }
                catch(IOException e){
                    Debug.LogWarning (string.Format("Error will reading file {0} ({1})", absPath, e));
                    return null;
                }
                Debug.LogWarning (string.Format("Could not find version number in README.txt at: {0}", absPath));
                return null;
            }

            /// <summary>
            /// Get a layer (to store UVs, normals, etc) on the mesh.
            /// If it doesn't exist yet, create it.
            /// </summary>
            public static FbxLayer GetOrCreateLayer(FbxMesh fbxMesh, int layer = 0 /* default layer */)
            {
                int maxLayerIndex = fbxMesh.GetLayerCount() - 1;
                while (layer > maxLayerIndex) {
                    // We'll have to create the layer (potentially several).
                    // Make sure to avoid infinite loops even if there's an
                    // FbxSdk bug.
                    int newLayerIndex = fbxMesh.CreateLayer();
                    if (newLayerIndex <= maxLayerIndex) {
                        // Error!
                        throw new System.Exception(
                                "Internal error: Unable to create mesh layer "
                                + (maxLayerIndex + 1)
                                + " on mesh " + fbxMesh.GetName());
                    }
                    maxLayerIndex = newLayerIndex;
                }
                return fbxMesh.GetLayer (layer);
            }

            /// <summary>
            /// Export the mesh's attributes using layer 0.
            /// </summary>
            private bool ExportComponentAttributes (MeshInfo mesh, FbxMesh fbxMesh, int[] unmergedTriangles)
            {
                // return true if any attribute was exported
                bool exportedAttribute = false;

                // Set the normals on Layer 0.
                FbxLayer fbxLayer = GetOrCreateLayer(fbxMesh);

                if (mesh.HasValidNormals()) {
                    using (var fbxLayerElement = FbxLayerElementNormal.Create (fbxMesh, "Normals")) {
                        fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                        fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eDirect);

                        // Add one normal per each vertex face index (3 per triangle)
                        FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                        for (int n = 0; n < unmergedTriangles.Length; n++) {
                            int unityTriangle = unmergedTriangles [n];
                            fbxElementArray.Add (ConvertNormalToRightHanded (mesh.Normals [unityTriangle]));
                        }

                        fbxLayer.SetNormals (fbxLayerElement);
                    }
                    exportedAttribute = true;
                }

                /// Set the binormals on Layer 0.
                if (mesh.HasValidBinormals()) {
                    using (var fbxLayerElement = FbxLayerElementBinormal.Create (fbxMesh, "Binormals")) {
                        fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                        fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eDirect);

                        // Add one normal per each vertex face index (3 per triangle)
                        FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                        for (int n = 0; n < unmergedTriangles.Length; n++) {
                            int unityTriangle = unmergedTriangles [n];
                            fbxElementArray.Add (ConvertNormalToRightHanded (mesh.Binormals [unityTriangle]));
                        }
                        fbxLayer.SetBinormals (fbxLayerElement);
                    }
                    exportedAttribute = true;
                }

                /// Set the tangents on Layer 0.
                if (mesh.HasValidTangents()) {
                    using (var fbxLayerElement = FbxLayerElementTangent.Create (fbxMesh, "Tangents")) {
                        fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                        fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eDirect);

                        // Add one normal per each vertex face index (3 per triangle)
                        FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                        for (int n = 0; n < unmergedTriangles.Length; n++) {
                            int unityTriangle = unmergedTriangles [n];
                            fbxElementArray.Add (ConvertNormalToRightHanded (
                                new Vector3 (
                                    mesh.Tangents [unityTriangle] [0],
                                    mesh.Tangents [unityTriangle] [1],
                                    mesh.Tangents [unityTriangle] [2]
                                )));
                        }
                        fbxLayer.SetTangents (fbxLayerElement);
                    }
                    exportedAttribute = true;
                }

                exportedAttribute |= ExportUVs (fbxMesh, mesh, unmergedTriangles);

                if (mesh.HasValidVertexColors()) {
                    using (var fbxLayerElement = FbxLayerElementVertexColor.Create (fbxMesh, "VertexColors")) {
                        fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                        fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eIndexToDirect);

                        // set texture coordinates per vertex
                        FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                        // (Uni-31596) only copy unique UVs into this array, and index appropriately
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

                        for (int i = 0; i < unmergedTriangles.Length; i++) {
                            fbxIndexArray.SetAt (i, unmergedTriangles [i]);
                        }
                        fbxLayer.SetVertexColors (fbxLayerElement);
                    }
                    exportedAttribute = true;
                }
                return exportedAttribute;
            }

            /// <summary>
            /// Unity has up to 4 uv sets per mesh. Export all the ones that exist.
            /// </summary>
            /// <param name="fbxMesh">Fbx mesh.</param>
            /// <param name="mesh">Mesh.</param>
            /// <param name="unmergedTriangles">Unmerged triangles.</param>
            private static bool ExportUVs(FbxMesh fbxMesh, MeshInfo mesh, int[] unmergedTriangles)
            {
                Vector2[][] uvs = new Vector2[][] {
                    mesh.UV,
                    mesh.mesh.uv2,
                    mesh.mesh.uv3,
                    mesh.mesh.uv4
                };

                int k = 0;
                for (int i = 0; i < uvs.Length; i++) {
                    if (uvs [i] == null || uvs [i].Length == 0) {
                        continue; // don't have these UV's, so skip
                    }

                    FbxLayer fbxLayer = GetOrCreateLayer (fbxMesh, k);
                    using (var fbxLayerElement = FbxLayerElementUV.Create (fbxMesh, "UVSet" + i))
                    {
                        fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygonVertex);
                        fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eIndexToDirect);

                        // set texture coordinates per vertex
                        FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray ();

                        // (Uni-31596) only copy unique UVs into this array, and index appropriately
                        for (int n = 0; n < uvs[i].Length; n++) {
                            fbxElementArray.Add (new FbxVector2 (uvs[i] [n] [0],
                                uvs[i] [n] [1]));
                        }

                        // For each face index, point to a texture uv
                        FbxLayerElementArray fbxIndexArray = fbxLayerElement.GetIndexArray ();
                        fbxIndexArray.SetCount (unmergedTriangles.Length);

                        for(int j = 0; j < unmergedTriangles.Length; j++){
                            fbxIndexArray.SetAt (j, unmergedTriangles [j]);
                        }
                        fbxLayer.SetUVs (fbxLayerElement, FbxLayerElement.EType.eTextureDiffuse);
                    }
                    k++;
                }

                // if we incremented k, then at least on set of UV's were exported
                return k > 0;
            }

            /// <summary>
            /// Takes in a left-handed UnityEngine.Vector3 denoting a normal,
            /// returns a right-handed FbxVector4.
            ///
            /// Unity is left-handed, Maya and Max are right-handed.
            /// The FbxSdk conversion routines can't handle changing handedness.
            ///
            /// Remember you also need to flip the winding order on your polygons.
            /// </summary>
            public static FbxVector4 ConvertNormalToRightHanded(Vector3 leftHandedVector)
            {
                // negating the x component of the vector converts it from left to right handed coordinates
                return new FbxVector4 (
                    -leftHandedVector[0],
                    leftHandedVector[1],
                    leftHandedVector[2]);
            }

            /// <summary>
            /// Takes in a left-handed UnityEngine.Vector3 denoting a position,
            /// returns a right-handed FbxVector4.
            ///
            /// Unity is left-handed, Maya and Max are right-handed.
            /// The FbxSdk conversion routines can't handle changing handedness.
            ///
            /// Remember you also need to flip the winding order on your polygons.
            /// </summary>
            public static FbxVector4 ConvertPositionToRightHanded(Vector3 leftHandedVector)
            {
                return UnitScaleFactor * ConvertNormalToRightHanded(leftHandedVector);
            }

            /// <summary>
            /// Exports a texture from Unity to FBX.
            /// The texture must be a property on the unityMaterial; it gets
            /// linked to the FBX via a property on the fbxMaterial.
            ///
            /// The texture file must be a file on disk; it is not embedded within the FBX.
            /// </summary>
            /// <param name="unityMaterial">Unity material.</param>
            /// <param name="unityPropName">Unity property name, e.g. "_MainTex".</param>
            /// <param name="fbxMaterial">Fbx material.</param>
            /// <param name="fbxPropName">Fbx property name, e.g. <c>FbxSurfaceMaterial.sDiffuse</c>.</param>
            public bool ExportTexture (Material unityMaterial, string unityPropName,
                                       FbxSurfaceMaterial fbxMaterial, string fbxPropName)
            {
                if (!unityMaterial) {
                    return false;
                }

                // Get the texture on this property, if any.
                if (!unityMaterial.HasProperty (unityPropName)) {
                    return false;
                }
                var unityTexture = unityMaterial.GetTexture (unityPropName);
                if (!unityTexture) {
                    return false;
                }

                // Find its filename
                var textureSourceFullPath = AssetDatabase.GetAssetPath (unityTexture);
                if (textureSourceFullPath == "") {
                    return false;
                }

                // get absolute filepath to texture
                textureSourceFullPath = Path.GetFullPath (textureSourceFullPath);

                if (Verbose) {
                    Debug.Log (string.Format ("{2}.{1} setting texture path {0}", textureSourceFullPath, fbxPropName, fbxMaterial.GetName ()));
                }

                // Find the corresponding property on the fbx material.
                var fbxMaterialProperty = fbxMaterial.FindProperty (fbxPropName);
                if (fbxMaterialProperty == null || !fbxMaterialProperty.IsValid ()) {
                    Debug.Log ("property not found");
                    return false;
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

                return true;
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
            public bool ExportMaterial (Material unityMaterial, FbxScene fbxScene, FbxNode fbxNode)
            {
                if (!unityMaterial) {
                    unityMaterial = DefaultMaterial;
                }

                var unityName = unityMaterial.name;
                if (MaterialMap.ContainsKey (unityName)) {
                    fbxNode.AddMaterial (MaterialMap [unityName]);
                    return true;
                }

                var fbxName = ExportSettings.mayaCompatibleNames
                    ? ConvertToMayaCompatibleName(unityName) : unityName;

                if (Verbose) {
                    if (unityName != fbxName) {
                        Debug.Log (string.Format ("exporting material {0} as {1}", unityName, fbxName));
                    } else {
                        Debug.Log(string.Format("exporting material {0}", unityName));
                    }
                }

                // We'll export either Phong or Lambert. Phong if it calls
                // itself specular, Lambert otherwise.
                var shader = unityMaterial.shader;
                bool specular = shader.name.ToLower ().Contains ("specular");

                var fbxMaterial = specular
                    ? FbxSurfacePhong.Create (fbxScene, fbxName)
                    : FbxSurfaceLambert.Create (fbxScene, fbxName);

                // Copy the flat colours over from Unity standard materials to FBX.
                fbxMaterial.Diffuse.Set (GetMaterialColor (unityMaterial, "_Color"));
                fbxMaterial.Emissive.Set (GetMaterialColor (unityMaterial, "_EmissionColor", 0));
                fbxMaterial.Ambient.Set (new FbxDouble3 ());

                fbxMaterial.BumpFactor.Set (unityMaterial.HasProperty ("_BumpScale") ? unityMaterial.GetFloat ("_BumpScale") : 0);

                if (specular) {
                    (fbxMaterial as FbxSurfacePhong).Specular.Set (GetMaterialColor (unityMaterial, "_SpecColor"));
                }

                // Export the textures from Unity standard materials to FBX.
                ExportTexture (unityMaterial, "_MainTex", fbxMaterial, FbxSurfaceMaterial.sDiffuse);
                ExportTexture (unityMaterial, "_EmissionMap", fbxMaterial, FbxSurfaceMaterial.sEmissive);
                ExportTexture (unityMaterial, "_BumpMap", fbxMaterial, FbxSurfaceMaterial.sNormalMap);
                if (specular) {
                    ExportTexture (unityMaterial, "_SpecGlosMap", fbxMaterial, FbxSurfaceMaterial.sSpecular);
                }

                MaterialMap.Add (unityName, fbxMaterial);
                fbxNode.AddMaterial (fbxMaterial);
                return true;
            }

            /// <summary>
            /// Sets up the material to polygon mapping for fbxMesh.
            /// To determine which part of the mesh uses which material, look at the submeshes
            /// and which polygons they represent.
            /// Assuming equal number of materials as submeshes, and that they are in the same order.
            /// (i.e. submesh 1 uses material 1)
            /// </summary>
            /// <param name="fbxMesh">Fbx mesh.</param>
            /// <param name="mesh">Mesh.</param>
            /// <param name="materials">Materials.</param>
            private void AssignLayerElementMaterial(FbxMesh fbxMesh, Mesh mesh, int materialCount)
            {
                // Add FbxLayerElementMaterial to layer 0 of the node
                FbxLayer fbxLayer = fbxMesh.GetLayer (0 /* default layer */);
                if (fbxLayer == null) {
                    fbxMesh.CreateLayer ();
                    fbxLayer = fbxMesh.GetLayer (0 /* default layer */);
                }

                using (var fbxLayerElement = FbxLayerElementMaterial.Create (fbxMesh, "Material")) {
                    // if there is only one material then set everything to that material
                    if (materialCount == 1) {
                        fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eAllSame);
                        fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eIndexToDirect);

                        FbxLayerElementArray fbxElementArray = fbxLayerElement.GetIndexArray ();
                        fbxElementArray.Add (0);
                    } else {
                        fbxLayerElement.SetMappingMode (FbxLayerElement.EMappingMode.eByPolygon);
                        fbxLayerElement.SetReferenceMode (FbxLayerElement.EReferenceMode.eIndexToDirect);

                        FbxLayerElementArray fbxElementArray = fbxLayerElement.GetIndexArray ();

                        for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++) {
                            var topology = mesh.GetTopology (subMeshIndex);
                            int polySize;

                            switch (topology) {
                            case MeshTopology.Triangles:
                                polySize = 3;
                                break;
                            case MeshTopology.Quads:
                                polySize = 4;
                                break;
                            case MeshTopology.Lines:
                                throw new System.NotImplementedException();
                            case MeshTopology.Points:
                                throw new System.NotImplementedException();
                            case MeshTopology.LineStrip:
                                throw new System.NotImplementedException();
                            default:
                                throw new System.NotImplementedException ();
                            }

                            // Specify the material index for each polygon.
                            // Material index should match subMeshIndex.
                            var indices = mesh.GetIndices (subMeshIndex);
                            for(int j = 0, n = indices.Length / polySize; j < n; j++){
                                fbxElementArray.Add (subMeshIndex);
                            }
                        }
                    }
                    fbxLayer.SetMaterials (fbxLayerElement);
                }
            }

            /// <summary>
            /// Exports a unity mesh and attaches it to the node as an FbxMesh.
            ///
            /// Able to export materials per sub-mesh as well (by default, exports with the default material).
            ///
            /// Use fbxNode.GetMesh() to access the exported mesh.
            /// </summary>
            public bool ExportMesh (Mesh mesh, FbxNode fbxNode, Material[] materials = null)
            {
                var meshInfo = new MeshInfo(mesh, materials);
                return ExportMesh(meshInfo, fbxNode);
            }

            /// <summary>
            /// Exports a unity mesh and attaches it to the node as an FbxMesh.
            /// </summary>
            bool ExportMesh (MeshInfo meshInfo, FbxNode fbxNode)
            {
                if (!meshInfo.IsValid) {
                    return false;
                }

                NumMeshes++;
                NumTriangles += meshInfo.Triangles.Length / 3;

                // create the mesh structure.
                var fbxScene = fbxNode.GetScene();
                FbxMesh fbxMesh = FbxMesh.Create (fbxScene, "Scene");

                // Create control points.
                Dictionary<Vector3, int> ControlPointToIndex = new Dictionary<Vector3, int> ();
                {
                    var vertices = meshInfo.Vertices;
                    for (int v = 0, n = meshInfo.VertexCount; v < n; v++) {
                        if (ControlPointToIndex.ContainsKey (vertices [v])) {
                            continue;
                        }
                        ControlPointToIndex [vertices [v]] = ControlPointToIndex.Count();
                    }
                    fbxMesh.InitControlPoints (ControlPointToIndex.Count());

                    foreach (var kvp in ControlPointToIndex) {
                        var controlPoint = kvp.Key;
                        var index = kvp.Value;
                        fbxMesh.SetControlPointAt (ConvertPositionToRightHanded(controlPoint), index);
                    }
                }

                var unmergedPolygons = new List<int> ();
                var mesh = meshInfo.mesh;
                for (int s = 0; s < mesh.subMeshCount; s++) {
                    var topology = mesh.GetTopology (s);
                    var indices = mesh.GetIndices (s);

                    int polySize;
                    int[] vertOrder;

                    switch (topology) {
                    case MeshTopology.Triangles:
                        polySize = 3;
                        // flip winding order so that Maya and Unity import it properly
                        vertOrder = new int[]{ 0, 2, 1 };
                        break;
                    case MeshTopology.Quads:
                        polySize = 4;
                        // flip winding order so that Maya and Unity import it properly
                        vertOrder = new int[]{ 0, 3, 2, 1 };
                        break;
                    case MeshTopology.Lines:
                        throw new System.NotImplementedException();
                    case MeshTopology.Points:
                        throw new System.NotImplementedException();
                    case MeshTopology.LineStrip:
                        throw new System.NotImplementedException();
                    default: 
                        throw new System.NotImplementedException ();
                    }

                    for (int f = 0; f < indices.Length / polySize; f++) {
                        fbxMesh.BeginPolygon ();

                        foreach (int val in vertOrder) {
                            int polyVert = indices [polySize * f + val];

                            // Save the polygon order (without merging vertices) so we
                            // properly export UVs, normals, binormals, etc.
                            unmergedPolygons.Add(polyVert);

                            polyVert = ControlPointToIndex [meshInfo.Vertices [polyVert]];
                            fbxMesh.AddPolygon (polyVert);

                        }
                        fbxMesh.EndPolygon ();
                    }
                }

                // Set up materials per submesh.
                foreach (var mat in meshInfo.Materials) {
                    ExportMaterial (mat, fbxScene, fbxNode);
                }
                AssignLayerElementMaterial (fbxMesh, meshInfo.mesh, meshInfo.Materials.Length);

                // Set up normals, etc.
                ExportComponentAttributes (meshInfo, fbxMesh, unmergedPolygons.ToArray());

                // set the fbxNode containing the mesh
                fbxNode.SetNodeAttribute (fbxMesh);
                fbxNode.SetShadingMode (FbxNode.EShadingMode.eWireFrame);
                return true;
            }

            /// <summary>
            /// Takes a Quaternion and returns a Euler with XYZ rotation order.
            /// Also converts from left (Unity) to righthanded (Maya) coordinates.
            /// 
            /// Note: Cannot simply use the FbxQuaternion.DecomposeSphericalXYZ()
            ///       function as this returns the angle in spherical coordinates 
            ///       instead of Euler angles, which Maya does not import properly. 
            /// </summary>
            /// <returns>Euler with XYZ rotation order.</returns>
            public static FbxDouble3 ConvertQuaternionToXYZEuler(Quaternion q)
            {
                FbxQuaternion quat = new FbxQuaternion (q.x, q.y, q.z, q.w);
                FbxAMatrix m = new FbxAMatrix ();
                m.SetQ (quat);
                var vector4 = m.GetR ();

                // Negate the y and z values of the rotation to convert 
                // from Unity to Maya coordinates (left to righthanded).
                var result = new FbxDouble3 (vector4.X, -vector4.Y, -vector4.Z);

                return result;
            }

            // get a fbxNode's global default position.
            protected bool ExportTransform (UnityEngine.Transform unityTransform, FbxNode fbxNode, Vector3 newCenter, TransformExportType exportType)
            {
                // Fbx rotation order is XYZ, but Unity rotation order is ZXY.
                // This causes issues when converting euler to quaternion, causing the final
                // rotation to be slighlty off.
                // Fixed by exporting the rotations as eulers with XYZ rotation order.
                fbxNode.SetRotationOrder (FbxNode.EPivotSet.eSourcePivot, FbxEuler.EOrder.eOrderXYZ);

                UnityEngine.Vector3 unityTranslate;
                FbxDouble3 fbxRotate;
                UnityEngine.Vector3 unityScale;

                switch (exportType) {
                case TransformExportType.Reset:
                    unityTranslate = Vector3.zero;
                    fbxRotate = new FbxDouble3 (0);
                    unityScale = Vector3.one;
                    break;
                case TransformExportType.Global:
                    unityTranslate = GetRecenteredTranslation(unityTransform, newCenter);
                    fbxRotate = ConvertQuaternionToXYZEuler(unityTransform.rotation);
                    unityScale = unityTransform.lossyScale;
                    break;
                default: /*case TransformExportType.Local*/
                    unityTranslate = unityTransform.localPosition;
                    fbxRotate = ConvertQuaternionToXYZEuler(unityTransform.localRotation);
                    unityScale = unityTransform.localScale;
                    break;
                }

                // Transfer transform data from Unity to Fbx
                var fbxTranslate = ConvertPositionToRightHanded(unityTranslate);
                var fbxScale = new FbxDouble3 (unityScale.x, unityScale.y, unityScale.z);

                // set the local position of fbxNode
                fbxNode.LclTranslation.Set (new FbxDouble3(fbxTranslate.X, fbxTranslate.Y, fbxTranslate.Z));
                fbxNode.LclRotation.Set (fbxRotate);
                fbxNode.LclScaling.Set (fbxScale);

                return true;
            }

            /// <summary>
            /// if this game object is a model prefab then export with shared components
            /// </summary>
            protected bool ExportInstance (GameObject unityGo, FbxNode fbxNode, FbxScene fbxScene)
            {
                PrefabType unityPrefabType = PrefabUtility.GetPrefabType(unityGo);

                if (unityPrefabType != PrefabType.PrefabInstance) return false;

                Object unityPrefabParent = PrefabUtility.GetPrefabParent (unityGo);

                if (Verbose)
                    Debug.Log (string.Format ("exporting instance {0}({1})", unityGo.name, unityPrefabParent.name));

                FbxMesh fbxMesh = null;

                if (!SharedMeshes.TryGetValue (unityPrefabParent.name, out fbxMesh))
                {
                    if (ExportMesh (unityGo, fbxNode) && fbxNode.GetMesh() != null) {
                        SharedMeshes [unityPrefabParent.name] = fbxNode.GetMesh ();
                        return true;
                    }
                }

                if (fbxMesh == null) return false;

                // set the fbxNode containing the mesh
                fbxNode.SetNodeAttribute (fbxMesh);
                fbxNode.SetShadingMode (FbxNode.EShadingMode.eWireFrame);

                return true;
            }

            /// <summary>
            /// Exports camera component
            /// </summary>
            protected bool ExportCamera (GameObject unityGO, FbxScene fbxScene, FbxNode fbxNode)
            {
                Camera unityCamera = unityGO.GetComponent<Camera> ();
                if (unityCamera == null) {
                    return false;
                }

                FbxCamera fbxCamera = FbxCamera.Create (fbxScene.GetFbxManager(), unityCamera.name);
                if (fbxCamera == null) {
                    return false;
                }

                float aspectRatio = unityCamera.aspect;

                #region Configure Film Camera from Game Camera
                // Configure FilmBack settings: 35mm TV Projection (0.816 x 0.612)
                float apertureHeightInInches = 0.612f;
                float apertureWidthInInches = aspectRatio * apertureHeightInInches;

                FbxCamera.EProjectionType projectionType =
                    unityCamera.orthographic ? FbxCamera.EProjectionType.eOrthogonal : FbxCamera.EProjectionType.ePerspective;

                fbxCamera.ProjectionType.Set(projectionType);
                fbxCamera.FilmAspectRatio.Set(aspectRatio);
                fbxCamera.SetApertureWidth (apertureWidthInInches);
                fbxCamera.SetApertureHeight (apertureHeightInInches);
                fbxCamera.SetApertureMode (FbxCamera.EApertureMode.eVertical);

                // Focal Length
                fbxCamera.FocalLength.Set(fbxCamera.ComputeFocalLength (unityCamera.fieldOfView));

                // Field of View
                fbxCamera.FieldOfView.Set (unityCamera.fieldOfView);

                // NearPlane
                fbxCamera.SetNearPlane (unityCamera.nearClipPlane*UnitScaleFactor);

                // FarPlane
                fbxCamera.SetFarPlane (unityCamera.farClipPlane*UnitScaleFactor);
                #endregion

                fbxNode.SetNodeAttribute (fbxCamera);

                // set +90 post rotation to counteract for FBX camera's facing +X direction by default
                fbxNode.SetPostRotation(FbxNode.EPivotSet.eSourcePivot, new FbxVector4(0,90,0));
                // have to set rotation active to true in order for post rotation to be applied
                fbxNode.SetRotationActive (true);

                // make the last camera exported the default camera
                DefaultCamera = fbxNode.GetName ();

                return true;
            }

            /// <summary>
            /// Exports light component.
            /// Supported types: point, spot and directional
            /// Cookie => Gobo
            /// </summary>
            protected bool ExportLight (GameObject unityGo, FbxScene fbxScene, FbxNode fbxNode)
            {
                Light unityLight = unityGo.GetComponent<Light> ();

                if (unityLight == null)
                    return false;

                FbxLight.EType fbxLightType;

                // Is light type supported?
                if (!MapLightType.TryGetValue (unityLight.type, out fbxLightType))
                    return false;
                
                FbxLight fbxLight = FbxLight.Create (fbxScene.GetFbxManager (), unityLight.name);

                // Set the type of the light.      
                fbxLight.LightType.Set(fbxLightType);

                switch (unityLight.type) 
                {
                case LightType.Directional : {
                        break;
                    }
                case LightType.Spot : {
                        // Set the angle of the light's spotlight cone in degrees.
                        fbxLight.InnerAngle.Set(unityLight.spotAngle);
                        fbxLight.OuterAngle.Set(unityLight.spotAngle);
                        break;
                    }
                case LightType.Point : {
                        break;
                    }
                case LightType.Area : {
                        // TODO: areaSize: The size of the area light by scaling the node XY
                        break;
                    }
                }
                // The color of the light.
                var unityLightColor = unityLight.color;
                fbxLight.Color.Set (new FbxDouble3(unityLightColor.r, unityLightColor.g, unityLightColor.b));

                // Set the Intensity of a light is multiplied with the Light color.
                fbxLight.Intensity.Set (unityLight.intensity * UnitScaleFactor /*compensate for Maya scaling by system units*/ );

                // Set the range of the light.
                // applies-to: Point & Spot
                // => FarAttenuationStart, FarAttenuationEnd
                fbxLight.FarAttenuationStart.Set (0.01f /* none zero start */);
                fbxLight.FarAttenuationEnd.Set(unityLight.range*UnitScaleFactor);

                // shadows           Set how this light casts shadows
                // applies-to: Point & Spot
                bool unityLightCastShadows = unityLight.shadows != LightShadows.None;
                fbxLight.CastShadows.Set (unityLightCastShadows);

                fbxNode.SetNodeAttribute (fbxLight);

                // set +90 post rotation on x to counteract for FBX light's facing -Y direction by default
                fbxNode.SetPostRotation(FbxNode.EPivotSet.eSourcePivot, new FbxVector4(90,0,0));
                // have to set rotation active to true in order for post rotation to be applied
                fbxNode.SetRotationActive (true);

                return true;
            }

            /// <summary>
            /// Export an AnimationCurve.
            /// NOTE: This is not used for rotations, because we need to convert from
            /// quaternion to euler and various other stuff.
            /// </summary>
            protected void ExportAnimCurve(UnityEngine.Object unityObj,
                AnimationCurve unityAnimCurve,
                string unityPropertyName,
                FbxScene fbxScene,
                FbxAnimLayer fbxAnimLayer)
            {
                FbxPropertyChannelPair fbxPropertyChannelPair;
                if (!FbxPropertyChannelPair.TryGetValue(unityPropertyName, out fbxPropertyChannelPair))
                {
                    Debug.LogWarning(string.Format("no mapping from Unity '{0}' to fbx property", unityPropertyName));
                    return;
                }

                GameObject unityGo = GetGameObject(unityObj);
                if (unityGo == null && !unityObj)
                {
                    Debug.LogError(string.Format("cannot find gameobject for {0}", unityObj.ToString()));
                    return;
                }

                FbxNode fbxNode;
                if (!MapUnityObjectToFbxNode.TryGetValue(unityGo, out fbxNode))
                {
                    Debug.LogError(string.Format("no fbx node for {0}", unityGo.ToString()));
                    return;
                }
                // map unity property name to fbx property
                var fbxProperty = fbxNode.FindProperty(fbxPropertyChannelPair.Property, false);
                if (!fbxProperty.IsValid())
                {
                    var fbxNodeAttribute = fbxNode.GetNodeAttribute();
                    if (fbxNodeAttribute != null)
                    {
                        fbxProperty = fbxNodeAttribute.FindProperty(fbxPropertyChannelPair.Property, false);
                    }
                }
                if (!fbxProperty.IsValid())
                {
                    Debug.LogError(string.Format("no fbx property {0} found on {1} node or nodeAttribute ", fbxPropertyChannelPair.Property, fbxNode.GetName()));
                    return;
                }

                if (Verbose)
                {
                    Debug.Log("Exporting animation for " + unityObj.ToString() + " (" + unityPropertyName + ")");
                }

                FbxAnimCurve fbxAnimCurve;
                // Create the AnimCurve on the channel
                if (fbxPropertyChannelPair.Channel != null)
                {
                    fbxAnimCurve = fbxProperty.GetCurve(fbxAnimLayer, fbxPropertyChannelPair.Channel, true);
                }
                else
                {
                    fbxAnimCurve = fbxProperty.GetCurve(fbxAnimLayer, true);
                }
                // copy Unity AnimCurve to FBX AnimCurve.
                fbxAnimCurve.KeyModifyBegin();

                for (int keyIndex = 0, n = unityAnimCurve.length; keyIndex < n; ++keyIndex)
                {
                    var key = unityAnimCurve[keyIndex];

                    if (fbxProperty.GetName() == "Intensity")
                    {
                            key.value *= 100.0f;
                    }
                    var fbxTime = FbxTime.FromSecondDouble(key.time);
                    fbxAnimCurve.KeyAdd(fbxTime);
                    fbxAnimCurve.KeySet(keyIndex, fbxTime, key.value);
                }

                fbxAnimCurve.KeyModifyEnd();
            }

            /// <summary>
            /// Store FBX property name and channel name 
            /// </summary>
            struct FbxPropertyChannelPair {
                public string Property { get ; private set; }
                public string Channel { get ; private set; }
                public FbxPropertyChannelPair(string p, string c) {
                    Property = p;
                    Channel = c;
                }

                /// <summary>
                /// Map a Unity property name to the corresponding FBX property and
                /// channel names.
                /// </summary>
                public static bool TryGetValue(string unityPropertyName, out FbxPropertyChannelPair prop)
                {
                    System.StringComparison ct = System.StringComparison.CurrentCulture;

                    if (unityPropertyName.StartsWith ("m_LocalPosition.x", ct) || unityPropertyName.EndsWith ("T.x", ct)) {
                        prop = new FbxPropertyChannelPair ("Lcl Translation", Globals.FBXSDK_CURVENODE_COMPONENT_X);
                        return true;
                    }

                    if (unityPropertyName.StartsWith ("m_LocalPosition.y", ct) || unityPropertyName.EndsWith ("T.y", ct)) {
                        prop = new FbxPropertyChannelPair ("Lcl Translation", Globals.FBXSDK_CURVENODE_COMPONENT_Y);
                        return true;
                    }

                    if (unityPropertyName.StartsWith ("m_LocalPosition.z", ct) || unityPropertyName.EndsWith ("T.z", ct)) {
                        prop = new FbxPropertyChannelPair ("Lcl Translation", Globals.FBXSDK_CURVENODE_COMPONENT_Z);
                        return true;
                    }

                    if (unityPropertyName.StartsWith("m_Intensity", ct) || unityPropertyName.EndsWith("T.z", ct))
                    {
                        prop = new FbxPropertyChannelPair("Intensity", null);
                        return true;
                    }

                    if (unityPropertyName.StartsWith("m_SpotAngle", ct) || unityPropertyName.EndsWith("T.z", ct))
                    {
                        prop = new FbxPropertyChannelPair("OuterAngle", null);
                        return true;
                    }

                    if (unityPropertyName.StartsWith("m_Color", ct) || unityPropertyName.EndsWith("T.z", ct))
                    {
                        prop = new FbxPropertyChannelPair("Color", Globals.FBXSDK_CURVENODE_COLOR);
                        return true;
                    }

                    if (unityPropertyName.StartsWith("m_Color.r", ct) || unityPropertyName.EndsWith("T.z", ct))
                    {
                        prop = new FbxPropertyChannelPair("Color", Globals.FBXSDK_CURVENODE_COLOR_RED);
                        return true;
                    }

                    if (unityPropertyName.StartsWith("m_Color.g", ct) || unityPropertyName.EndsWith("T.z", ct))
                    {
                        prop = new FbxPropertyChannelPair("Color", Globals.FBXSDK_CURVENODE_COLOR_GREEN);
                        return true;
                    }

                    if (unityPropertyName.StartsWith("m_Color.b", ct) || unityPropertyName.EndsWith("T.z", ct))
                    {
                        prop = new FbxPropertyChannelPair("Color", Globals.FBXSDK_CURVENODE_COLOR_BLUE);
                        return true;
                    }

                    prop = new FbxPropertyChannelPair ();
                    return false;
                }
            }

            /// <summary>
            /// Exporting rotations is more complicated. We need to convert
            /// from quaternion to euler. We use this class to help.
            /// </summary>
            class QuaternionCurve {
                public AnimationCurve x;
                public AnimationCurve y;
                public AnimationCurve z;
                public AnimationCurve w;

                public struct Key {
                    public FbxTime time;
                    public FbxVector4 euler;
                }

                public QuaternionCurve() { }

                public static int GetQuaternionIndex(string unityPropertyName) {
                    System.StringComparison ct = System.StringComparison.CurrentCulture;
                    bool isQuaternionComponent = false;

                    isQuaternionComponent |= unityPropertyName.StartsWith ("m_LocalRotation.", ct);
                    isQuaternionComponent |= unityPropertyName.EndsWith ("Q.x", ct);
                    isQuaternionComponent |= unityPropertyName.EndsWith ("Q.y", ct);
                    isQuaternionComponent |= unityPropertyName.EndsWith ("Q.z", ct);
                    isQuaternionComponent |= unityPropertyName.EndsWith ("Q.w", ct);

                    if (!isQuaternionComponent) { return -1; }

                    switch(unityPropertyName[unityPropertyName.Length - 1]) {
                    case 'x': return 0;
                    case 'y': return 1;
                    case 'z': return 2;
                    case 'w': return 3;
                    default: return -1;
                    }
                }

                public void SetCurve(int i, AnimationCurve curve) {
                    switch(i) {
                    case 0: x = curve; break;
                    case 1: y = curve; break;
                    case 2: z = curve; break;
                    case 3: w = curve; break;
                    default: throw new System.IndexOutOfRangeException();
                    }
                }

                Key [] ComputeKeys(UnityEngine.Quaternion restRotation, FbxNode node) {
                    // Get the source pivot pre-rotation if any, so we can
                    // remove it from the animation we get from Unity.
                    var fbxPreRotationEuler = node.GetRotationActive() ? node.GetPreRotation(FbxNode.EPivotSet.eSourcePivot)
                        : new FbxVector4();
                    var fbxPreRotationInverse = new FbxQuaternion();
                    fbxPreRotationInverse.ComposeSphericalXYZ(fbxPreRotationEuler);
                    fbxPreRotationInverse.Inverse();

                    // If we're only animating along certain coords for some
                    // reason, we'll need to fill in the other coords with the
                    // rest-pose value.
                    var lclQuaternion = new FbxQuaternion(restRotation.x, restRotation.y, restRotation.z, restRotation.w);

                    // Find when we have keys set.
                    var keyTimes = new HashSet<float>();
                    if (x != null) { foreach(var key in x.keys) { keyTimes.Add(key.time); } }
                    if (y != null) { foreach(var key in y.keys) { keyTimes.Add(key.time); } }
                    if (z != null) { foreach(var key in z.keys) { keyTimes.Add(key.time); } }
                    if (w != null) { foreach(var key in w.keys) { keyTimes.Add(key.time); } }

                    // Convert to the Key type.
                    var keys = new Key[keyTimes.Count];
                    int i = 0;
                    foreach(var seconds in keyTimes) {

                        // The final animation, including the effect of pre-rotation.
                        // If we have no curve, assume the node has the correct rotation right now.
                        // We need to evaluate since we might only have keys in one of the axes.
                        var fbxFinalAnimation = new FbxQuaternion(
                            (x == null) ? lclQuaternion[0] : x.Evaluate(seconds),
                            (y == null) ? lclQuaternion[1] : y.Evaluate(seconds),
                            (z == null) ? lclQuaternion[2] : z.Evaluate(seconds),
                            (w == null) ? lclQuaternion[3] : w.Evaluate(seconds));

                        // Cancel out the pre-rotation. Order matters. FBX reads left-to-right.
                        // When we run animation we will apply:
                        //      pre-rotation
                        //      then pre-rotation inverse
                        //      then animation.
                        var fbxAnimation = fbxPreRotationInverse * fbxFinalAnimation;

                        // Store the key so we can sort them later.
                        Key key;
                        key.time = FbxTime.FromSecondDouble(seconds);
                        key.euler = fbxAnimation.DecomposeSphericalXYZ();
                        keys[i++] = key;
                    }

                    // Sort the keys by time
                    System.Array.Sort(keys, (Key a, Key b) => a.time.CompareTo(b.time));

                    return keys;
                }

                public void Animate(Transform unityTransform, FbxNode fbxNode, FbxAnimLayer fbxAnimLayer, bool Verbose) {
                    /* Find or create the three curves. */
                    var x = fbxNode.LclRotation.GetCurve(fbxAnimLayer, Globals.FBXSDK_CURVENODE_COMPONENT_X, true);
                    var y = fbxNode.LclRotation.GetCurve(fbxAnimLayer, Globals.FBXSDK_CURVENODE_COMPONENT_Y, true);
                    var z = fbxNode.LclRotation.GetCurve(fbxAnimLayer, Globals.FBXSDK_CURVENODE_COMPONENT_Z, true);

                    /* set the keys */
                    x.KeyModifyBegin();
                    y.KeyModifyBegin();
                    z.KeyModifyBegin();

                    var keys = ComputeKeys(unityTransform.localRotation, fbxNode);
                    for(int i = 0, n = keys.Length; i < n; ++i) {
                        var key = keys[i];
                        x.KeyAdd(key.time);
                        x.KeySet(i, key.time, (float)key.euler.X);

                        y.KeyAdd(key.time);
                        y.KeySet(i, key.time, (float)key.euler.Y);

                        z.KeyAdd(key.time);
                        z.KeySet(i, key.time, (float)key.euler.Z);
                    }

                    z.KeyModifyEnd();
                    y.KeyModifyEnd();
                    x.KeyModifyEnd();

                    if (Verbose) {
                        Debug.Log("Exported rotation animation for " + fbxNode.GetName());
                    }
                }
            }

            /// <summary>
            /// Export an AnimationClip as a single take
            /// </summary>
            protected void ExportAnimationClip (AnimationClip unityAnimClip, GameObject unityRoot, FbxScene fbxScene)
            {
                if (!unityAnimClip) return;

                if (Verbose)
                    Debug.Log (string.Format ("exporting clip {1} for {0}", unityRoot.name, unityAnimClip.name));

                // setup anim stack
                FbxAnimStack fbxAnimStack = FbxAnimStack.Create (fbxScene, unityAnimClip.name);
                fbxAnimStack.Description.Set ("Animation Take: " + unityAnimClip.name);

                // add one mandatory animation layer
                FbxAnimLayer fbxAnimLayer = FbxAnimLayer.Create (fbxScene, "Animation Base Layer");
                fbxAnimStack.AddMember (fbxAnimLayer);

                // Set up the FPS so our frame-relative math later works out
                // Custom frame rate isn't really supported in FBX SDK (there's
                // a bug), so try hard to find the nearest time mode.
                FbxTime.EMode timeMode = FbxTime.EMode.eCustom;
                double precision = 1e-6;
                while (timeMode == FbxTime.EMode.eCustom && precision < 1000) {
                    timeMode = FbxTime.ConvertFrameRateToTimeMode (unityAnimClip.frameRate, precision);
                    precision *= 10;
                }
                if (timeMode == FbxTime.EMode.eCustom) {
                    timeMode = FbxTime.EMode.eFrames30;
                }
                FbxTime.SetGlobalTimeMode (timeMode);

                // set time correctly
                var fbxStartTime = FbxTime.FromSecondDouble (0);
                var fbxStopTime = FbxTime.FromSecondDouble (unityAnimClip.length);

                fbxAnimStack.SetLocalTimeSpan (new FbxTimeSpan (fbxStartTime, fbxStopTime));

                /* The major difficulty: Unity uses quaternions for rotation
                 * (which is how it should be) but FBX uses euler angles. So we
                 * need to gather up the list of transform curves per object. */
                var quaternions = new Dictionary<UnityEngine.GameObject, QuaternionCurve> ();

                foreach (EditorCurveBinding unityCurveBinding in AnimationUtility.GetCurveBindings (unityAnimClip)) {
                    Object unityObj = AnimationUtility.GetAnimatedObject (unityRoot, unityCurveBinding);
                    if (!unityObj) { continue; }

                    AnimationCurve unityAnimCurve = AnimationUtility.GetEditorCurve (unityAnimClip, unityCurveBinding);
                    if (unityAnimCurve == null) { continue; }

                    int index = QuaternionCurve.GetQuaternionIndex (unityCurveBinding.propertyName);
                    if (index == -1) {
                        if (Verbose)
                            Debug.Log (string.Format ("export binding {1} for {0}", unityCurveBinding.propertyName, unityObj.ToString ()));

                        /* Some normal property (e.g. translation), export right away */
                        ExportAnimCurve (unityObj, unityAnimCurve, unityCurveBinding.propertyName,
                            fbxScene, fbxAnimLayer);
                    } else {
                        /* Rotation property; save it to convert quaternion -> euler later. */

                        var unityGo = GetGameObject (unityObj);
                        if (!unityGo) { continue; }

                        QuaternionCurve quat;
                        if (!quaternions.TryGetValue (unityGo, out quat)) {
                            quat = new QuaternionCurve ();
                            quaternions.Add (unityGo, quat);
                        }
                        quat.SetCurve (index, unityAnimCurve);
                    }
                }

                /* now export all the quaternion curves */
                foreach (var kvp in quaternions) {
                    var unityGo = kvp.Key;
                    var quat = kvp.Value;

                    FbxNode fbxNode;
                    if (!MapUnityObjectToFbxNode.TryGetValue (unityGo, out fbxNode)) {
                        Debug.LogError (string.Format ("no fbxnode found for '0'", unityGo.name));
                        continue;
                    }
                    quat.Animate (unityGo.transform, fbxNode, fbxAnimLayer, Verbose);
                }
            }

            /// <summary>
            /// Export the Animator component on this game object
            /// </summary>
            protected void ExportAnimation (GameObject unityRoot, FbxScene fbxScene)
            {
                var exported = new HashSet<AnimationClip> ();

                var animator = unityRoot.GetComponent<Animator> ();
                if (animator)
                { 
                    // Try the animator controller (mecanim)
                    var controller = animator.runtimeAnimatorController;

                    if (controller) 
                    { 
                        // Only export each clip once per game object.
                        foreach (var clip in controller.animationClips) {
                            if (exported.Add (clip)) {
                                ExportAnimationClip (clip, unityRoot, fbxScene);
                            }
                        }
                    }
                }

                // Try the playable director
                var director = unityRoot.GetComponent<UnityEngine.Playables.PlayableDirector> ();
                if (director)
                {
                    Debug.Log(string.Format("exporting animationclips from playabledirector on {0} not supported", unityRoot.name));
                    // TODO: export animationclips from playabledirector
                }

                // Try the animation (legacy)
                var animation = unityRoot.GetComponent<Animation> ();
                if (animation) 
                { 
                    // Only export each clip once per game object.
                    foreach (var animObj in animation) {
                        AnimationState animState = animObj as AnimationState;
                        if (animState)
                        {
                            AnimationClip clip = animState.clip;
                            if (exported.Add (clip)) {
                                ExportAnimationClip (clip, unityRoot, fbxScene);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// configures default camera for the scene
            /// </summary>
            protected void SetDefaultCamera (FbxScene fbxScene)
            {
                if (DefaultCamera == "")
                    DefaultCamera = Globals.FBXSDK_CAMERA_PERSPECTIVE;

                fbxScene.GetGlobalSettings ().SetDefaultCamera (DefaultCamera);
            }

            /// <summary>
            /// Ensures that the inputted name is unique.
            /// If a duplicate name is found, then it is incremented.
            /// e.g. Sphere becomes Sphere_1
            /// </summary>
            /// <returns>Unique name</returns>
            /// <param name="name">Name</param>
            private string GetUniqueName(string name)
            {
                var uniqueName = name;
                if (NameToIndexMap.ContainsKey (name)) {
                    uniqueName = string.Format (UniqueNameFormat, name, NameToIndexMap [name]);
                    NameToIndexMap [name]++;
                } else {
                    NameToIndexMap [name] = 1;
                }
                return uniqueName;
            }

            /// <summary>
            /// Unconditionally export components on this game object
            /// </summary>
            protected int ExportComponents (
                GameObject  unityGo, FbxScene fbxScene, FbxNode fbxNodeParent,
                int exportProgress, int objectCount, Vector3 newCenter,
                TransformExportType exportType = TransformExportType.Local)
            {
                int numObjectsExported = exportProgress;

                if (ExportSettings.mayaCompatibleNames) {
                    unityGo.name = ConvertToMayaCompatibleName (unityGo.name);
                }

                // create an FbxNode and add it as a child of parent
                FbxNode fbxNode = FbxNode.Create (fbxScene, GetUniqueName (unityGo.name));
                MapUnityObjectToFbxNode [unityGo] = fbxNode;

                numObjectsExported++;
                if (EditorUtility.DisplayCancelableProgressBar (
                        ProgressBarTitle,
                        string.Format ("Creating FbxNode {0}/{1}", numObjectsExported, objectCount),
                        (numObjectsExported / (float)objectCount) * 0.5f)) {
                    // cancel silently
                    return -1;
                }

                // Default inheritance type in FBX is RrSs, which causes scaling issues in Maya as
                // both Maya and Unity use RSrs inheritance by default.
                // Note: MotionBuilder uses RrSs inheritance by default as well, though it is possible
                //       to select a different inheritance type in the UI.
                // Use RSrs as the scaling inhertiance instead.
                fbxNode.SetTransformationInheritType (FbxTransform.EInheritType.eInheritRSrs);

                ExportTransform ( unityGo.transform, fbxNode, newCenter, exportType);

                // try export mesh
                bool exportedMesh = ExportInstance (unityGo, fbxNode, fbxScene);
                if (!exportedMesh) {
                    exportedMesh = ExportMesh (unityGo, fbxNode);
                }

                // export camera, but only if no mesh was exported
                bool exportedCamera = false;
                if (!exportedMesh) {
                    exportedCamera = ExportCamera (unityGo, fbxScene, fbxNode);
                }

                // export light, but only if no mesh or camera was exported
                if (!exportedMesh && !exportedCamera) {
                    ExportLight (unityGo, fbxScene, fbxNode);
                }

                if (Verbose)
                    Debug.Log (string.Format ("exporting {0}", fbxNode.GetName ()));

                fbxNodeParent.AddChild (fbxNode);

                // now  unityGo  through our children and recurse
                foreach (Transform childT in  unityGo.transform) {
                    numObjectsExported = ExportComponents (childT.gameObject, fbxScene, fbxNode, numObjectsExported, objectCount, newCenter);
                }

                // now export animation (after hierarchy has been exported)
                ExportAnimation (unityGo, fbxScene);

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

            /// <summary>
            /// Recursively go through the hierarchy, unioning the bounding box centers
            /// of all the children, to find the combined bounds.
            /// </summary>
            /// <param name="t">Transform.</param>
            /// <param name="boundsUnion">The Bounds that is the Union of all the bounds on this transform's hierarchy.</param>
            private static void EncapsulateBounds(Transform t, ref Bounds boundsUnion)
            {
                var bounds = GetBounds (t);
                boundsUnion.Encapsulate (bounds);

                foreach (Transform child in t) {
                    EncapsulateBounds (child, ref boundsUnion);
                }
            }

            /// <summary>
            /// Gets the bounds of a transform. 
            /// Looks first at the Renderer, then Mesh, then Collider.
            /// Default to a bounds with center transform.position and size zero.
            /// </summary>
            /// <returns>The bounds.</returns>
            /// <param name="t">Transform.</param>
            private static Bounds GetBounds(Transform t)
            {
                var renderer = t.GetComponent<Renderer> ();
                if (renderer) {
                    return renderer.bounds;
                }
                var mesh = t.GetComponent<Mesh> ();
                if (mesh) {
                    return mesh.bounds;
                }
                var collider = t.GetComponent<Collider> ();
                if (collider) {
                    return collider.bounds;
                }
                return new Bounds(t.position, Vector3.zero);
            }

            /// <summary>
            /// Finds the center of a group of GameObjects.
            /// </summary>
            /// <returns>Center of gameObjects.</returns>
            /// <param name="gameObjects">Game objects.</param>
            public static Vector3 FindCenter(IEnumerable<GameObject> gameObjects)
            {
                Bounds bounds = new Bounds();
                // Assign the initial bounds to first GameObject's bounds
                // (if we initialize the bounds to 0, then 0 will be part of the bounds)
                foreach (var go in gameObjects) {
                    var tempBounds = GetBounds (go.transform);
                    bounds = new Bounds (tempBounds.center, tempBounds.size);
                    break;
                }
                foreach (var go in gameObjects) {
                    EncapsulateBounds (go.transform, ref bounds);
                }
                return bounds.center;
            }

            /// <summary>
            /// Gets the recentered translation.
            /// </summary>
            /// <returns>The recentered translation.</returns>
            /// <param name="t">Transform.</param>
            /// <param name="center">Center point.</param>
            public static Vector3 GetRecenteredTranslation(Transform t, Vector3 center)
            {
                return t.position - center;
            }

            public enum TransformExportType { Local, Global, Reset };

            /// <summary>
            /// Export all the objects in the set.
            /// Return the number of objects in the set that we exported.
            ///
            /// This refreshes the asset database.
            /// </summary>
            public int ExportAll (IEnumerable<UnityEngine.Object> unityExportSet)
            {
                exportCancelled = false;

                // Export first to a temporary file
                // in case the export is cancelled.
                // This way we won't overwrite existing files.
                try{
                    m_tempFilePath = Path.GetTempFileName();
                }
                catch(IOException){
                    return 0;
                }
                m_lastFilePath = LastFilePath;

                if (string.IsNullOrEmpty (m_tempFilePath)) {
                    return 0;
                }

                try {
                    bool status = false;
                    // Create the FBX manager
                    using (var fbxManager = FbxManager.Create ()) {
                        // Configure fbx IO settings.
                        fbxManager.SetIOSettings (FbxIOSettings.Create (fbxManager, Globals.IOSROOT));

                        // Create the exporter
                        var fbxExporter = FbxExporter.Create (fbxManager, "Exporter");

                        // Initialize the exporter.
                        // fileFormat must be binary if we are embedding textures
                        int fileFormat = -1;
                        if (EditorTools.ExportSettings.instance.ExportFormatSelection == (int)ExportFormat.ASCII)
                        {
                            fileFormat = fbxManager.GetIOPluginRegistry().FindWriterIDByDescription("FBX ascii (*.fbx)");
                        }                        
                        
                        status = fbxExporter.Initialize (m_tempFilePath, fileFormat, fbxManager.GetIOSettings ());
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
                        fbxSceneInfo.Original_ApplicationName.Set(string.Format("Unity {0}", PACKAGE_UI_NAME));
                        // set last saved to be the same as original, as this is a new file.
                        fbxSceneInfo.LastSaved_ApplicationName.Set(fbxSceneInfo.Original_ApplicationName.Get());

                        var version = GetVersionFromReadme();
                        if(version != null){
                            fbxSceneInfo.Original_ApplicationVersion.Set(version);
                            fbxSceneInfo.LastSaved_ApplicationVersion.Set(fbxSceneInfo.Original_ApplicationVersion.Get());
                        }
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
                                exportProgress = this.ExportComponents (
                                    unityGo, fbxScene, fbxRootNode, exportProgress,
                                    count, Vector3.zero, TransformExportType.Reset);
                                if (exportCancelled || exportProgress < 0) {
                                    Debug.LogWarning ("Export Cancelled");
                                    return 0;
                                }
                            }
                        }
                        else{
                            // find the center of the export set
                            Vector3 center = ExportSettings.centerObjects? FindCenter(revisedExportSet) : Vector3.zero;

                            foreach (var unityGo in revisedExportSet) {
                                exportProgress = this.ExportComponents (unityGo, fbxScene, fbxRootNode,
                                    exportProgress, count, center, TransformExportType.Global);
                                if (exportCancelled || exportProgress < 0) {
                                    Debug.LogWarning ("Export Cancelled");
                                    return 0;
                                }
                            }
                        }

                        // Set the scene's default camera.
                        SetDefaultCamera (fbxScene);

                        // Export the scene to the file.
                        status = fbxExporter.Export (fbxScene);

                        // cleanup
                        fbxScene.Destroy ();
                        fbxExporter.Destroy ();
                    }

                    if (exportCancelled) {
                        Debug.LogWarning ("Export Cancelled");
                        return 0;
                    }
                    // delete old file, move temp file
                    ReplaceFile();
                    AssetDatabase.Refresh();

                    return status == true ? NumNodes : 0;
                }
                finally {
                    // You must clear the progress bar when you're done,
                    // otherwise it never goes away and many actions in Unity
                    // are blocked (e.g. you can't quit).
                    EditorUtility.ClearProgressBar ();

                    // make sure the temp file is deleted, no matter
                    // when we return
                    DeleteTempFile();
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

            /// <summary>
            /// Deletes the file that got created while exporting.
            /// </summary>
            private void DeleteTempFile ()
            {
                if (!File.Exists (m_tempFilePath)) {
                    return;
                }

                try {
                    File.Delete (m_tempFilePath);
                } catch (IOException) {
                }

                if (File.Exists (m_tempFilePath)) {
                    Debug.LogWarning ("Failed to delete file: " + m_tempFilePath);
                }
            }

            /// <summary>
            /// Replaces the file we are overwriting with
            /// the temp file that was exported to.
            /// </summary>
            private void ReplaceFile ()
            {
                if (m_tempFilePath.Equals (m_lastFilePath) || !File.Exists (m_tempFilePath)) {
                    return;
                }
                // delete old file
                try {
                    File.Delete (m_lastFilePath);
                } catch (IOException) {
                }

                // refresh the database so Unity knows the file's been deleted
                AssetDatabase.Refresh();

                if (File.Exists (m_lastFilePath)) {
                    Debug.LogWarning ("Failed to delete file: " + m_lastFilePath);
                }

                // rename the new file
                try{
                    File.Move(m_tempFilePath, m_lastFilePath);
                } catch(IOException){
                    Debug.LogWarning (string.Format("Failed to move file {0} to {1}", m_tempFilePath, m_lastFilePath));
                }
            }

            /// <summary>
            /// Add a menu item to a GameObject's context menu.
            /// </summary>
            /// <param name="command">Command.</param>
            [MenuItem (MenuItemName, false, 30)]
            static void OnContextItem (MenuCommand command)
            {
                if (Selection.objects.Length <= 0) {
                    DisplayNoSelectionDialog ();
                    return;
                }
                OnExport ();
            }

            /// <summary>
            // Validate the menu item defined by the function above.
            /// </summary>
            [MenuItem (MenuItemName, true, 30)]
            public static bool OnValidateMenuItem ()
            {
                return true;
            }

            public static void DisplayNoSelectionDialog()
            {
                UnityEditor.EditorUtility.DisplayDialog (
                    string.Format("{0} Warning", PACKAGE_UI_NAME), 
                    "No GameObjects selected for export.", 
                    "Ok");
            }
            //
            // export mesh info from Unity
            //
            ///<summary>
            ///Information about the mesh that is important for exporting.
            ///</summary>
            class MeshInfo
            {
                public Mesh mesh;

                /// <summary>
                /// Return true if there's a valid mesh information
                /// </summary>
                public bool IsValid { get { return mesh; } }

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
                private int[] m_triangles;
                public int [] Triangles { get { 
                        if(m_triangles == null) { m_triangles = mesh.triangles; }
                        return m_triangles; 
                    } }

                /// <summary>
                /// Gets the vertices, represented in local coordinates.
                /// </summary>
                /// <value>The vertices.</value>
                private Vector3[] m_vertices;
                public Vector3 [] Vertices { get { 
                        if(m_vertices == null) { m_vertices = mesh.vertices; }
                        return m_vertices; 
                    } }

                /// <summary>
                /// Gets the normals for the vertices.
                /// </summary>
                /// <value>The normals.</value>
                private Vector3[] m_normals;
                public Vector3 [] Normals { get {
                        if (m_normals == null) {
                            m_normals = mesh.normals;
                        }
                        return m_normals; 
                    } }

                /// <summary>
                /// Gets the binormals for the vertices.
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
                            var normals = Normals;
                            var tangents = Tangents;

                            if (HasValidNormals() && HasValidTangents()) {
                                m_Binormals = new Vector3 [normals.Length];

                                for (int i = 0; i < normals.Length; i++)
                                    m_Binormals [i] = Vector3.Cross (normals [i],
                                        tangents [i])
                                    * tangents [i].w;
                            }
                        }
                        return m_Binormals;
                    }
                }

                /// <summary>
                /// Gets the tangents for the vertices.
                /// </summary>
                /// <value>The tangents.</value>
                private Vector4[] m_tangents;
                public Vector4 [] Tangents { get { 
                        if (m_tangents == null) {
                            m_tangents = mesh.tangents;
                        }
                        return m_tangents; 
                    } }

                /// <summary>
                /// Gets the vertex colors for the vertices.
                /// </summary>
                /// <value>The vertex colors.</value>
                private Color32 [] m_vertexColors;
                public Color32 [] VertexColors { get { 
                        if (m_vertexColors == null) {
                            m_vertexColors = mesh.colors32;
                        }
                        return m_vertexColors; 
                    } }

                /// <summary>
                /// Gets the uvs.
                /// </summary>
                /// <value>The uv.</value>
                private Vector2[] m_UVs;
                public Vector2 [] UV { get { 
                        if (m_UVs == null) {
                            m_UVs = mesh.uv;
                        }
                        return m_UVs; 
                    } }

                /// <summary>
                /// The material(s) used.
                /// Always at least one.
                /// None are missing materials (we replace missing materials with the default material).
                /// </summary>
                public Material[] Materials { get ; private set; }

                /// <summary>
                /// Set up the MeshInfo with the given mesh and materials.
                /// </summary>
                public MeshInfo (Mesh mesh, Material[] materials)
                {
                    this.mesh = mesh;

                    this.m_Binormals = null;
                    this.m_vertices = null;
                    this.m_triangles = null;
                    this.m_normals = null;
                    this.m_UVs = null;
                    this.m_vertexColors = null;
                    this.m_tangents = null;

                    if (materials == null) {
                        this.Materials = new Material[] { DefaultMaterial };
                    } else {
                        this.Materials = materials.Select (mat => mat ? mat : DefaultMaterial).ToArray ();
                        if (this.Materials.Length == 0) {
                            this.Materials = new Material[] { DefaultMaterial };
                        }
                    }
                }

                public bool HasValidNormals(){
                    return Normals != null && Normals.Length > 0;
                }

                public bool HasValidBinormals(){
                    return HasValidNormals () &&
                        HasValidTangents () &&
                        Binormals != null;
                }

                public bool HasValidTangents(){
                    return Tangents != null && Tangents.Length > 0;
                }

                public bool HasValidVertexColors(){
                    return VertexColors != null && VertexColors.Length > 0;
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
                } else if (obj is Behaviour) {
                    var behaviour = obj as Behaviour;
                    return behaviour.gameObject;
                }

                return null;
            }

            /// <summary>
            /// If your MonoBehaviour knows about some custom geometry that
            /// isn't in a MeshFilter or SkinnedMeshRenderer, use
            /// RegisterMeshCallback to get a callback when the exporter tries
            /// to export your component.
            ///
            /// The callback should return true, and output the mesh you want.
            ///
            /// Return false if you don't want to drive this game object.
            ///
            /// Return true and output a null mesh if you don't want the
            /// exporter to output anything.
            /// </summary>
            public delegate bool GetMeshForComponent<T>(ModelExporter exporter, T component, FbxNode fbxNode) where T : MonoBehaviour;
            public delegate bool GetMeshForComponent(ModelExporter exporter, MonoBehaviour component, FbxNode fbxNode);

            /// <summary>
            /// Map from type (must be a MonoBehaviour) to callback.
            /// The type safety is lost; the caller must ensure it at run-time.
            /// </summary>
            static Dictionary<System.Type, GetMeshForComponent> MeshForComponentCallbacks
                = new Dictionary<System.Type, GetMeshForComponent>();

            /// <summary>
            /// Register a callback to invoke if the object has a component of type T.
            ///
            /// This function is prefered over the other mesh callback
            /// registration methods because it's type-safe, efficient, and
            /// invocation order between types can be controlled in the UI by
            /// reordering the components.
            ///
            /// It's an error to register a callback for a component that
            /// already has one, unless 'replace' is set to true.
            /// </summary>
            public static void RegisterMeshCallback<T>(GetMeshForComponent<T> callback, bool replace = false)
                where T: UnityEngine.MonoBehaviour
            {
                // Under the hood we lose type safety, but don't let the user notice!
                RegisterMeshCallback(typeof(T),
                        (ModelExporter exporter, MonoBehaviour component, FbxNode fbxNode) =>
                            callback(exporter, (T)component, fbxNode),
                        replace);
            }

            /// <summary>
            /// Register a callback to invoke if the object has a component of type T.
            ///
            /// The callback will be invoked with an argument of type T, it's
            /// safe to downcast.
            ///
            /// Normally you'll want to use the generic form, but this one is
            /// easier to use with reflection.
            /// </summary>
            public static void RegisterMeshCallback(System.Type t,
                    GetMeshForComponent callback,
                    bool replace = false)
            {
                if (!t.IsSubclassOf(typeof(MonoBehaviour))) {
                    throw new System.Exception("Registering a callback for a type that isn't derived from MonoBehaviour: " + t);
                }
                if (!replace && MeshForComponentCallbacks.ContainsKey(t)) {
                    throw new System.Exception("Replacing a callback for type " + t);
                }
                MeshForComponentCallbacks[t] = callback;
            }

            /// <summary>
            /// Delegate used to convert a GameObject into a mesh.
            ///
            /// This is useful if you want to have broader control over
            /// the export process than the GetMeshForComponent callbacks
            /// provide. But it's less efficient because you'll get a callback
            /// on every single GameObject.
            /// </summary>
            public delegate bool GetMeshForObject(ModelExporter exporter, GameObject gameObject, FbxNode fbxNode);

            static List<GetMeshForObject> MeshForObjectCallbacks = new List<GetMeshForObject>();

            /// <summary>
            /// Register a callback to invoke on every GameObject we export.
            ///
            /// Avoid doing this if you can use a callback that depends on type.
            ///
            /// The GameObject-based callbacks are checked before the
            /// component-based ones.
            ///
            /// Multiple GameObject-based callbacks can be registered; they are
            /// checked in order of registration.
            /// </summary>
            public static void RegisterMeshObjectCallback(GetMeshForObject callback)
            {
                MeshForObjectCallbacks.Add(callback);
            }

            /// <summary>
            /// Forget the callback linked to a component of type T.
            /// </summary>
            public static void UnRegisterMeshCallback<T>()
            {
                MeshForComponentCallbacks.Remove(typeof(T));
            }

            /// <summary>
            /// Forget the callback linked to a component of type T.
            /// </summary>
            public static void UnRegisterMeshCallback(System.Type t)
            {
                MeshForComponentCallbacks.Remove(t);
            }

            /// <summary>
            /// Forget a GameObject-based callback.
            /// </summary>
            public static void UnRegisterMeshCallback(GetMeshForObject callback)
            {
                MeshForObjectCallbacks.Remove(callback);
            }

            /// <summary>
            /// Exports a mesh for a unity gameObject.
            ///
            /// This goes through the callback system to find the right mesh and
            /// allow plugins to substitute their own meshes.
            /// </summary>
            bool ExportMesh (GameObject gameObject, FbxNode fbxNode)
            {
                // First allow the object-based callbacks to have a hack at it.
                foreach(var callback in MeshForObjectCallbacks) {
                    if (callback(this, gameObject, fbxNode)) {
                        return true;
                    }
                }

                // Next iterate over components and allow the component-based
                // callbacks to have a hack at it. This is complicated by the
                // potential of subclassing. While we're iterating we keep the
                // first MeshFilter or SkinnedMeshRenderer we find.
                Component defaultComponent = null;
                foreach(var component in gameObject.GetComponents<Component>()) {
                    if (!component) {
                        continue;
                    }
                    var monoBehaviour = component as MonoBehaviour;
                    if (!monoBehaviour) {
                        // Check for default handling. But don't commit yet.
                        if (defaultComponent) {
                            continue;
                        } else if (component is MeshFilter) {
                            defaultComponent = component;
                        } else if (component is SkinnedMeshRenderer) {
                            defaultComponent = component;
                        }
                    } else {
                        // Check if we have custom behaviour for this component type, or
                        // one of its base classes.
                        if (!monoBehaviour.enabled) {
                            continue;
                        }
                        var componentType = monoBehaviour.GetType ();
                        do {
                            GetMeshForComponent callback;
                            if (MeshForComponentCallbacks.TryGetValue (componentType, out callback)) {
                                if (callback (this, monoBehaviour, fbxNode)) {
                                    return true;
                                }
                            }
                            componentType = componentType.BaseType;
                        } while(componentType.IsSubclassOf (typeof(MonoBehaviour)));
                    }
                }

                // If we're here, custom handling didn't work.
                // Revert to default handling.
                var meshFilter = defaultComponent as MeshFilter;
                if (meshFilter) {
                    var renderer = gameObject.GetComponent<Renderer>();
                    var materials = renderer ? renderer.sharedMaterials : null;
                    return ExportMesh(new MeshInfo(meshFilter.sharedMesh, materials), fbxNode);
                } else {
                    var smr = defaultComponent as SkinnedMeshRenderer;
                    if (smr) {
                        var mesh = new Mesh();
                        smr.BakeMesh(mesh);
                        var materials = smr.sharedMaterials;
                        bool result = ExportMesh(new MeshInfo(mesh, materials), fbxNode);
                        Object.DestroyImmediate(mesh);
                        return result;
                    }
                }

                return false;
            }

            /// <summary>
            /// keep a map between GameObject and FbxNode for quick lookup when we export
            /// animation.
            /// </summary>
            Dictionary<GameObject, FbxNode> MapUnityObjectToFbxNode = new Dictionary<GameObject, FbxNode> ();

            /// <summary>
            /// Number of nodes exported including siblings and decendents
            /// </summary>
            public int NumNodes { get { return MapUnityObjectToFbxNode.Count; } }

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

            public bool Verbose { private set {;} get { return Debug.unityLogger.logEnabled; } }

            /// <summary>
            /// manage the selection of a filename
            /// </summary>
            static string LastFilePath { get; set; }
            private string m_tempFilePath { get; set; }
            private string m_lastFilePath { get; set; }

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
            public static string ExportObjects (string filePath, UnityEngine.Object[] objects = null)
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

            public static string ExportObject (string filePath, UnityEngine.Object root)
            {
                return ExportObjects(filePath, new Object[] { root } );
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
