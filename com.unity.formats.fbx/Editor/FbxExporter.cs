using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEditor;
using System.Linq;
using Autodesk.Fbx;
using System.Runtime.CompilerServices;  
using System.Runtime.Serialization;
using UnityEditor.Formats.Fbx.Exporter.Visitors;
using UnityEditor.Formats.Fbx.Exporter.CustomExtensions;
using System.Security.Permissions;

[assembly: InternalsVisibleTo("Unity.Formats.Fbx.Editor.Tests")]  
[assembly: InternalsVisibleTo("Unity.ProBuilder.AddOns.Editor")]  

namespace UnityEditor.Formats.Fbx.Exporter
{
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
    internal delegate bool GetMeshForComponent<T>(ModelExporter exporter, T component, FbxNode fbxNode) where T : MonoBehaviour;
    internal delegate bool GetMeshForComponent(ModelExporter exporter, MonoBehaviour component, FbxNode fbxNode);

    /// <summary>
    /// Delegate used to convert a GameObject into a mesh.
    ///
    /// This is useful if you want to have broader control over
    /// the export process than the GetMeshForComponent callbacks
    /// provide. But it's less efficient because you'll get a callback
    /// on every single GameObject.
    /// </summary>
    internal delegate bool GetMeshForObject(ModelExporter exporter, GameObject gameObject, FbxNode fbxNode);

    [System.Serializable]
    internal class ModelExportException : System.Exception
    {
        public ModelExportException(){}

        public ModelExportException(string message)
            : base(message){}

        public ModelExportException(string message, System.Exception inner)
            : base(message, inner){}

        protected ModelExportException(SerializationInfo info, StreamingContext context)
            : base(info, context){}
    }

    /// <summary>
    /// Use the ModelExporter class to export Unity GameObjects to an FBX file.
    /// <para>
    /// Use the ExportObject and ExportObjects methods. The default export
    /// options are used when exporting the objects to the FBX file.
    /// </para>
    /// <para>For information on using the ModelExporter class, see <a href="../manual/devguide.html">the Developer's Guide</a>.</para>
    /// </summary>
    public sealed class ModelExporter : System.IDisposable
    {
        const string Title =
            "Created by FBX Exporter from Unity Technologies";

        const string Subject =
            "";

        const string Keywords =
            "Nodes Meshes Materials Textures Cameras Lights Skins Animation";

        const string Comments =
            @"";

        /// <summary>
        /// Path to the CHANGELOG file in Unity's virtual file system. Used to get the version number.
        /// </summary>
        const string ChangeLogPath = "Packages/com.unity.formats.fbx/CHANGELOG.md";

        // NOTE: The ellipsis at the end of the Menu Item name prevents the context
        //       from being passed to command, thus resulting in OnContextItem()
        //       being called only once regardless of what is selected.
        const string MenuItemName = "GameObject/Export To FBX...";

        const string TimelineClipMenuItemName = "GameObject/Export Selected Timeline Clip...";

        const string ProgressBarTitle = "FBX Export";

        const char MayaNamespaceSeparator = ':';

        // replace invalid chars with this one
        const char InvalidCharReplacement = '_';

        const string RegexCharStart = "[";
        const string RegexCharEnd = "]";

        internal const float UnitScaleFactor = 100f;

        internal const string PACKAGE_UI_NAME = "FBX Exporter";

        /// <summary>
        /// name of the scene's default camera
        /// </summary>
        private static string DefaultCamera = "";

        private const string SkeletonPrefix = "_Skel";

        private const string SkinPrefix = "_Skin";

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
        /// Which components map from Unity Object to Fbx Object
        /// </summary>
        internal enum FbxNodeRelationType
        {
            NodeAttribute,
            Property,
            Material
        }

        internal static Dictionary<System.Type, KeyValuePair<System.Type,FbxNodeRelationType>> MapsToFbxObject = new Dictionary<System.Type, KeyValuePair<System.Type,FbxNodeRelationType>> ()
        {
            { typeof(Transform),            new KeyValuePair<System.Type, FbxNodeRelationType>(typeof(FbxProperty), FbxNodeRelationType.Property) },
            { typeof(MeshFilter),           new KeyValuePair<System.Type, FbxNodeRelationType>(typeof(FbxMesh), FbxNodeRelationType.NodeAttribute) },
            { typeof(SkinnedMeshRenderer),  new KeyValuePair<System.Type, FbxNodeRelationType>(typeof(FbxMesh), FbxNodeRelationType.NodeAttribute) },
            { typeof(Light),                new KeyValuePair<System.Type, FbxNodeRelationType>(typeof(FbxLight), FbxNodeRelationType.NodeAttribute) },
            { typeof(Camera),               new KeyValuePair<System.Type, FbxNodeRelationType>(typeof(FbxCamera), FbxNodeRelationType.NodeAttribute) },
            { typeof(Material),             new KeyValuePair<System.Type, FbxNodeRelationType>(typeof(FbxSurfaceMaterial), FbxNodeRelationType.Material) },
        };

        /// <summary>
        /// keep a map between GameObject and FbxNode for quick lookup when we export
        /// animation.
        /// </summary>
        Dictionary<GameObject, FbxNode> MapUnityObjectToFbxNode = new Dictionary<GameObject, FbxNode> ();

        /// <summary>
        /// keep a map between the constrained FbxNode (in Unity this is the GameObject with constraint component)
        /// and its FbxConstraints for quick lookup when exporting constraint animations.
        /// </summary>
        Dictionary<FbxNode, Dictionary<FbxConstraint, System.Type>> MapConstrainedObjectToConstraints = new Dictionary<FbxNode, Dictionary<FbxConstraint, System.Type>>();

        /// <summary>
        /// keep a map between the FbxNode and its blendshape channels for quick lookup when exporting blendshapes.
        /// </summary>
        Dictionary<FbxNode, Dictionary<FbxBlendShapeChannel, string>> MapUnityObjectToBlendShapes = new Dictionary<FbxNode, Dictionary<FbxBlendShapeChannel, string>>();

        /// <summary>
        /// Map Unity material ID to FBX material object
        /// </summary>
        Dictionary<int, FbxSurfaceMaterial> MaterialMap = new Dictionary<int, FbxSurfaceMaterial> ();

        /// <summary>
        /// Map texture filename name to FBX texture object
        /// </summary>
        Dictionary<string, FbxTexture> TextureMap = new Dictionary<string, FbxTexture> ();

        /// <summary>
        /// Map the ID of a prefab to an FbxMesh (for preserving instances) 
        /// </summary>
        Dictionary<int, FbxMesh> SharedMeshes = new Dictionary<int, FbxMesh> ();

        /// <summary>
        /// Map for the Name of an Object to number of objects with this name.
        /// Used for enforcing unique names on export.
        /// </summary>
        Dictionary<string, int> NameToIndexMap = new Dictionary<string, int> ();

        /// <summary>
        /// Map for the Material Name to number of materials with this name.
        /// Used for enforcing unique names on export.
        /// </summary>
        Dictionary<string, int> MaterialNameToIndexMap = new Dictionary<string, int>();

        /// <summary>
        /// Map for the Texture Name to number of textures with this name.
        /// Used for enforcing unique names on export.
        /// </summary>
        Dictionary<string, int> TextureNameToIndexMap = new Dictionary<string, int>();
        
        Dictionary<Mesh, FbxNode> MeshToFbxNodeMap = new Dictionary<Mesh, FbxNode>();

        /// <summary>
        /// Format for creating unique names
        /// </summary>
        const string UniqueNameFormat = "{0}_{1}";

        /// <summary>
        /// The animation fbx file format.
        /// </summary>
        const string AnimFbxFileFormat = "{0}/{1}@{2}.fbx";

        /// <summary>
        /// Gets the export settings.
        /// </summary>
        internal static ExportSettings ExportSettings {
            get { return ExportSettings.instance; }
        }

        internal static IExportOptions DefaultOptions {
            get { return new ExportModelSettingsSerialize(); }
        }

        private IExportOptions m_exportOptions;
        private IExportOptions ExportOptions {
            get {
                if (m_exportOptions == null) {
                    // get default settings;
                    m_exportOptions = DefaultOptions;
                }
                return m_exportOptions;
            }
            set { m_exportOptions = value; }
        }

        /// <summary>
        /// Gets the Unity default material.
        /// </summary>
        internal static Material DefaultMaterial {
            get {
                if (!s_defaultMaterial) {
                    var obj = GameObject.CreatePrimitive (PrimitiveType.Quad);
                    s_defaultMaterial = obj.GetComponent<Renderer> ().sharedMaterial;
                    Object.DestroyImmediate (obj);
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
        internal static string GetVersionFromReadme()
        {
            if (!File.Exists (ChangeLogPath)) {
                Debug.LogWarning (string.Format("Could not find version number, the ChangeLog file is missing from: {0}", ChangeLogPath));
                return null;
            }

            try {
                // The standard format is:
                //   ## [a.b.c-whatever] - yyyy-mm-dd
                // Another format is:
                //   **Version**: a.b.c-whatever
                // we handle either one and read out the version
                var lines = File.ReadAllLines (ChangeLogPath);
                var regexes = new string [] {
                    @"^\s*##\s*\[(.*)\]",
                    @"^\s*\*\*Version\*\*:\s*(.*)\s*"
                };
                foreach (var line in lines) {
                    foreach (var regex in regexes) {
                        var match = System.Text.RegularExpressions.Regex.Match(line, regex);
                        if (match.Success) {
                            var version = match.Groups[1].Value;
                            return version.Trim ();
                        }
                    }
                }

                // If we're here, we didn't find any match.
                Debug.LogWarning (string.Format("Could not find most recent version number in {0}", ChangeLogPath));
                return null;
            }
            catch(IOException e){
                Debug.LogException (e);
                Debug.LogWarning (string.Format("Error reading file {0} ({1})", ChangeLogPath, e));
                return null;
            }

        }

        /// <summary>
        /// Get a layer (to store UVs, normals, etc) on the mesh.
        /// If it doesn't exist yet, create it.
        /// </summary>
        internal static FbxLayer GetOrCreateLayer(FbxMesh fbxMesh, int layer = 0 /* default layer */)
        {
            int maxLayerIndex = fbxMesh.GetLayerCount() - 1;
            while (layer > maxLayerIndex) {
                // We'll have to create the layer (potentially several).
                // Make sure to avoid infinite loops even if there's an
                // FbxSdk bug.
                int newLayerIndex = fbxMesh.CreateLayer();
                if (newLayerIndex <= maxLayerIndex) {
                    // Error!
                    throw new ModelExportException(
                        "Internal error: Unable to create mesh layer "
                        + (maxLayerIndex + 1)
                        + " on mesh " + fbxMesh.GetName ());
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
                        fbxElementArray.Add (ConvertToFbxVector4 (mesh.Normals [unityTriangle]));
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
                        fbxElementArray.Add (ConvertToFbxVector4 (mesh.Binormals [unityTriangle]));
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
                        fbxElementArray.Add (ConvertToFbxVector4 (
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
        /// Export the mesh's blend shapes.
        /// </summary>
        private bool ExportBlendShapes(FbxNode fbxNode, MeshInfo mesh, FbxMesh fbxMesh, FbxScene fbxScene, int[] unmergedTriangles)
        {
            var umesh = mesh.mesh;
            if (umesh.blendShapeCount == 0)
                return false;

            var fbxBlendShape = FbxBlendShape.Create(fbxScene, umesh.name + "_BlendShape");
            fbxMesh.AddDeformer(fbxBlendShape);

            var numVertices = umesh.vertexCount;
            var basePoints = umesh.vertices;
            var baseNormals = umesh.normals;
            var baseTangents = umesh.tangents;
            var deltaPoints = new Vector3[numVertices];
            var deltaNormals = new Vector3[numVertices];
            var deltaTangents = new Vector3[numVertices];

            for (int bi = 0; bi < umesh.blendShapeCount; ++bi)
            {
                var bsName = umesh.GetBlendShapeName(bi);
                var numFrames = umesh.GetBlendShapeFrameCount(bi);
                var fbxChannel = FbxBlendShapeChannel.Create(fbxScene, bsName);
                fbxBlendShape.AddBlendShapeChannel(fbxChannel);

                Dictionary<FbxBlendShapeChannel, string> blenshapeChannels;
                if (!MapUnityObjectToBlendShapes.TryGetValue(fbxNode, out blenshapeChannels))
                {
                    blenshapeChannels = new Dictionary<FbxBlendShapeChannel, string>();
                    MapUnityObjectToBlendShapes.Add(fbxNode, blenshapeChannels);
                }
                blenshapeChannels.Add(fbxChannel, bsName);

                for (int fi = 0; fi < numFrames; ++fi)
                {
                    var weight = umesh.GetBlendShapeFrameWeight(bi, fi);
                    umesh.GetBlendShapeFrameVertices(bi, fi, deltaPoints, deltaNormals, deltaTangents);

                    var fbxShapeName = bsName;

                    if (numFrames > 1)
                    {
                        fbxShapeName += "_" + fi;
                    }

                    var fbxShape = FbxShape.Create(fbxScene, fbxShapeName);
                    fbxChannel.AddTargetShape(fbxShape, weight);

                    // control points
                    fbxShape.InitControlPoints(ControlPointToIndex.Count());
                    for (int vi = 0; vi < numVertices; ++vi)
                    {
                        int ni = ControlPointToIndex[basePoints[vi]];
                        var v = basePoints[vi] + deltaPoints[vi];
                        fbxShape.SetControlPointAt(ConvertToFbxVector4(v, UnitScaleFactor), ni);
                    }

                    // normals
                    if (mesh.HasValidNormals())
                    {
                        var elemNormals = fbxShape.CreateElementNormal();
                        elemNormals.SetMappingMode(FbxLayerElement.EMappingMode.eByPolygonVertex);
                        elemNormals.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
                        var dstNormals = elemNormals.GetDirectArray();
                        dstNormals.SetCount(unmergedTriangles.Length);
                        for (int ii = 0; ii < unmergedTriangles.Length; ++ii)
                        {
                            int vi = unmergedTriangles[ii];
                            var n = baseNormals[vi] + deltaNormals[vi];
                            dstNormals.SetAt(ii, ConvertToFbxVector4(n));
                        }
                    }

                    // tangents
                    if (mesh.HasValidTangents())
                    {
                        var elemTangents = fbxShape.CreateElementTangent();
                        elemTangents.SetMappingMode(FbxLayerElement.EMappingMode.eByPolygonVertex);
                        elemTangents.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
                        var dstTangents = elemTangents.GetDirectArray();
                        dstTangents.SetCount(unmergedTriangles.Length);
                        for (int ii = 0; ii < unmergedTriangles.Length; ++ii)
                        {
                            int vi = unmergedTriangles[ii];
                            var t = (Vector3)baseTangents[vi] + deltaTangents[vi];
                            dstTangents.SetAt(ii, ConvertToFbxVector4(t));
                        }
                    }
                }
            }
            return true;
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
        internal static FbxVector4 ConvertToFbxVector4(Vector3 leftHandedVector, float unitScale = 1f)
        {
            // negating the x component of the vector converts it from left to right handed coordinates
            return unitScale * new FbxVector4 (
                leftHandedVector[0],
                leftHandedVector[1],
                leftHandedVector[2]);
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
        internal bool ExportTexture (Material unityMaterial, string unityPropName,
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
            var textureSourceFullPath = AssetDatabase.GetAssetPath(unityTexture);
            if (string.IsNullOrEmpty(textureSourceFullPath)) {
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
                var textureName = GetUniqueTextureName(fbxPropName + "_Texture");
                var fbxTexture = FbxFileTexture.Create (fbxMaterial, textureName);
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
        internal FbxDouble3 GetMaterialColor (Material unityMaterial, string unityPropName, float defaultValue = 1)
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
        internal bool ExportMaterial (Material unityMaterial, FbxScene fbxScene, FbxNode fbxNode)
        {
            if (!unityMaterial) {
                unityMaterial = DefaultMaterial;
            }

            var unityID = unityMaterial.GetInstanceID();
            FbxSurfaceMaterial mappedMaterial;
            if (MaterialMap.TryGetValue (unityID, out mappedMaterial)) {
                fbxNode.AddMaterial (mappedMaterial);
                return true;
            }

            var unityName = unityMaterial.name;
            var fbxName = ExportOptions.UseMayaCompatibleNames
                ? ConvertToMayaCompatibleName(unityName) : unityName;

            fbxName = GetUniqueMaterialName(fbxName);

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

            MaterialMap.Add (unityID, fbxMaterial);
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
                                throw new System.NotImplementedException();
                        }

                        // Specify the material index for each polygon.
                        // Material index should match subMeshIndex.
                        var indices = mesh.GetIndices(subMeshIndex);
                        for (int j = 0, n = indices.Length / polySize; j < n; j++) {
                            fbxElementArray.Add(subMeshIndex);
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
        internal bool ExportMesh (Mesh mesh, FbxNode fbxNode, Material[] materials = null)
        {
            var meshInfo = new MeshInfo(mesh, materials);
            return ExportMesh(meshInfo, fbxNode);
        }

        /// <summary>
        /// Keeps track of the index of each point in the exported vertex array.
        /// </summary>
        private Dictionary<Vector3, int> ControlPointToIndex = new Dictionary<Vector3, int> ();

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
            ControlPointToIndex.Clear();
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
                    fbxMesh.SetControlPointAt (ConvertToFbxVector4(controlPoint, UnitScaleFactor), index);
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
                        vertOrder = new int[] { 0, 1, 2 };
                        break;
                    case MeshTopology.Quads:
                        polySize = 4;
                        vertOrder = new int[] { 0, 1, 2, 3 };
                        break;
                    case MeshTopology.Lines:
                        throw new System.NotImplementedException();
                    case MeshTopology.Points:
                        throw new System.NotImplementedException();
                    case MeshTopology.LineStrip:
                        throw new System.NotImplementedException();
                    default:
                        throw new System.NotImplementedException();
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

            // Set up blend shapes.
            ExportBlendShapes(fbxNode, meshInfo, fbxMesh, fbxScene, unmergedPolygons.ToArray());

            // set the fbxNode containing the mesh
            fbxNode.SetNodeAttribute (fbxMesh);
            fbxNode.SetShadingMode (FbxNode.EShadingMode.eWireFrame);
            return true;
        }

        /// <summary>
        /// Export GameObject as a skinned mesh with material, bones, a skin and, a bind pose.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        private bool ExportSkinnedMesh (GameObject unityGo, FbxScene fbxScene, FbxNode fbxNode)
        {
            if(!unityGo || fbxNode == null)
            {
                return false;
            }

            SkinnedMeshRenderer unitySkin
            = unityGo.GetComponent<SkinnedMeshRenderer> ();

            if (unitySkin == null) {
                return false;
            }

            var mesh = unitySkin.sharedMesh;
            if (!mesh) {
                return false;
            }

            if (Verbose)
                Debug.Log (string.Format ("exporting {0} {1}", "Skin", fbxNode.GetName ()));


            var meshInfo = new MeshInfo(unitySkin.sharedMesh, unitySkin.sharedMaterials);

            FbxMesh fbxMesh = null;
            if (ExportMesh(meshInfo, fbxNode))
            {
                fbxMesh = fbxNode.GetMesh();
            }
            if (fbxMesh == null)
            {
                Debug.LogError("Could not find mesh");
                return false;
            }

            Dictionary<SkinnedMeshRenderer, Transform[]> skinnedMeshToBonesMap;
            // export skeleton
            if (ExportSkeleton (unitySkin, fbxScene, out skinnedMeshToBonesMap)) {
                // bind mesh to skeleton
                ExportSkin (unitySkin, meshInfo, fbxScene, fbxMesh, fbxNode);

                // add bind pose
                ExportBindPose (unitySkin, fbxNode, fbxScene, skinnedMeshToBonesMap);

                // now that the skin and bindpose are set, make sure that each of the bones
                // is set to its original position
                var bones = unitySkin.bones;
                foreach (var bone in bones)
                {
                    // ignore null bones
                    if (bone != null)
                    {
                        var fbxBone = MapUnityObjectToFbxNode[bone.gameObject];
                        ExportTransform(bone, fbxBone, newCenter: Vector3.zero, TransformExportType.Local);

                        // Cancel out the pre-rotation from the exported rotation

                        // Get prerotation
                        var fbxPreRotationEuler = fbxBone.GetPreRotation(FbxNode.EPivotSet.eSourcePivot);
                        // Convert the prerotation to a Quaternion
                        var fbxPreRotationQuaternion = EulerToQuaternionXYZ(fbxPreRotationEuler);
                        // Inverse of the prerotation
                        fbxPreRotationQuaternion.Inverse();

                        // Multiply LclRotation by pre-rotation inverse to get the LclRotation without pre-rotation applied
                        var finalLclRotationQuat = fbxPreRotationQuaternion * EulerToQuaternionZXY(bone.localEulerAngles);

                        // Convert to Euler with Unity axis system and update LclRotation
                        var finalUnityQuat = new Quaternion((float)finalLclRotationQuat.X, (float)finalLclRotationQuat.Y, (float)finalLclRotationQuat.Z, (float)finalLclRotationQuat.W);
                        fbxBone.LclRotation.Set(ToFbxDouble3(finalUnityQuat.eulerAngles));
                    }
                    else
                    {
                        Debug.Log("Warning: One or more bones are null. Skeleton may not export correctly.");
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the bind pose for the Unity bone.
        /// </summary>
        /// <returns>The bind pose.</returns>
        /// <param name="unityBone">Unity bone.</param>
        /// <param name="bindPoses">Bind poses.</param>
        /// <param name="boneDict">Dictionary of bone to index.</param>
        /// <param name="skinnedMesh">Skinned mesh.</param>
        private Matrix4x4 GetBindPose(
            Transform unityBone, Matrix4x4[] bindPoses,
            Dictionary<Transform, int> boneDict, SkinnedMeshRenderer skinnedMesh
        ){
            Matrix4x4 bindPose;
            int index;
            if (boneDict.TryGetValue (unityBone, out index)) {
                bindPose = bindPoses [index];
            } else {
                bindPose = unityBone.worldToLocalMatrix * skinnedMesh.transform.localToWorldMatrix;
            }
            return bindPose;
        }

        /// <summary>
        /// Export bones of skinned mesh, if this is a skinned mesh with
        /// bones and bind poses.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        private bool ExportSkeleton (SkinnedMeshRenderer skinnedMesh, FbxScene fbxScene, out Dictionary<SkinnedMeshRenderer, Transform[]> skinnedMeshToBonesMap)
        {
            skinnedMeshToBonesMap = new Dictionary<SkinnedMeshRenderer, Transform[]> ();

            if (!skinnedMesh) {
                return false;
            }
            var bones = skinnedMesh.bones;
            if (bones == null || bones.Length == 0) {
                return false;
            }
            var mesh = skinnedMesh.sharedMesh;
            if (!mesh) {
                return false;
            }

            var bindPoses = mesh.bindposes;
            if (bindPoses == null || bindPoses.Length != bones.Length) {
                return false;
            }

            // Two steps:
            // 0. Set up the map from bone to index.
            // 1. Set the transforms.

            // Step 0: map transform to index so we can look up index by bone.
            Dictionary<Transform, int> index = new Dictionary<Transform, int>();
            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++) {
                Transform unityBoneTransform = bones [boneIndex];

                // ignore null bones
                if (unityBoneTransform != null)
                {
                    index[unityBoneTransform] = boneIndex;
                }
            }

            skinnedMeshToBonesMap.Add (skinnedMesh, bones);

            // Step 1: Set transforms
            var boneInfo = new SkinnedMeshBoneInfo (skinnedMesh, index);
            foreach (var bone in bones) {
                // ignore null bones
                if (bone != null)
                {
                    var fbxBone = MapUnityObjectToFbxNode[bone.gameObject];
                    ExportBoneTransform(fbxBone, fbxScene, bone, boneInfo);
                }
            }
            return true;
        }

        /// <summary>
        /// Export binding of mesh to skeleton
        /// </summary>
        private bool ExportSkin (SkinnedMeshRenderer skinnedMesh, 
                                    MeshInfo meshInfo, FbxScene fbxScene, FbxMesh fbxMesh,
                                    FbxNode fbxRootNode)
        {
            FbxSkin fbxSkin = FbxSkin.Create (fbxScene, (skinnedMesh.name + SkinPrefix));

            FbxAMatrix fbxMeshMatrix = fbxRootNode.EvaluateGlobalTransform ();

            // keep track of the bone index -> fbx cluster mapping, so that we can add the bone weights afterwards
            Dictionary<int, FbxCluster> boneCluster = new Dictionary<int, FbxCluster> ();

            for(int i = 0; i < skinnedMesh.bones.Length; i++) {
                // ignore null bones
                if (skinnedMesh.bones[i] != null)
                {
                    FbxNode fbxBoneNode = MapUnityObjectToFbxNode[skinnedMesh.bones[i].gameObject];

                    // Create the deforming cluster
                    FbxCluster fbxCluster = FbxCluster.Create(fbxScene, "BoneWeightCluster");

                    fbxCluster.SetLink(fbxBoneNode);
                    fbxCluster.SetLinkMode(FbxCluster.ELinkMode.eNormalize);

                    boneCluster.Add(i, fbxCluster);

                    // set the Transform and TransformLink matrix
                    fbxCluster.SetTransformMatrix(fbxMeshMatrix);

                    FbxAMatrix fbxLinkMatrix = fbxBoneNode.EvaluateGlobalTransform();
                    fbxCluster.SetTransformLinkMatrix(fbxLinkMatrix);

                    // add the cluster to the skin
                    fbxSkin.AddCluster(fbxCluster);
                }
            }

            // set the vertex weights for each bone
            SetVertexWeights(meshInfo, boneCluster);

            // Add the skin to the mesh after the clusters have been added
            fbxMesh.AddDeformer (fbxSkin);

            return true;
        }

        /// <summary>
        /// set vertex weights in cluster
        /// </summary>
        private void SetVertexWeights (MeshInfo meshInfo, Dictionary<int, FbxCluster> boneCluster)
        {
            HashSet<int> visitedVertices = new HashSet<int> ();

            // set the vertex weights for each bone
            for (int i = 0; i < meshInfo.BoneWeights.Length; i++) {
                var actualIndex = ControlPointToIndex [meshInfo.Vertices [i]];

                if (visitedVertices.Contains (actualIndex)) {
                    continue;
                }
                visitedVertices.Add (actualIndex);

                var boneWeights = meshInfo.BoneWeights;
                int[] indices = {
                    boneWeights [i].boneIndex0,
                    boneWeights [i].boneIndex1,
                    boneWeights [i].boneIndex2,
                    boneWeights [i].boneIndex3
                };
                float[] weights = {
                    boneWeights [i].weight0,
                    boneWeights [i].weight1,
                    boneWeights [i].weight2,
                    boneWeights [i].weight3
                };

                for (int j = 0; j < indices.Length; j++) {
                    if (weights [j] <= 0) {
                        continue;
                    }
                    if (!boneCluster.ContainsKey (indices [j])) {
                        continue;
                    }
                    // add vertex and weighting on vertex to this bone's cluster
                    boneCluster [indices [j]].AddControlPointIndex (actualIndex, weights [j]);
                }
            }
        }

        /// <summary>
        /// Export bind pose of mesh to skeleton
        /// </summary>
        private bool ExportBindPose (SkinnedMeshRenderer skinnedMesh, FbxNode fbxMeshNode,
                                FbxScene fbxScene, Dictionary<SkinnedMeshRenderer, Transform[]> skinnedMeshToBonesMap)
        {
            if (fbxMeshNode == null || skinnedMeshToBonesMap == null || fbxScene == null)
            {
                return false;
            }

            FbxPose fbxPose = FbxPose.Create(fbxScene, fbxMeshNode.GetName());

            // set as bind pose
            fbxPose.SetIsBindPose (true);

            // assume each bone node has one weighted vertex cluster
            Transform[] bones;
            if (!skinnedMeshToBonesMap.TryGetValue (skinnedMesh, out bones)) {
                return false;
            }
            for (int i = 0; i < bones.Length; i++) {
                // ignore null bones
                if (bones[i] != null)
                {
                    FbxNode fbxBoneNode = MapUnityObjectToFbxNode[bones[i].gameObject];

                    // EvaluateGlobalTransform returns an FbxAMatrix (affine matrix)
                    // which has to be converted to an FbxMatrix so that it can be passed to fbxPose.Add().
                    // The hierarchy for FbxMatrix and FbxAMatrix is as follows:
                    //
                    //      FbxDouble4x4
                    //      /           \
                    // FbxMatrix     FbxAMatrix
                    //
                    // Therefore we can't convert directly from FbxAMatrix to FbxMatrix,
                    // however FbxMatrix has a constructor that takes an FbxAMatrix.
                    FbxMatrix fbxBindMatrix = new FbxMatrix(fbxBoneNode.EvaluateGlobalTransform());

                    fbxPose.Add(fbxBoneNode, fbxBindMatrix);
                }
            }

            fbxPose.Add (fbxMeshNode, new FbxMatrix (fbxMeshNode.EvaluateGlobalTransform ()));

            // add the pose to the scene
            fbxScene.AddPose (fbxPose);

            return true;
        }
        
        internal static FbxDouble3 ToFbxDouble3(Vector3 v)
        {
            return new FbxDouble3(v.x, v.y, v.z);
        }

        internal static FbxDouble3 ToFbxDouble3(FbxVector4 v)
        {
            return new FbxDouble3(v.X, v.Y, v.Z);
        }

        /// <summary>
        /// Euler (roll/pitch/yaw (ZXY rotation order) to quaternion.
        /// </summary>
        /// <returns>a quaternion.</returns>
        /// <param name="euler">ZXY Euler.</param>
        internal static FbxQuaternion EulerToQuaternionZXY(Vector3 euler)
        {
            var unityQuat = Quaternion.Euler(euler);
            return new FbxQuaternion(unityQuat.x, unityQuat.y, unityQuat.z, unityQuat.w);
        }

        /// <summary>
        /// Euler X/Y/Z rotation order to quaternion.
        /// </summary>
        /// <param name="euler">XYZ Euler.</param>
        /// <returns>a quaternion</returns>
        internal static FbxQuaternion EulerToQuaternionXYZ(FbxVector4 euler)
        {
            FbxAMatrix m = new FbxAMatrix ();
            m.SetR (euler);
            return m.GetQ ();
        }

        // get a fbxNode's global default position.
        internal bool ExportTransform (UnityEngine.Transform unityTransform, FbxNode fbxNode, Vector3 newCenter, TransformExportType exportType)
        {
            UnityEngine.Vector3 unityTranslate;
            FbxDouble3 fbxRotate;
            UnityEngine.Vector3 unityScale;

            switch (exportType) {
                case TransformExportType.Reset:
                    unityTranslate = Vector3.zero;
                    fbxRotate = new FbxDouble3(0);
                    unityScale = Vector3.one;
                    break;
                case TransformExportType.Global:
                    unityTranslate = GetRecenteredTranslation(unityTransform, newCenter);
                    fbxRotate = ToFbxDouble3(unityTransform.eulerAngles);
                    unityScale = unityTransform.lossyScale;
                    break;
                default: /*case TransformExportType.Local*/
                    unityTranslate = unityTransform.localPosition;
                    fbxRotate = ToFbxDouble3(unityTransform.localEulerAngles);
                    unityScale = unityTransform.localScale;
                    break;
            }

            // Transfer transform data from Unity to Fbx
            var fbxTranslate = ConvertToFbxVector4(unityTranslate, UnitScaleFactor);
            var fbxScale = new FbxDouble3 (unityScale.x, unityScale.y, unityScale.z);

            // set the local position of fbxNode
            fbxNode.LclTranslation.Set (new FbxDouble3(fbxTranslate.X, fbxTranslate.Y, fbxTranslate.Z));
            fbxNode.LclRotation.Set (fbxRotate);
            fbxNode.LclScaling.Set (fbxScale);

            return true;
        }

        /// <summary>
        /// if this game object is a model prefab or the model has already been exported, then export with shared components
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        private bool ExportInstance(GameObject unityGo, FbxScene fbxScene, FbxNode fbxNode)
        {
            if (!unityGo || fbxNode == null)
            {
                return false;
            }

            Object unityPrefabParent = PrefabUtility.GetCorrespondingObjectFromSource(unityGo);

            FbxMesh fbxMesh = null;

            if (unityPrefabParent != null && !SharedMeshes.TryGetValue (unityPrefabParent.GetInstanceID(), out fbxMesh))
            {
                if (Verbose)
                    Debug.Log (string.Format ("exporting instance {0}({1})", unityGo.name, unityPrefabParent.name));
                
                if (ExportMesh (unityGo, fbxNode) && fbxNode.GetMesh() != null) {
                    SharedMeshes [unityPrefabParent.GetInstanceID()] = fbxNode.GetMesh ();
                    return true;
                }
                return false;
            }
            // check if mesh is shared between 2 objects that are not prefabs
            else if (unityPrefabParent == null)
            {
                // check if same mesh has already been exported
                MeshFilter unityGoMesh = unityGo.GetComponent<MeshFilter>();
                if (unityGoMesh != null && MeshToFbxNodeMap.ContainsKey(unityGoMesh.sharedMesh))
                {
                    fbxMesh = MeshToFbxNodeMap[unityGoMesh.sharedMesh].GetMesh();
                }
                // export mesh as normal and add it to list
                else
                {
                    if (unityGoMesh != null)
                    {
                        MeshToFbxNodeMap.Add(unityGoMesh.sharedMesh, fbxNode);
                    }
                    return false;
                }
            }

            if (fbxMesh == null)
            {
                return false;
            }

            // We don't export the mesh because we already have it from the parent, but we still need to assign the material
            var renderer = unityGo.GetComponent<Renderer>();
            var materials = renderer ? renderer.sharedMaterials : null;

            Autodesk.Fbx.FbxSurfaceMaterial newMaterial = null;
            if (materials != null)
            {
                foreach (var mat in materials) {
                    if (MaterialMap.TryGetValue(mat.GetInstanceID(), out newMaterial))
                    {
                        fbxNode.AddMaterial(newMaterial);
                    }
                    else
                    {
                        // create new material
                        ExportMaterial(mat, fbxScene, fbxNode);
                    }
                }
            }

            // set the fbxNode containing the mesh
            fbxNode.SetNodeAttribute (fbxMesh);
            fbxNode.SetShadingMode (FbxNode.EShadingMode.eWireFrame);

            return true;
        }

        /// <summary>
        /// Exports camera component
        /// </summary>
        private bool ExportCamera (GameObject unityGO, FbxScene fbxScene, FbxNode fbxNode)
        {
            if (!unityGO || fbxScene == null || fbxNode == null)
            {
                return false;
            }

            Camera unityCamera = unityGO.GetComponent<Camera> ();
            if (unityCamera == null) {
                return false;
            }

            FbxCamera fbxCamera = FbxCamera.Create (fbxScene.GetFbxManager(), unityCamera.name);
            if (fbxCamera == null) {
                return false;
            }

            CameraVisitor.ConfigureCamera(unityCamera, fbxCamera);
                
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
        private bool ExportLight (GameObject unityGo, FbxScene fbxScene, FbxNode fbxNode)
        {
            if(!unityGo || fbxScene == null || fbxNode == null)
            {
                return false;
            }

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

        private bool ExportCommonConstraintProperties<TUnityConstraint,TFbxConstraint>(TUnityConstraint uniConstraint, TFbxConstraint fbxConstraint, FbxNode fbxNode)
            where TUnityConstraint : IConstraint where TFbxConstraint : FbxConstraint
        {
            fbxConstraint.Active.Set(uniConstraint.constraintActive);
            fbxConstraint.Lock.Set(uniConstraint.locked);
            fbxConstraint.Weight.Set(uniConstraint.weight * UnitScaleFactor);

            AddFbxNodeToConstraintsMapping(fbxNode, fbxConstraint, typeof(TUnityConstraint));
            return true;
        }

        private struct ExpConstraintSource
        {
            private FbxNode m_node;
            public FbxNode node
            {
                get { return m_node; }
                set { m_node = value; }
            }

            private float m_weight;
            public float weight
            {
                get { return m_weight; }
                set { m_weight = value; }
            }

            public ExpConstraintSource(FbxNode node, float weight)
            {
                this.m_node = node;
                this.m_weight = weight;
            }
        }

        private List<ExpConstraintSource> GetConstraintSources(IConstraint unityConstraint)
        {
            if(unityConstraint == null)
            {
                return null;
            }

            var fbxSources = new List<ExpConstraintSource>();
            var sources = new List<ConstraintSource>();
            unityConstraint.GetSources(sources);
            foreach (var source in sources)
            {
                // ignore any sources that are not getting exported
                FbxNode sourceNode;
                if (!MapUnityObjectToFbxNode.TryGetValue(source.sourceTransform.gameObject, out sourceNode))
                {
                    continue;
                }
                fbxSources.Add(new ExpConstraintSource(sourceNode, source.weight * UnitScaleFactor));
            }
            return fbxSources;
        }

        private void AddFbxNodeToConstraintsMapping<T>(FbxNode fbxNode, T fbxConstraint, System.Type uniConstraintType) where T : FbxConstraint
        {
            Dictionary<FbxConstraint, System.Type> constraintMapping;
            if (!MapConstrainedObjectToConstraints.TryGetValue(fbxNode, out constraintMapping))
            {
                constraintMapping = new Dictionary<FbxConstraint, System.Type>();
                MapConstrainedObjectToConstraints.Add(fbxNode, constraintMapping);
            }
            constraintMapping.Add(fbxConstraint, uniConstraintType);
        }

        private bool ExportPositionConstraint(IConstraint uniConstraint, FbxScene fbxScene, FbxNode fbxNode)
        {
            if(fbxNode == null)
            {
                return false;
            }

            var uniPosConstraint = uniConstraint as PositionConstraint;
            Debug.Assert (uniPosConstraint != null);

            FbxConstraintPosition fbxPosConstraint = FbxConstraintPosition.Create(fbxScene, fbxNode.GetName() + "_positionConstraint");
            fbxPosConstraint.SetConstrainedObject(fbxNode);
            var uniSources = GetConstraintSources(uniPosConstraint);
            uniSources.ForEach(uniSource => fbxPosConstraint.AddConstraintSource(uniSource.node, uniSource.weight));
            ExportCommonConstraintProperties(uniPosConstraint, fbxPosConstraint, fbxNode);

            var uniAffectedAxes = uniPosConstraint.translationAxis;
            fbxPosConstraint.AffectX.Set((uniAffectedAxes & Axis.X) == Axis.X);
            fbxPosConstraint.AffectY.Set((uniAffectedAxes & Axis.Y) == Axis.Y);
            fbxPosConstraint.AffectZ.Set((uniAffectedAxes & Axis.Z) == Axis.Z);

            var fbxTranslationOffset = ConvertToFbxVector4(uniPosConstraint.translationOffset, UnitScaleFactor);
            fbxPosConstraint.Translation.Set(ToFbxDouble3(fbxTranslationOffset));

            // rest position is the position of the fbx node
            var fbxRestTranslation = ConvertToFbxVector4(uniPosConstraint.translationAtRest, UnitScaleFactor);
            // set the local position of fbxNode
            fbxNode.LclTranslation.Set(ToFbxDouble3(fbxRestTranslation));
            return true;
        }

        private bool ExportRotationConstraint(IConstraint uniConstraint, FbxScene fbxScene, FbxNode fbxNode)
        {
            if(fbxNode == null)
            {
                return false;
            }

            var uniRotConstraint = uniConstraint as RotationConstraint;
            Debug.Assert(uniRotConstraint != null);

            FbxConstraintRotation fbxRotConstraint = FbxConstraintRotation.Create(fbxScene, fbxNode.GetName() + "_rotationConstraint");
            fbxRotConstraint.SetConstrainedObject(fbxNode);
            var uniSources = GetConstraintSources(uniRotConstraint);
            uniSources.ForEach(uniSource => fbxRotConstraint.AddConstraintSource(uniSource.node, uniSource.weight));
            ExportCommonConstraintProperties(uniRotConstraint, fbxRotConstraint, fbxNode);

            var uniAffectedAxes = uniRotConstraint.rotationAxis;
            fbxRotConstraint.AffectX.Set((uniAffectedAxes & Axis.X) == Axis.X);
            fbxRotConstraint.AffectY.Set((uniAffectedAxes & Axis.Y) == Axis.Y);
            fbxRotConstraint.AffectZ.Set((uniAffectedAxes & Axis.Z) == Axis.Z);

            // Not converting rotation offset to XYZ euler as it gives the incorrect result in both Maya and Unity.
            var uniRotationOffset = uniRotConstraint.rotationOffset;
            var fbxRotationOffset = ToFbxDouble3(uniRotationOffset);

            fbxRotConstraint.Rotation.Set(fbxRotationOffset);

            // rest rotation is the rotation of the fbx node
            var fbxRestRotation = ToFbxDouble3(uniRotConstraint.rotationAtRest);
            // set the local rotation of fbxNode
            fbxNode.LclRotation.Set(fbxRestRotation);
            return true;
        }

        private bool ExportScaleConstraint(IConstraint uniConstraint, FbxScene fbxScene, FbxNode fbxNode)
        {
            if(fbxNode == null)
            {
                return false;
            }

            var uniScaleConstraint = uniConstraint as ScaleConstraint;
            Debug.Assert(uniScaleConstraint != null);

            FbxConstraintScale fbxScaleConstraint = FbxConstraintScale.Create(fbxScene, fbxNode.GetName() + "_scaleConstraint");
            fbxScaleConstraint.SetConstrainedObject(fbxNode);
            var uniSources = GetConstraintSources(uniScaleConstraint);
            uniSources.ForEach(uniSource => fbxScaleConstraint.AddConstraintSource(uniSource.node, uniSource.weight));
            ExportCommonConstraintProperties(uniScaleConstraint, fbxScaleConstraint, fbxNode);

            var uniAffectedAxes = uniScaleConstraint.scalingAxis;
            fbxScaleConstraint.AffectX.Set((uniAffectedAxes & Axis.X) == Axis.X);
            fbxScaleConstraint.AffectY.Set((uniAffectedAxes & Axis.Y) == Axis.Y);
            fbxScaleConstraint.AffectZ.Set((uniAffectedAxes & Axis.Z) == Axis.Z);

            var uniScaleOffset = uniScaleConstraint.scaleOffset;
            var fbxScalingOffset = ToFbxDouble3(uniScaleOffset);
            fbxScaleConstraint.Scaling.Set(fbxScalingOffset);

            // rest rotation is the rotation of the fbx node
            var uniRestScale = uniScaleConstraint.scaleAtRest;
            var fbxRestScale = ToFbxDouble3(uniRestScale);
            // set the local rotation of fbxNode
            fbxNode.LclScaling.Set(fbxRestScale);
            return true;
        }

        private bool ExportAimConstraint(IConstraint uniConstraint, FbxScene fbxScene, FbxNode fbxNode)
        {
            if(fbxNode == null)
            {
                return false;
            }

            var uniAimConstraint = uniConstraint as AimConstraint;
            Debug.Assert(uniAimConstraint != null);

            FbxConstraintAim fbxAimConstraint = FbxConstraintAim.Create(fbxScene, fbxNode.GetName() + "_aimConstraint");
            fbxAimConstraint.SetConstrainedObject(fbxNode);
            var uniSources = GetConstraintSources(uniAimConstraint);
            uniSources.ForEach(uniSource => fbxAimConstraint.AddConstraintSource(uniSource.node, uniSource.weight));
            ExportCommonConstraintProperties(uniAimConstraint, fbxAimConstraint, fbxNode);

            var uniAffectedAxes = uniAimConstraint.rotationAxis;
            fbxAimConstraint.AffectX.Set((uniAffectedAxes & Axis.X) == Axis.X);
            fbxAimConstraint.AffectY.Set((uniAffectedAxes & Axis.Y) == Axis.Y);
            fbxAimConstraint.AffectZ.Set((uniAffectedAxes & Axis.Z) == Axis.Z);

            var uniRotationOffset = uniAimConstraint.rotationOffset;
            var fbxRotationOffset = ToFbxDouble3(uniRotationOffset);
            fbxAimConstraint.RotationOffset.Set(fbxRotationOffset);

            // rest rotation is the rotation of the fbx node
            var fbxRestRotation = ToFbxDouble3(uniAimConstraint.rotationAtRest);
            // set the local rotation of fbxNode
            fbxNode.LclRotation.Set(fbxRestRotation);

            FbxConstraintAim.EWorldUp fbxWorldUpType = FbxConstraintAim.EWorldUp.eAimAtNone;
            switch (uniAimConstraint.worldUpType)
            {
                case AimConstraint.WorldUpType.None:
                    fbxWorldUpType = FbxConstraintAim.EWorldUp.eAimAtNone;
                    break;
                case AimConstraint.WorldUpType.ObjectRotationUp:
                    fbxWorldUpType = FbxConstraintAim.EWorldUp.eAimAtObjectRotationUp;
                    break;
                case AimConstraint.WorldUpType.ObjectUp:
                    fbxWorldUpType = FbxConstraintAim.EWorldUp.eAimAtObjectUp;
                    break;
                case AimConstraint.WorldUpType.SceneUp:
                    fbxWorldUpType = FbxConstraintAim.EWorldUp.eAimAtSceneUp;
                    break;
                case AimConstraint.WorldUpType.Vector:
                    fbxWorldUpType = FbxConstraintAim.EWorldUp.eAimAtVector;
                    break;
                default:
                    throw new System.NotImplementedException();
            }
            fbxAimConstraint.WorldUpType.Set((int)fbxWorldUpType);
                
            var uniAimVector = ConvertToFbxVector4(uniAimConstraint.aimVector);
            fbxAimConstraint.AimVector.Set(ToFbxDouble3(uniAimVector));
            fbxAimConstraint.UpVector.Set(ToFbxDouble3(uniAimConstraint.upVector));
            fbxAimConstraint.WorldUpVector.Set(ToFbxDouble3(uniAimConstraint.worldUpVector));

            if (uniAimConstraint.worldUpObject && MapUnityObjectToFbxNode.ContainsKey(uniAimConstraint.worldUpObject.gameObject))
            {
                fbxAimConstraint.SetWorldUpObject(MapUnityObjectToFbxNode[uniAimConstraint.worldUpObject.gameObject]);
            }
            return true;
        }

        private bool ExportParentConstraint(IConstraint uniConstraint, FbxScene fbxScene, FbxNode fbxNode)
        {
            if(fbxNode == null)
            {
                return false;
            }

            var uniParentConstraint = uniConstraint as ParentConstraint;
            Debug.Assert(uniParentConstraint != null);

            FbxConstraintParent fbxParentConstraint = FbxConstraintParent.Create(fbxScene, fbxNode.GetName() + "_parentConstraint");
            fbxParentConstraint.SetConstrainedObject(fbxNode);
            var uniSources = GetConstraintSources(uniParentConstraint);
            var uniTranslationOffsets = uniParentConstraint.translationOffsets;
            var uniRotationOffsets = uniParentConstraint.rotationOffsets;
            for(int i = 0; i < uniSources.Count; i++)
            {
                var uniSource = uniSources[i];
                var uniTranslationOffset = uniTranslationOffsets[i];
                var uniRotationOffset = uniRotationOffsets[i];

                fbxParentConstraint.AddConstraintSource(uniSource.node, uniSource.weight);
                    
                var fbxTranslationOffset = ConvertToFbxVector4(uniTranslationOffset, UnitScaleFactor);
                fbxParentConstraint.SetTranslationOffset(uniSource.node, fbxTranslationOffset);
                    
                var fbxRotationOffset = ConvertToFbxVector4(uniRotationOffset);
                fbxParentConstraint.SetRotationOffset(uniSource.node, fbxRotationOffset);
            }
            ExportCommonConstraintProperties(uniParentConstraint, fbxParentConstraint, fbxNode);

            var uniTranslationAxes = uniParentConstraint.translationAxis;
            fbxParentConstraint.AffectTranslationX.Set((uniTranslationAxes & Axis.X) == Axis.X);
            fbxParentConstraint.AffectTranslationY.Set((uniTranslationAxes & Axis.Y) == Axis.Y);
            fbxParentConstraint.AffectTranslationZ.Set((uniTranslationAxes & Axis.Z) == Axis.Z);

            var uniRotationAxes = uniParentConstraint.rotationAxis;
            fbxParentConstraint.AffectRotationX.Set((uniRotationAxes & Axis.X) == Axis.X);
            fbxParentConstraint.AffectRotationY.Set((uniRotationAxes & Axis.Y) == Axis.Y);
            fbxParentConstraint.AffectRotationZ.Set((uniRotationAxes & Axis.Z) == Axis.Z);

            // rest position is the position of the fbx node
            var fbxRestTranslation = ConvertToFbxVector4(uniParentConstraint.translationAtRest, UnitScaleFactor);
            // set the local position of fbxNode
            fbxNode.LclTranslation.Set(ToFbxDouble3(fbxRestTranslation));

            // rest rotation is the rotation of the fbx node
            var fbxRestRotation = ToFbxDouble3(uniParentConstraint.rotationAtRest);
            // set the local rotation of fbxNode
            fbxNode.LclRotation.Set(fbxRestRotation);
            return true;
        }

        private delegate bool ExportConstraintDelegate(IConstraint c , FbxScene fs, FbxNode fn);

        private bool ExportConstraints (GameObject unityGo, FbxScene fbxScene, FbxNode fbxNode)
        {
            if (!unityGo)
            {
                return false;
            }

            var mapConstraintTypeToExportFunction = new Dictionary<System.Type, ExportConstraintDelegate>()
            {
                { typeof(PositionConstraint), ExportPositionConstraint },
                { typeof(RotationConstraint), ExportRotationConstraint },
                { typeof(ScaleConstraint), ExportScaleConstraint },
                { typeof(AimConstraint), ExportAimConstraint },
                { typeof(ParentConstraint), ExportParentConstraint }
            };

            // check if GameObject has one of the 5 supported constraints: aim, parent, position, rotation, scale
            var uniConstraints = unityGo.GetComponents<IConstraint>();

            foreach(var uniConstraint in uniConstraints)
            {
                var uniConstraintType = uniConstraint.GetType();
                ExportConstraintDelegate constraintDelegate;
                if (!mapConstraintTypeToExportFunction.TryGetValue(uniConstraintType, out constraintDelegate))
                {
                    Debug.LogWarningFormat("FbxExporter: Missing function to export constraint of type {0}", uniConstraintType.Name);
                    continue;
                }
                constraintDelegate(uniConstraint, fbxScene, fbxNode);
            }

            return true;
        }

        /// <summary>
        /// Return set of sample times to cover all keys on animation curves
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        internal static HashSet<float> GetSampleTimes(AnimationCurve[] animCurves, double sampleRate)
        {
            var keyTimes = new HashSet<float>();
            double fs = 1.0/sampleRate;

            double firstTime = double.MaxValue, lastTime = double.MinValue;

            foreach (var ac in animCurves)
            {
                if (ac==null || ac.length<=0) continue;

                firstTime = System.Math.Min(firstTime, ac[0].time);
                lastTime = System.Math.Max(lastTime, ac[ac.length-1].time);
            }

            // if these values didn't get set there were no valid anim curves,
            // so don't return any keys
            if(firstTime == double.MaxValue || lastTime == double.MinValue)
            {
                return keyTimes;
            }

            int firstframe = (int)System.Math.Floor(firstTime * sampleRate);
            int lastframe = (int)System.Math.Ceiling(lastTime * sampleRate);
            for (int i = firstframe; i <= lastframe; i++) {
                keyTimes.Add ((float)(i * fs));
            }

            return keyTimes;
        }

        /// <summary>
        /// Return set of all keys times on animation curves
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        internal static HashSet<float> GetKeyTimes(AnimationCurve[] animCurves)
        {
            var keyTimes = new HashSet<float>();

            foreach (var ac in animCurves)
            {
                if (ac!=null) foreach(var key in ac.keys) { keyTimes.Add(key.time); }
            }

            return keyTimes;
        }

        /// <summary>
        /// Export animation curve key frames with key tangents 
        /// NOTE : This is a work in progress (WIP). We only export the key time and value on
        /// a Cubic curve using the default tangents.
        /// </summary>
        internal static void ExportAnimationKeys (AnimationCurve uniAnimCurve, FbxAnimCurve fbxAnimCurve, 
            UnityToMayaConvertSceneHelper convertSceneHelper)
        {
            // Copy Unity AnimCurve to FBX AnimCurve.
            // NOTE: only cubic keys are supported by the FbxImporter
            using (new FbxAnimCurveModifyHelper(new List<FbxAnimCurve>{fbxAnimCurve}))
            {
                for (int keyIndex = 0; keyIndex < uniAnimCurve.length; ++keyIndex) 
                {
                    var uniKeyFrame = uniAnimCurve [keyIndex];
                    var fbxTime = FbxTime.FromSecondDouble (uniKeyFrame.time);

                    int fbxKeyIndex = fbxAnimCurve.KeyAdd (fbxTime);


                    // configure tangents
                    var lTangent = AnimationUtility.GetKeyLeftTangentMode(uniAnimCurve, keyIndex);
                    var rTangent = AnimationUtility.GetKeyRightTangentMode(uniAnimCurve, keyIndex);

                    // Always set tangent mode to eTangentBreak, as other modes are not handled the same in FBX as in
                    // Unity, thus leading to discrepancies in animation curves.
                    FbxAnimCurveDef.ETangentMode tanMode = FbxAnimCurveDef.ETangentMode.eTangentBreak;

                    // Default to cubic interpolation, which is the default for KeySet
                    FbxAnimCurveDef.EInterpolationType interpMode = FbxAnimCurveDef.EInterpolationType.eInterpolationCubic;
                    switch (rTangent)
                    {
                        case AnimationUtility.TangentMode.Linear:
                            interpMode = FbxAnimCurveDef.EInterpolationType.eInterpolationLinear;
                            break;
                        case AnimationUtility.TangentMode.Constant:
                            interpMode = FbxAnimCurveDef.EInterpolationType.eInterpolationConstant;
                            break;
                        default:
                            break;
                    }

                    fbxAnimCurve.KeySet (fbxKeyIndex, 
                        fbxTime, 
                        convertSceneHelper.Convert(uniKeyFrame.value),
                        interpMode,
                        tanMode,
                        // value of right slope
                        convertSceneHelper.Convert(uniKeyFrame.outTangent),
                        // value of next left slope
                        keyIndex < uniAnimCurve.length -1 ? convertSceneHelper.Convert(uniAnimCurve[keyIndex+1].inTangent) : 0,
                        FbxAnimCurveDef.EWeightedMode.eWeightedAll,
                        // weight for right slope
                        uniKeyFrame.outWeight,
                        // weight for next left slope
                        keyIndex < uniAnimCurve.length - 1 ? uniAnimCurve[keyIndex + 1].inWeight : 0
                    );
                }
            }
        }

        /// <summary>
        /// Export animation curve key samples
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        internal void ExportAnimationSamples (AnimationCurve uniAnimCurve, FbxAnimCurve fbxAnimCurve,
            double sampleRate,
            UnityToMayaConvertSceneHelper convertSceneHelper)
        {
                
            using (new FbxAnimCurveModifyHelper(new List<FbxAnimCurve>{fbxAnimCurve}))
            {
                foreach (var currSampleTime in GetSampleTimes(new AnimationCurve[]{uniAnimCurve}, sampleRate)) 
                {
                    float currSampleValue = uniAnimCurve.Evaluate((float)currSampleTime);

                    var fbxTime = FbxTime.FromSecondDouble (currSampleTime);

                    int fbxKeyIndex = fbxAnimCurve.KeyAdd (fbxTime);

                    fbxAnimCurve.KeySet (fbxKeyIndex, 
                        fbxTime, 
                        convertSceneHelper.Convert(currSampleValue)
                    );
                }
            }
        }

        /// <summary>
        /// Get the FbxConstraint associated with the constrained node.
        /// </summary>
        /// <param name="constrainedNode"></param>
        /// <param name="uniConstraintType"></param>
        /// <returns></returns>
        private FbxConstraint GetFbxConstraint(FbxNode constrainedNode, System.Type uniConstraintType)
        {
            if (uniConstraintType == null || !uniConstraintType.GetInterfaces().Contains(typeof(IConstraint)))
            {
                // not actually a constraint
                return null;
            }

            Dictionary<FbxConstraint, System.Type> constraints;
            if (!MapConstrainedObjectToConstraints.TryGetValue(constrainedNode, out constraints))
            {
                return null;
            }
                
            foreach (var constraint in constraints)
            {
                if (uniConstraintType == constraint.Value)
                {
                    return constraint.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the FbxBlendshape with the given name associated with the FbxNode.
        /// </summary>
        /// <param name="blendshapeNode"></param>
        /// <param name="uniPropertyName"></param>
        /// <returns></returns>
        private FbxBlendShapeChannel GetFbxBlendShape(FbxNode blendshapeNode, string uniPropertyName)
        {
            Dictionary<FbxBlendShapeChannel, string> blendshapeChannels;
            if (!MapUnityObjectToBlendShapes.TryGetValue(blendshapeNode, out blendshapeChannels))
            {
                return null;
            }

            foreach (var channel in blendshapeChannels)
            {
                if (uniPropertyName == ("blendShape." + channel.Value))
                {
                    return channel.Key;
                }
            }
            return null;
        }

        private FbxProperty GetFbxProperty(FbxNode fbxNode, string fbxPropertyName, System.Type uniPropertyType, string uniPropertyName)
        {
            if(fbxNode == null)
            {
                return null;
            }

            // check if property maps to a constraint
            // check this first because both constraints and FbxNodes can contain a RotationOffset property,
            // but only the constraint one is animatable.
            var fbxConstraint = GetFbxConstraint(fbxNode, uniPropertyType);
            if(fbxConstraint != null)
            {
                var prop = fbxConstraint.FindProperty(fbxPropertyName, false);
                if (prop.IsValid())
                {
                    return prop;
                }
            }

            // check if the property maps to a blendshape
            var fbxBlendShape = GetFbxBlendShape(fbxNode, uniPropertyName);
            if (fbxBlendShape != null)
            {
                var prop = fbxBlendShape.FindProperty(fbxPropertyName, false);
                if (prop.IsValid())
                {
                    return prop;
                }
            }

            // map unity property name to fbx property
            var fbxProperty = fbxNode.FindProperty(fbxPropertyName, false);
            if (fbxProperty.IsValid())
            {
                return fbxProperty;
            }

            var fbxNodeAttribute = fbxNode.GetNodeAttribute();
            if (fbxNodeAttribute != null)
            {
                fbxProperty = fbxNodeAttribute.FindProperty(fbxPropertyName, false);
            }
            return fbxProperty;
        }

        /// <summary>
        /// Export an AnimationCurve.
        /// NOTE: This is not used for rotations, because we need to convert from
        /// quaternion to euler and various other stuff.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        private void ExportAnimationCurve (FbxNode fbxNode,
                                                AnimationCurve uniAnimCurve,
                                                float frameRate,
                                                string uniPropertyName,
                                                System.Type uniPropertyType,
                                                FbxAnimLayer fbxAnimLayer)
        {
            if(fbxNode == null)
            {
                return;
            }

            if (Verbose) {
                Debug.Log ("Exporting animation for " + fbxNode.GetName() + " (" + uniPropertyName + ")");
            }

            var fbxConstraint = GetFbxConstraint(fbxNode, uniPropertyType);
            FbxPropertyChannelPair[] fbxPropertyChannelPairs;
            if (!FbxPropertyChannelPair.TryGetValue (uniPropertyName, out fbxPropertyChannelPairs, fbxConstraint)) {
                Debug.LogWarning(string.Format("no mapping from Unity '{0}' to fbx property", uniPropertyName));
                return;
            }

            foreach (var fbxPropertyChannelPair in fbxPropertyChannelPairs) {
                // map unity property name to fbx property
                var fbxProperty = GetFbxProperty(fbxNode, fbxPropertyChannelPair.Property, uniPropertyType, uniPropertyName);
                if (!fbxProperty.IsValid ()) 
                {
                    Debug.LogError (string.Format ("no fbx property {0} found on {1} node or nodeAttribute ", fbxPropertyChannelPair.Property, fbxNode.GetName ()));
                    return;
                }
                if (!fbxProperty.GetFlag(FbxPropertyFlags.EFlags.eAnimatable))
                {
                    Debug.LogErrorFormat("fbx property {0} found on node {1} is not animatable", fbxPropertyChannelPair.Property, fbxNode.GetName());
                }

                // Create the AnimCurve on the channel
                FbxAnimCurve fbxAnimCurve = fbxProperty.GetCurve (fbxAnimLayer, fbxPropertyChannelPair.Channel, true);
                if(fbxAnimCurve == null)
                {
                    return;
                }

                // create a convert scene helper so that we can convert from Unity to Maya
                // AxisSystem (LeftHanded to RightHanded) and FBX's default units 
                // (Meters to Centimetres)
                var convertSceneHelper = new UnityToMayaConvertSceneHelper (uniPropertyName, fbxNode);
                
                if (ModelExporter.ExportSettings.BakeAnimationProperty) {
                    ExportAnimationSamples (uniAnimCurve, fbxAnimCurve, frameRate, convertSceneHelper);
                } else {
                    ExportAnimationKeys (uniAnimCurve, fbxAnimCurve, convertSceneHelper);
                }
            }
        }

        internal class UnityToMayaConvertSceneHelper
        {
            bool convertDistance = false;
            bool convertToRadian = false;
            bool convertLensShiftX = false;
            bool convertLensShiftY = false;

            FbxCamera camera = null;

            float unitScaleFactor = 1f;

            public UnityToMayaConvertSceneHelper(string uniPropertyName, FbxNode fbxNode)
            {
                System.StringComparison cc = System.StringComparison.CurrentCulture;

                bool partT = uniPropertyName.StartsWith("m_LocalPosition.", cc) || uniPropertyName.StartsWith("m_TranslationOffset", cc);

                convertDistance |= partT;
                convertDistance |= uniPropertyName.StartsWith ("m_Intensity", cc);
                convertDistance |= uniPropertyName.ToLower().EndsWith("weight", cc);
                convertLensShiftX |= uniPropertyName.StartsWith("m_LensShift.x", cc);
                convertLensShiftY |= uniPropertyName.StartsWith("m_LensShift.y", cc);
                if (convertLensShiftX || convertLensShiftY)
                {
                    camera = fbxNode.GetCamera();
                }

                // The ParentConstraint's source Rotation Offsets are read in as radians, so make sure they are exported as radians
                convertToRadian = uniPropertyName.StartsWith("m_RotationOffsets.Array.data", cc);

                if (convertDistance)
                    unitScaleFactor = ModelExporter.UnitScaleFactor;

                if (convertToRadian)
                {
                    unitScaleFactor *= (Mathf.PI / 180);
                }
            }

            public float Convert(float value)
            {
                float convertedValue = value;
                if (convertLensShiftX || convertLensShiftY)
                {
                    convertedValue = Mathf.Clamp(Mathf.Abs(value), 0f, 1f)*Mathf.Sign(value);
                }
                if (camera != null)
                {
                    if (convertLensShiftX)
                    {
                        convertedValue *= (float)camera.GetApertureWidth();
                    }
                    else if (convertLensShiftY)
                    {
                        convertedValue *= (float)camera.GetApertureHeight();
                    }
                }

                // left handed to right handed conversion
                // meters to centimetres conversion
                return unitScaleFactor * convertedValue;
            }

        }

        /// <summary>
        /// Export an AnimationClip as a single take
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        private void ExportAnimationClip (AnimationClip uniAnimClip, GameObject uniRoot, FbxScene fbxScene)
        {
            if (!uniAnimClip || !uniRoot || fbxScene == null) return;

            if (Verbose)
                Debug.Log (string.Format ("Exporting animation clip ({1}) for {0}", uniRoot.name, uniAnimClip.name));

            // setup anim stack
            FbxAnimStack fbxAnimStack = FbxAnimStack.Create (fbxScene, uniAnimClip.name);
            fbxAnimStack.Description.Set ("Animation Take: " + uniAnimClip.name);

            // add one mandatory animation layer
            FbxAnimLayer fbxAnimLayer = FbxAnimLayer.Create (fbxScene, "Animation Base Layer");
            fbxAnimStack.AddMember (fbxAnimLayer);

            // Set up the FPS so our frame-relative math later works out
            // Custom frame rate isn't really supported in FBX SDK (there's
            // a bug), so try hard to find the nearest time mode.
            FbxTime.EMode timeMode = FbxTime.EMode.eCustom;
            double precision = 1e-6;
            while (timeMode == FbxTime.EMode.eCustom && precision < 1000) {
                timeMode = FbxTime.ConvertFrameRateToTimeMode (uniAnimClip.frameRate, precision);
                precision *= 10;
            }
            if (timeMode == FbxTime.EMode.eCustom) {
                timeMode = FbxTime.EMode.eFrames30;
            }

            fbxScene.GetGlobalSettings ().SetTimeMode (timeMode);

            // set time correctly
            var fbxStartTime = FbxTime.FromSecondDouble (0);
            var fbxStopTime = FbxTime.FromSecondDouble (uniAnimClip.length);

            fbxAnimStack.SetLocalTimeSpan (new FbxTimeSpan (fbxStartTime, fbxStopTime));

            var unityCurves = new Dictionary<GameObject, List<UnityCurve>> ();

            // extract and store all necessary information from the curve bindings, namely the animation curves
            // and their corresponding property names for each GameObject.
            foreach (EditorCurveBinding uniCurveBinding in AnimationUtility.GetCurveBindings (uniAnimClip)) {
                Object uniObj = AnimationUtility.GetAnimatedObject (uniRoot, uniCurveBinding);
                if (!uniObj) {
                    continue;
                }

                AnimationCurve uniAnimCurve = AnimationUtility.GetEditorCurve (uniAnimClip, uniCurveBinding);
                if (uniAnimCurve == null) {
                    continue;
                }

                var uniGO = GetGameObject (uniObj);
                // Check if the GameObject has an FBX node to the animation. It might be null because the LOD selected doesn't match the one on the gameobject. 
                if (!uniGO || MapUnityObjectToFbxNode.ContainsKey(uniGO) == false) {
                    continue;
                }

                if (unityCurves.ContainsKey (uniGO)) {
                    unityCurves [uniGO].Add (new UnityCurve(uniCurveBinding.propertyName, uniAnimCurve, uniCurveBinding.type));
                    continue;
                }
                unityCurves.Add (uniGO, new List<UnityCurve> (){ new UnityCurve(uniCurveBinding.propertyName, uniAnimCurve, uniCurveBinding.type) });
            }

            // transfer root motion
            var animSource = ExportOptions.AnimationSource;
            var animDest = ExportOptions.AnimationDest;
            if (animSource && animDest && animSource != animDest) {
                // list of all transforms between source and dest, including source and dest
                var transformsFromSourceToDest = new List<Transform> ();
                var curr = animDest;
                while (curr != animSource) {
                    transformsFromSourceToDest.Add (curr);
                    curr = curr.parent;
                }
                transformsFromSourceToDest.Add (animSource);
                transformsFromSourceToDest.Reverse ();

                // while there are 2 transforms in the list, transfer the animation from the
                // first to the next transform.
                // Then remove the first transform from the list.
                while (transformsFromSourceToDest.Count >= 2) {
                    var source = transformsFromSourceToDest [0];
                    transformsFromSourceToDest.RemoveAt (0);
                    var dest = transformsFromSourceToDest [0];

                    TransferMotion (source, dest, uniAnimClip.frameRate, ref unityCurves);
                }
            }

            /* The major difficulty: Unity uses quaternions for rotation
                * (which is how it should be) but FBX uses Euler angles. So we
                * need to gather up the list of transform curves per object.
                * 
                * For euler angles, Unity uses ZXY rotation order while Maya uses XYZ.
                * Maya doesn't import files with ZXY rotation correctly, so have to convert to XYZ.
                * Need all 3 curves in order to convert.
                * 
                * Also, in both cases, prerotation has to be removed from the animated rotation if
                * there are bones being exported.
                */
            var rotations = new Dictionary<GameObject, RotationCurve>();

            // export the animation curves for each GameObject that has animation
            foreach (var kvp in unityCurves) {
                var uniGO = kvp.Key;
                foreach (var uniCurve in kvp.Value) {
                    var propertyName = uniCurve.propertyName;
                    var uniAnimCurve = uniCurve.uniAnimCurve;

                    // Do not create the curves if the component is a SkinnedMeshRenderer and if the option in FBX Export settings is toggled on.
                    if (!ExportOptions.AnimateSkinnedMesh && (uniGO.GetComponent<SkinnedMeshRenderer> () != null)) {
                        continue;    
                    }

                    FbxNode fbxNode;
                    if (!MapUnityObjectToFbxNode.TryGetValue(uniGO, out fbxNode))
                    {
                        Debug.LogError(string.Format("no FbxNode found for {0}", uniGO.name));
                        continue;
                    }

                    int index = QuaternionCurve.GetQuaternionIndex (propertyName);
                    if (index >= 0) {
                        // Rotation property; save it to convert quaternion -> euler later.
                        RotationCurve rotCurve = GetRotationCurve<QuaternionCurve> (uniGO, uniAnimClip.frameRate, ref rotations);
                        rotCurve.SetCurve (index, uniAnimCurve);
                        continue;
                    } 

                    // If this is an euler curve with a prerotation, then need to sample animations to remove the prerotation.
                    // Otherwise can export normally with tangents.
                    index = EulerCurve.GetEulerIndex (propertyName);
                    if (index >= 0 && 
                        // still need to sample euler curves if baking is specified
                        (ModelExporter.ExportSettings.BakeAnimationProperty ||
                        // also need to make sure to sample if there is a prerotation, as this is baked into the Unity curves
                        fbxNode.GetPreRotation(FbxNode.EPivotSet.eSourcePivot).Distance(new FbxVector4()) > 0)) {

                        RotationCurve rotCurve = GetRotationCurve<EulerCurve> (uniGO, uniAnimClip.frameRate, ref rotations);
                        rotCurve.SetCurve (index, uniAnimCurve);
                        continue;
                    }

                    // simple property (e.g. intensity), export right away
                    ExportAnimationCurve (fbxNode, uniAnimCurve, uniAnimClip.frameRate, 
                        propertyName, uniCurve.propertyType,
                        fbxAnimLayer);
                }
            }

            // now export all the quaternion curves 
            foreach (var kvp in rotations) {
                var unityGo = kvp.Key;
                var rot = kvp.Value;

                FbxNode fbxNode;
                if (!MapUnityObjectToFbxNode.TryGetValue (unityGo, out fbxNode)) {
                    Debug.LogError (string.Format ("no FbxNode found for {0}", unityGo.name));
                    continue;
                }
                rot.Animate (unityGo.transform, fbxNode, fbxAnimLayer, Verbose);
            }
        }

        /// <summary>
        /// Transfers transform animation from source to dest. Replaces dest's Unity Animation Curves with updated animations.
        /// NOTE: Source must be the parent of dest.
        /// </summary>
        /// <param name="source">Source animated object.</param>
        /// <param name="dest">Destination, child of the source.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="unityCurves">Unity curves.</param>
        [SecurityPermission(SecurityAction.LinkDemand)]
        private void TransferMotion(Transform source, Transform dest, float sampleRate, ref Dictionary<GameObject, List<UnityCurve>> unityCurves){
            // get sample times for curves in dest + source
            // at each sample time, evaluate all 18 transfom anim curves, creating 2 transform matrices
            // combine the matrices, get the new values, apply to the 9 new anim curves for dest
            if (dest.parent != source) {
                Debug.LogError ("dest must be a child of source");
                return;
            }

            List<UnityCurve> sourceUnityCurves;
            if (!unityCurves.TryGetValue (source.gameObject, out sourceUnityCurves)) {
                return; // nothing to do, source has no animation
            }

            List<UnityCurve> destUnityCurves;
            if (!unityCurves.TryGetValue (dest.gameObject, out destUnityCurves)) {
                destUnityCurves = new List<UnityCurve> ();
            }

            List<AnimationCurve> animCurves = new List<AnimationCurve> ();
            foreach (var curve in sourceUnityCurves) {
                // TODO: check if curve is anim related
                animCurves.Add(curve.uniAnimCurve);
            }
            foreach (var curve in destUnityCurves) {
                animCurves.Add (curve.uniAnimCurve);
            }

            var sampleTimes = GetSampleTimes (animCurves.ToArray (), sampleRate);
            // need to create 9 new UnityCurves, one for each property
            var posKeyFrames = new Keyframe[3][];
            var rotKeyFrames = new Keyframe[3][];
            var scaleKeyFrames = new Keyframe[3][];

            for (int k = 0; k < posKeyFrames.Length; k++) {
                posKeyFrames [k] = new Keyframe[sampleTimes.Count];
                rotKeyFrames[k] = new Keyframe[sampleTimes.Count];
                scaleKeyFrames[k] = new Keyframe[sampleTimes.Count];
            }

            // If we have a point in local coords represented as a column-vector x, the equation of x in coordinates relative to source's parent is:
            //   x_grandparent = source * dest * x
            // Now we're going to change dest to dest' which has the animation from source. And we're going to change
            // source to source' which has no animation. The equation of x will become:
            //   x_grandparent = source' * dest' * x
            // We're not changing x_grandparent and x, so we need that:
            //   source * dest = source' * dest'
            // We know dest and source (both animated) and source' (static). Solve for dest':
            //   dest' = (source')^-1 * source * dest
            int keyIndex = 0;
            var sourceStaticMatrixInverse = Matrix4x4.TRS(source.localPosition, source.localRotation, source.localScale).inverse;
            foreach (var currSampleTime in sampleTimes) 
            {
                var sourceLocalMatrix = GetTransformMatrix (currSampleTime, source, sourceUnityCurves);
                var destLocalMatrix = GetTransformMatrix (currSampleTime, dest, destUnityCurves);

                var newLocalMatrix = sourceStaticMatrixInverse * sourceLocalMatrix * destLocalMatrix;

                FbxVector4 translation, rotation, scale;
                GetTRSFromMatrix (newLocalMatrix, out translation, out rotation, out scale);

                // get rotation directly from matrix, as otherwise causes issues
                // with negative rotations.
                var rot = newLocalMatrix.rotation.eulerAngles;

                for (int k = 0; k < 3; k++) {
                    posKeyFrames [k][keyIndex] = new Keyframe(currSampleTime, (float)translation [k]);
                    rotKeyFrames [k][keyIndex] = new Keyframe(currSampleTime, rot [k]);
                    scaleKeyFrames [k][keyIndex] = new Keyframe(currSampleTime, (float)scale [k]);
                }
                keyIndex++;
            }

            // create the new list of unity curves, and add it to dest's curves
            var newUnityCurves = new List<UnityCurve>();
            string posPropName = "m_LocalPosition.";
            string rotPropName = "localEulerAnglesRaw.";
            string scalePropName = "m_LocalScale.";
            var xyz = "xyz";
            for (int k = 0; k < 3; k++) {
                var posUniCurve = new UnityCurve ( posPropName + xyz[k], new AnimationCurve(posKeyFrames[k]), typeof(Transform));
                newUnityCurves.Add (posUniCurve);

                var rotUniCurve = new UnityCurve ( rotPropName + xyz[k], new AnimationCurve(rotKeyFrames[k]), typeof(Transform));
                newUnityCurves.Add (rotUniCurve);

                var scaleUniCurve = new UnityCurve ( scalePropName + xyz[k], new AnimationCurve(scaleKeyFrames[k]), typeof(Transform));
                newUnityCurves.Add (scaleUniCurve);
            }

            // remove old transform curves
            RemoveTransformCurves (ref sourceUnityCurves);
            RemoveTransformCurves (ref destUnityCurves);

            unityCurves [source.gameObject] = sourceUnityCurves;
            if (!unityCurves.ContainsKey(dest.gameObject)) {
                unityCurves.Add (dest.gameObject, newUnityCurves);
                return;
            }
            unityCurves [dest.gameObject].AddRange(newUnityCurves);

        }


        private void RemoveTransformCurves(ref List<UnityCurve> curves){
            var transformCurves = new List<UnityCurve> ();
            var transformPropNames = new string[]{"m_LocalPosition.", "m_LocalRotation", "localEulerAnglesRaw.", "m_LocalScale."};
            foreach (var curve in curves) {
                foreach (var prop in transformPropNames) {
                    if (curve.propertyName.StartsWith (prop)) {
                        transformCurves.Add (curve);
                        break;
                    }
                }
            }
            foreach (var curve in transformCurves) {
                curves.Remove (curve);
            }
        }

        private Matrix4x4 GetTransformMatrix(float currSampleTime, Transform orig, List<UnityCurve> unityCurves){
            var sourcePos = orig.localPosition;
            var sourceRot = orig.localRotation;
            var sourceScale = orig.localScale;

            foreach (var uniCurve in unityCurves) {
                float currSampleValue = uniCurve.uniAnimCurve.Evaluate(currSampleTime);
                string propName = uniCurve.propertyName;
                // try position, scale, quat then euler
                int temp = QuaternionCurve.GetQuaternionIndex(propName);
                if (temp >= 0) {
                    sourceRot [temp] = currSampleValue;
                    continue;
                }
                temp = EulerCurve.GetEulerIndex (propName);
                if (temp >= 0) {
                    var euler = sourceRot.eulerAngles;
                    euler [temp] = currSampleValue;
                    sourceRot.eulerAngles = euler;
                    continue;
                }
                temp = GetPositionIndex (propName);
                if (temp >= 0) {
                    sourcePos [temp] = currSampleValue;
                    continue;
                }
                temp = GetScaleIndex (propName);
                if (temp >= 0) {
                    sourceScale [temp] = currSampleValue;
                }
            }

            sourceRot = Quaternion.Euler(sourceRot.eulerAngles.x, sourceRot.eulerAngles.y, sourceRot.eulerAngles.z);
            return Matrix4x4.TRS(sourcePos, sourceRot, sourceScale); 
        }

        internal struct UnityCurve {
            public string propertyName;
            public AnimationCurve uniAnimCurve;
            public System.Type propertyType;

            public UnityCurve(string propertyName, AnimationCurve uniAnimCurve, System.Type propertyType){
                this.propertyName = propertyName;
                this.uniAnimCurve = uniAnimCurve;
                this.propertyType = propertyType;
            }
        }

        private int GetPositionIndex(string uniPropertyName){
            System.StringComparison ct = System.StringComparison.CurrentCulture;
            bool isPositionComponent = uniPropertyName.StartsWith ("m_LocalPosition.", ct);

            if (!isPositionComponent) { return -1; }

            switch (uniPropertyName [uniPropertyName.Length - 1]) {
            case 'x':
                return 0;
            case 'y':
                return 1;
            case 'z':
                return 2;
            default:
                return -1;
            }
        }

        private int GetScaleIndex(string uniPropertyName){
            System.StringComparison ct = System.StringComparison.CurrentCulture;
            bool isScaleComponent = uniPropertyName.StartsWith ("m_LocalScale.", ct);

            if (!isScaleComponent) { return -1; }

            switch (uniPropertyName [uniPropertyName.Length - 1]) {
            case 'x':
                return 0;
            case 'y':
                return 1;
            case 'z':
                return 2;
            default:
                return -1;
            }
        }

        /// <summary>
        /// Gets or creates the rotation curve for GameObject uniGO.
        /// </summary>
        /// <returns>The rotation curve.</returns>
        /// <param name="uniGO">Unity GameObject.</param>
        /// <param name="frameRate">Frame rate.</param>
        /// <param name="rotations">Rotations.</param>
        /// <typeparam name="T"> RotationCurve is abstract so specify type of RotationCurve to create.</typeparam>
        private RotationCurve GetRotationCurve<T>(
            GameObject uniGO, float frameRate,
            ref Dictionary<GameObject, RotationCurve> rotations
            ) where T : RotationCurve, new()
        {
            RotationCurve rotCurve;
            if (!rotations.TryGetValue (uniGO, out rotCurve)) {
                rotCurve = new T { SampleRate = frameRate };
                rotations.Add (uniGO, rotCurve);
            }
            return rotCurve;
        }

        /// <summary>
        /// Export the Animator component on this game object
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        private void ExportAnimation (GameObject uniRoot, FbxScene fbxScene)
        {
            if (!uniRoot)
            {
                return;
            }

            var exportedClips = new HashSet<AnimationClip> ();

            var uniAnimator = uniRoot.GetComponent<Animator> ();
            if (uniAnimator)
            { 
                // Try the animator controller (mecanim)
                var controller = uniAnimator.runtimeAnimatorController;

                if (controller) 
                { 
                    // Only export each clip once per game object.
                    foreach (var clip in controller.animationClips) {
                        if (exportedClips.Add (clip)) {
                            ExportAnimationClip (clip, uniRoot, fbxScene);
                        }
                    }
                }
            }

            // Try the playable director
            var director = uniRoot.GetComponent<UnityEngine.Playables.PlayableDirector> ();
            if (director)
            {
                Debug.LogWarning(string.Format("Exporting animation from PlayableDirector on {0} not supported", uniRoot.name));
                // TODO: export animationclips from playabledirector
            }

            // Try the animation (legacy)
            var uniAnimation = uniRoot.GetComponent<Animation> ();
            if (uniAnimation) 
            { 
                // Only export each clip once per game object.
                foreach (var uniAnimObj in uniAnimation) {
                    AnimationState uniAnimState = uniAnimObj as AnimationState;
                    if (uniAnimState)
                    {
                        AnimationClip uniAnimClip = uniAnimState.clip;
                        if (exportedClips.Add (uniAnimClip)) {
                            ExportAnimationClip (uniAnimClip, uniRoot, fbxScene);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// configures default camera for the scene
        /// </summary>
        private void SetDefaultCamera (FbxScene fbxScene)
        {
            if(fbxScene == null) { return; }

            if (string.IsNullOrEmpty(DefaultCamera))
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
        /// <param name="nameToCountMap">The dictionary to use to map name to # of occurences</param>
        private string GetUniqueName(string name, Dictionary<string, int> nameToCountMap)
        {
            var uniqueName = name;
            int count;
            if (nameToCountMap.TryGetValue(name, out count))
            {
                uniqueName = string.Format(UniqueNameFormat, name, count);
            }
            else
            {
                count = 0;
            }
            nameToCountMap[name] = count + 1;
            return uniqueName;
        }

        /// <summary>
        /// Ensures that the inputted name is unique.
        /// If a duplicate name is found, then it is incremented.
        /// e.g. Sphere becomes Sphere_1
        /// </summary>
        /// <returns>Unique name</returns>
        /// <param name="name">Name</param>
        private string GetUniqueFbxNodeName(string name)
        {
            return GetUniqueName(name, NameToIndexMap);
        }

        /// <summary>
        /// Ensures that the inputted material name is unique.
        /// If a duplicate name is found, then it is incremented.
        /// e.g. mat becomes mat_1
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Unique material name</returns>
        private string GetUniqueMaterialName(string name)
        {
            return GetUniqueName(name, MaterialNameToIndexMap);
        }

        /// <summary>
        /// Ensures that the inputted texture name is unique.
        /// If a duplicate name is found, then it is incremented.
        /// e.g. tex becomes tex_1
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Unique texture name</returns>
        private string GetUniqueTextureName(string name)
        {
            return GetUniqueName(name, TextureNameToIndexMap);
        }

        /// <summary>
        /// Create a fbxNode from unityGo.
        /// </summary>
        /// <param name="unityGo"></param>
        /// <param name="fbxScene"></param>
        /// <returns>the created FbxNode</returns>
        private FbxNode CreateFbxNode(GameObject unityGo, FbxScene fbxScene)
        {

            string fbxName = unityGo.name;
            if (ExportOptions.UseMayaCompatibleNames)
            {
                fbxName = ConvertToMayaCompatibleName(unityGo.name);
                if (ExportOptions.AllowSceneModification)
                {
                    unityGo.name = fbxName;
                }
            }
            
            FbxNode fbxNode = FbxNode.Create(fbxScene, GetUniqueFbxNodeName(fbxName));

            // Default inheritance type in FBX is RrSs, which causes scaling issues in Maya as
            // both Maya and Unity use RSrs inheritance by default.
            // Note: MotionBuilder uses RrSs inheritance by default as well, though it is possible
            //       to select a different inheritance type in the UI.
            // Use RSrs as the scaling inheritance instead.
            fbxNode.SetTransformationInheritType(FbxTransform.EInheritType.eInheritRSrs);

            // Fbx rotation order is XYZ, but Unity rotation order is ZXY.
            // Also, DeepConvert does not convert the rotation order (assumes XYZ), unless RotationActive is true.
            fbxNode.SetRotationOrder(FbxNode.EPivotSet.eSourcePivot, FbxEuler.EOrder.eOrderZXY);
            fbxNode.SetRotationActive(true);

            MapUnityObjectToFbxNode[unityGo] = fbxNode;

            return fbxNode;
        }

        /// <summary>
        /// Creates an FbxNode for each GameObject.
        /// </summary>
        /// <returns>The number of nodes exported.</returns>
        internal int ExportTransformHierarchy(
            GameObject  unityGo, FbxScene fbxScene, FbxNode fbxNodeParent,
            int exportProgress, int objectCount, Vector3 newCenter,
            TransformExportType exportType = TransformExportType.Local,
            ExportSettings.LODExportType lodExportType = ExportSettings.LODExportType.All
        )
        {
            int numObjectsExported = exportProgress;

            FbxNode fbxNode = CreateFbxNode(unityGo, fbxScene);

            if (Verbose)
                Debug.Log (string.Format ("exporting {0}", fbxNode.GetName ()));

            numObjectsExported++;
            if (EditorUtility.DisplayCancelableProgressBar (
                    ProgressBarTitle,
                    string.Format ("Creating FbxNode {0}/{1}", numObjectsExported, objectCount),
                    (numObjectsExported / (float)objectCount) * 0.25f)) {
                // cancel silently
                return -1;
            }

            ExportTransform (unityGo.transform, fbxNode, newCenter, exportType);

            fbxNodeParent.AddChild (fbxNode);

            // if this object has an LOD group, then export according to the LOD preference setting
            var lodGroup = unityGo.GetComponent<LODGroup>();
            if (lodGroup && lodExportType != ExportSettings.LODExportType.All) {
                LOD[] lods = lodGroup.GetLODs ();

                // LODs are ordered from highest to lowest.
                // If exporting lowest LOD, reverse the array
                if (lodExportType == ExportSettings.LODExportType.Lowest) {
                    // reverse the array
                    LOD[] tempLods = new LOD[lods.Length];
                    System.Array.Copy (lods, tempLods, lods.Length);
                    System.Array.Reverse (tempLods);
                    lods = tempLods;
                }

                for(int i = 0; i < lods.Length; i++){
                    var lod = lods [i];
                    bool exportedRenderer = false;
                    foreach (var renderer in lod.renderers) {
                        // only export if parented under LOD group
                        if (renderer.transform.parent == unityGo.transform) {
                            numObjectsExported = ExportTransformHierarchy (renderer.gameObject, fbxScene, fbxNode, numObjectsExported, objectCount, newCenter, lodExportType: lodExportType);
                            exportedRenderer = true;
                        } else if(Verbose) {
                            Debug.LogFormat ("FbxExporter: Not exporting LOD {0}: {1}", i, renderer.name);
                        }
                    }

                    // if at least one renderer for this LOD was exported, then we succeeded
                    // so stop exporting.
                    if (exportedRenderer) {
                        return numObjectsExported;
                    }
                }
            }

            // now  unityGo  through our children and recurse
            foreach (Transform childT in  unityGo.transform) {
                numObjectsExported = ExportTransformHierarchy (childT.gameObject, fbxScene, fbxNode, numObjectsExported, objectCount, newCenter, lodExportType: lodExportType);
            }

            return numObjectsExported;
        }

        /// <summary>
        /// Exports all animation clips in the hierarchy along with
        /// the minimum required GameObject information.
        /// i.e. Animated GameObjects, their ancestors, and their transforms are exported, 
        ///     but components are only exported if explicitly animated. Meshes are not exported.
        /// </summary>
        /// <returns>The number of nodes exported.</returns>
        [SecurityPermission(SecurityAction.LinkDemand)]
        internal int ExportAnimationOnly(
            GameObject unityGO,
            FbxScene fbxScene,
            int exportProgress,
            int objectCount,
            Vector3 newCenter,
            IExportData data,
            TransformExportType exportType = TransformExportType.Local
        ){
            AnimationOnlyExportData exportData = (AnimationOnlyExportData)data;
            int numObjectsExported = exportProgress;

            // make sure anim destination node is exported as well
            var exportSet = exportData.Objects;
            if (ExportOptions.AnimationDest && ExportOptions.AnimationSource)
            {
                exportSet.Add(ExportOptions.AnimationDest.gameObject);
            }

            // first export all the animated bones that are in the export set
            // as only a subset of bones are exported, but we still need to make sure the bone transforms are correct
            if(!ExportAnimatedBones(unityGO, fbxScene, ref numObjectsExported, objectCount, exportData))
            {
                // export cancelled
                return -1;
            }

            // export everything else and make sure all nodes are connected
            foreach (var go in exportSet) {
                FbxNode node;
                if (!ExportGameObjectAndParents (
                    go, unityGO, fbxScene, out node, newCenter, exportType, ref numObjectsExported, objectCount
                    )) {
                    // export cancelled
                    return -1;
                }

                ExportConstraints(go, fbxScene, node);

                System.Type compType;
                if (exportData.exportComponent.TryGetValue (go, out compType)) {
                    if (compType == typeof(Light)) {
                        ExportLight (go, fbxScene, node);
                    } else if (compType == typeof(Camera)) {
                        ExportCamera (go, fbxScene, node);
                    }
                }
            }

            return numObjectsExported;
        }

        internal class SkinnedMeshBoneInfo {
            public SkinnedMeshRenderer skinnedMesh;
            public Dictionary<Transform, int> boneDict;
            public Dictionary<Transform, Matrix4x4> boneToBindPose;

            public SkinnedMeshBoneInfo(SkinnedMeshRenderer skinnedMesh, Dictionary<Transform, int> boneDict){
                this.skinnedMesh = skinnedMesh;
                this.boneDict = boneDict;
                this.boneToBindPose = new Dictionary<Transform, Matrix4x4>();
            }
        }

        private bool ExportAnimatedBones (
            GameObject unityGo,
            FbxScene fbxScene,
            ref int exportProgress,
            int objectCount,
            AnimationOnlyExportData exportData
            )
        {
            var skinnedMeshRenderers = unityGo.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skinnedMesh in skinnedMeshRenderers)
            {
                var boneArray = skinnedMesh.bones;
                var bones = new HashSet<GameObject>();
                var boneDict = new Dictionary<Transform, int>();

                for (int i = 0; i < boneArray.Length; i++)
                {
                    bones.Add(boneArray[i].gameObject);
                    boneDict.Add(boneArray[i], i);
                }

                // get the bones that are also in the export set
                bones.IntersectWith(exportData.Objects);

                var boneInfo = new SkinnedMeshBoneInfo(skinnedMesh, boneDict);
                foreach (var bone in bones)
                {
                    FbxNode fbxNode;
                    // bone already exported
                    if (MapUnityObjectToFbxNode.TryGetValue(bone, out fbxNode))
                    {
                        continue;
                    }
                    fbxNode = CreateFbxNode(bone, fbxScene);

                    exportProgress++;
                    if (EditorUtility.DisplayCancelableProgressBar(
                            ProgressBarTitle,
                        string.Format("Creating FbxNode {0}/{1}", exportProgress, objectCount),
                        (exportProgress / (float)objectCount) * 0.5f))
                    {
                        // cancel silently
                        return false;
                    }
                    ExportBoneTransform(fbxNode, fbxScene, bone.transform, boneInfo);
                }
            }
            return true;
        }

        /// <summary>
        /// Exports the Gameobject and its ancestors.
        /// </summary>
        /// <returns><c>true</c>, if game object and parents were exported,
        ///  <c>false</c> if export cancelled.</returns>
        private bool ExportGameObjectAndParents(
            GameObject unityGo,
            GameObject rootObject,
            FbxScene fbxScene, 
            out FbxNode fbxNode,
            Vector3 newCenter,
            TransformExportType exportType,
            ref int exportProgress,
            int objectCount
            )
        {
            // node doesn't exist so create it
            if (!MapUnityObjectToFbxNode.TryGetValue(unityGo, out fbxNode))
            {
                fbxNode = CreateFbxNode(unityGo, fbxScene);

                exportProgress++;
                if (EditorUtility.DisplayCancelableProgressBar(
                        ProgressBarTitle,
                    string.Format("Creating FbxNode {0}/{1}", exportProgress, objectCount),
                    (exportProgress / (float)objectCount) * 0.5f))
                {
                    // cancel silently
                    return false;
                }

                ExportTransform(unityGo.transform, fbxNode, newCenter, exportType);
            }

            if (unityGo == rootObject || unityGo.transform.parent == null)
            {
                fbxScene.GetRootNode().AddChild(fbxNode);
                return true;
            }

            // make sure all the nodes are connected and exported
            FbxNode fbxNodeParent;
            if (!ExportGameObjectAndParents (
                unityGo.transform.parent.gameObject,
                rootObject,
                fbxScene,
                out fbxNodeParent,
                newCenter,
                TransformExportType.Local,
                ref exportProgress,
                objectCount
            )) {
                // export cancelled
                return false;
            }
            fbxNodeParent.AddChild (fbxNode);

            return true;
        }

        /// <summary>
        /// Exports the bone transform.
        /// </summary>
        /// <returns><c>true</c>, if bone transform was exported, <c>false</c> otherwise.</returns>
        /// <param name="fbxNode">Fbx node.</param>
        /// <param name="fbxScene">Fbx scene.</param>
        /// <param name="unityBone">Unity bone.</param>
        /// <param name="boneInfo">Bone info.</param>
        private bool ExportBoneTransform(
            FbxNode fbxNode, FbxScene fbxScene, Transform unityBone, SkinnedMeshBoneInfo boneInfo
        ){
            if (boneInfo == null || boneInfo.skinnedMesh == null || boneInfo.boneDict == null || unityBone == null) {
                return false;
            }

            var skinnedMesh = boneInfo.skinnedMesh;
            var boneDict = boneInfo.boneDict;
            var rootBone = skinnedMesh.rootBone;

            // setup the skeleton
            var fbxSkeleton = fbxNode.GetSkeleton ();
            if (fbxSkeleton == null) {
                fbxSkeleton = FbxSkeleton.Create (fbxScene, unityBone.name + SkeletonPrefix);

                fbxSkeleton.Size.Set (1.0f * UnitScaleFactor);
                fbxNode.SetNodeAttribute (fbxSkeleton);
            }
            var fbxSkeletonType = FbxSkeleton.EType.eLimbNode;

            // Only set the rootbone's skeleton type to FbxSkeleton.EType.eRoot
            // if it has at least one child that is also a bone.
            // Otherwise if it is marked as Root but has no bones underneath,
            // Maya will import it as a Null object instead of a bone.
            if (rootBone == unityBone && rootBone.childCount > 0)
            {
                var hasChildBone = false;
                foreach (Transform child in unityBone)
                {
                    if (boneDict.ContainsKey(child))
                    {
                        hasChildBone = true;
                        break;
                    }
                }
                if (hasChildBone)
                {
                    fbxSkeletonType = FbxSkeleton.EType.eRoot;
                }
            }
            fbxSkeleton.SetSkeletonType (fbxSkeletonType);

            var bindPoses = skinnedMesh.sharedMesh.bindposes;

            // get bind pose
            Matrix4x4 bindPose;
            if (!boneInfo.boneToBindPose.TryGetValue (unityBone, out bindPose)) {
                bindPose = GetBindPose (unityBone, bindPoses, boneDict, skinnedMesh);
                boneInfo.boneToBindPose.Add (unityBone, bindPose);
            }

            Matrix4x4 pose;
            // get parent's bind pose
            Matrix4x4 parentBindPose;
            if (!boneInfo.boneToBindPose.TryGetValue (unityBone.parent, out parentBindPose)) {
                parentBindPose = GetBindPose (unityBone.parent, bindPoses, boneDict, skinnedMesh);
                boneInfo.boneToBindPose.Add (unityBone.parent, parentBindPose);
            }
            pose = parentBindPose * bindPose.inverse;

            FbxVector4 translation, rotation, scale;
            GetTRSFromMatrix (pose, out translation, out rotation, out scale);

            // Export bones with zero rotation, using a pivot instead to set the rotation
            // so that the bones are easier to animate and the rotation shows up as the "joint orientation" in Maya.
            fbxNode.LclTranslation.Set (new FbxDouble3(translation.X*UnitScaleFactor, translation.Y*UnitScaleFactor, translation.Z*UnitScaleFactor));
            fbxNode.LclRotation.Set (new FbxDouble3(0,0,0));
            fbxNode.LclScaling.Set (new FbxDouble3 (scale.X, scale.Y, scale.Z));

            // TODO (UNI-34294): add detailed comment about why we export rotation as pre-rotation
            fbxNode.SetRotationActive (true);
            fbxNode.SetPivotState (FbxNode.EPivotSet.eSourcePivot, FbxNode.EPivotState.ePivotReference);
            fbxNode.SetPreRotation (FbxNode.EPivotSet.eSourcePivot, new FbxVector4 (rotation.X, rotation.Y, rotation.Z));

            return true;
        }

        private void GetTRSFromMatrix(Matrix4x4 unityMatrix, out FbxVector4 translation, out FbxVector4 rotation, out FbxVector4 scale){
            // FBX is transposed relative to Unity: transpose as we convert.
            FbxMatrix matrix = new FbxMatrix ();
            matrix.SetColumn (0, new FbxVector4 (unityMatrix.GetRow (0).x, unityMatrix.GetRow (0).y, unityMatrix.GetRow (0).z, unityMatrix.GetRow (0).w));
            matrix.SetColumn (1, new FbxVector4 (unityMatrix.GetRow (1).x, unityMatrix.GetRow (1).y, unityMatrix.GetRow (1).z, unityMatrix.GetRow (1).w));
            matrix.SetColumn (2, new FbxVector4 (unityMatrix.GetRow (2).x, unityMatrix.GetRow (2).y, unityMatrix.GetRow (2).z, unityMatrix.GetRow (2).w));
            matrix.SetColumn (3, new FbxVector4 (unityMatrix.GetRow (3).x, unityMatrix.GetRow (3).y, unityMatrix.GetRow (3).z, unityMatrix.GetRow (3).w));

            // FBX wants translation, rotation (in euler angles) and scale.
            // We assume there's no real shear, just rounding error.
            FbxVector4 shear;
            double sign;
            matrix.GetElements (out translation, out rotation, out shear, out scale, out sign);
        }

        /// <summary>
        /// Counts how many objects are between this object and the root (exclusive).
        /// </summary>
        /// <returns>The object to root count.</returns>
        /// <param name="startObject">Start object.</param>
        /// <param name="root">Root object.</param>
        private static int GetObjectToRootDepth(Transform startObject, Transform root){
            if (startObject == null) {
                return 0;
            }

            int count = 0;
            var parent = startObject.parent;
            while (parent != null && parent != root) {
                count++;
                parent = parent.parent;
            }
            return count;
        }


        /// <summary>
        /// Gets the count of animated objects to be exported.
        /// 
        /// In addition, collects the minimum set of what needs to be exported for each GameObject hierarchy.
        /// This contains all the animated GameObjects, their ancestors, their transforms, as well as any animated
        /// components and the animation clips. Also, the first animation to export, if any.
        /// </summary>
        /// <returns>The animation only hierarchy count.</returns>
        /// <param name="exportSet">GameObject hierarchies selected for export.</param>
        /// <param name="hierarchyToExportData">Map from GameObject hierarchy to animation export data.</param>
        [SecurityPermission(SecurityAction.LinkDemand)]
        internal int GetAnimOnlyHierarchyCount(Dictionary<GameObject, IExportData> hierarchyToExportData)
        {
            // including any parents of animated objects that are exported
            var completeExpSet = new HashSet<GameObject>();
            foreach (var data in hierarchyToExportData.Values) {
                foreach (var go in data.Objects) {
                    completeExpSet.Add(go);

                    var parent = go.transform.parent;
                    while (parent != null && completeExpSet.Add(parent.gameObject)) {
                        parent = parent.parent;
                    }
                }
            }

            return completeExpSet.Count;
        }

        [SecurityPermission(SecurityAction.LinkDemand)]
        internal static Dictionary<GameObject, IExportData> GetExportData(Object[] objects, IExportOptions exportOptions = null)
        {
            if (exportOptions==null)
                exportOptions = DefaultOptions;
            Debug.Assert(exportOptions!=null);

            Dictionary<GameObject, IExportData>  exportData = new Dictionary<GameObject, IExportData>();

            if (exportOptions.ModelAnimIncludeOption == ExportSettings.Include.Model)
            {
                return null;
            }

            foreach (var obj in objects) 
            {
                GameObject go = ModelExporter.GetGameObject (obj);
                if (go)
                {
                    exportData[go] = GetExportData(go, exportOptions);
                }
                else if (IsEditorClip(obj)) 
                {
                    KeyValuePair<GameObject, AnimationClip> pair = AnimationOnlyExportData.GetGameObjectAndAnimationClip(obj);
                    exportData[pair.Key] = GetExportData (pair.Key, pair.Value, exportOptions);
                }
            }

            return exportData.Count == 0 ? null : exportData;
        }

        [SecurityPermission(SecurityAction.LinkDemand)]
        internal static IExportData GetExportData(GameObject rootObject, AnimationClip animationClip, IExportOptions exportOptions = null)
        {
            if (exportOptions==null)
                exportOptions = DefaultOptions;
            Debug.Assert(exportOptions!=null);
                
            var exportData = new AnimationOnlyExportData();
            exportData.CollectDependencies(animationClip, rootObject, exportOptions);
                
            // could not find any dependencies, return null
            if(exportData.Objects.Count <= 0)
            {
                return null;
            }
            return exportData;
        }

        internal static IExportData GetExportData(GameObject go, IExportOptions exportOptions = null)
        {
            if (exportOptions==null)
                exportOptions = DefaultOptions;
            Debug.Assert(exportOptions!=null);

            // gather all animation clips
            var legacyAnim = go.GetComponentsInChildren<Animation>();
            var genericAnim = go.GetComponentsInChildren<Animator>();

            var exportData = new AnimationOnlyExportData();

            int depthFromRootAnimation = int.MaxValue;
            Animation rootAnimation = null;
            foreach (var anim in legacyAnim)
            {
                int count = GetObjectToRootDepth(anim.transform, go.transform);

                if (count < depthFromRootAnimation)
                {
                    depthFromRootAnimation = count;
                    rootAnimation = anim;
                }

                var animClips = AnimationUtility.GetAnimationClips(anim.gameObject);
                exportData.CollectDependencies(animClips, anim.gameObject, exportOptions);
            }

            int depthFromRootAnimator = int.MaxValue;
            Animator rootAnimator = null;
            foreach (var anim in genericAnim)
            {
                int count = GetObjectToRootDepth(anim.transform, go.transform);

                if (count < depthFromRootAnimator)
                {
                    depthFromRootAnimator = count;
                    rootAnimator = anim;
                }

                // Try the animator controller (mecanim)
                var controller = anim.runtimeAnimatorController;
                if (controller)
                {
                    exportData.CollectDependencies(controller.animationClips, anim.gameObject, exportOptions);
                }
            }

            // set the first clip to export
            if (depthFromRootAnimation < depthFromRootAnimator)
            {
                exportData.defaultClip = rootAnimation.clip;
            }
            else if(rootAnimator)
            {
                // Try the animator controller (mecanim)
                var controller = rootAnimator.runtimeAnimatorController;
                if (controller)
                {
                    var dController = controller as UnityEditor.Animations.AnimatorController;
                    if (dController && dController.layers.Count() > 0)
                    {
                        var motion = dController.layers[0].stateMachine.defaultState.motion;
                        var defaultClip = motion as AnimationClip;
                        if (defaultClip)
                        {
                            exportData.defaultClip = defaultClip;
                        }
                        else
                        {
                            Debug.LogWarningFormat("Couldn't export motion {0}", motion.name);
                        }
                    }
                }
            }
            return exportData;
        }

        /// <summary>
        /// Export components on this game object.
        /// Transform components have already been exported.
        /// This function exports the other components and animation.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        private bool ExportComponents(FbxScene fbxScene)
        {
            var animationNodes = new HashSet<GameObject> ();

            int numObjectsExported = 0;
            int objectCount = MapUnityObjectToFbxNode.Count;
            foreach (KeyValuePair<GameObject, FbxNode> entry in MapUnityObjectToFbxNode) {
                numObjectsExported++;
                if (EditorUtility.DisplayCancelableProgressBar (
                        ProgressBarTitle,
                        string.Format ("Exporting Components for GameObject {0}/{1}", numObjectsExported, objectCount),
                        ((numObjectsExported / (float)objectCount) * 0.25f) + 0.25f)) {
                    // cancel silently
                    return false;
                }

                var unityGo = entry.Key;
                var fbxNode = entry.Value;

                // try export mesh
                bool exportedMesh = ExportInstance (unityGo, fbxScene, fbxNode);

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

                ExportConstraints(unityGo, fbxScene, fbxNode);
            }
            return true;
        }

        /// <summary>
        /// Checks if the GameObject has animation.
        /// </summary>
        /// <returns><c>true</c>, if object has animation, <c>false</c> otherwise.</returns>
        /// <param name="go">Go.</param>
        private bool GameObjectHasAnimation(GameObject go){
            return go != null &&
                (go.GetComponent<Animator> () ||
                go.GetComponent<Animation> () ||
                go.GetComponent<UnityEngine.Playables.PlayableDirector> ());
        }

        /// <summary>
        /// A count of how many GameObjects we are exporting, to have a rough
        /// idea of how long creating the scene will take.
        /// </summary>
        /// <returns>The hierarchy count.</returns>
        /// <param name="exportSet">Export set.</param>
        internal int GetHierarchyCount (HashSet<GameObject> exportSet)
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
        [SecurityPermission(SecurityAction.LinkDemand)]
        internal static HashSet<GameObject> RemoveRedundantObjects(IEnumerable<UnityEngine.Object> unityExportSet)
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
        internal static Vector3 FindCenter(IEnumerable<GameObject> gameObjects)
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
        internal static Vector3 GetRecenteredTranslation(Transform t, Vector3 center)
        {
            return t.position - center;
        }

        internal enum TransformExportType { Local, Global, Reset };

        /// <summary>
        /// Export all the objects in the set.
        /// Return the number of objects in the set that we exported.
        ///
        /// This refreshes the asset database.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        internal int ExportAll (
            IEnumerable<UnityEngine.Object> unityExportSet, 
            Dictionary<GameObject, IExportData> exportData)
        {
            exportCancelled = false;
            
            m_lastFilePath = LastFilePath;

            // Export first to a temporary file
            // in case the export is cancelled.
            // This way we won't overwrite existing files.
            try
            {
                // create a temp file in the same directory where the fbx will be exported
                var exportDir = Path.GetDirectoryName(m_lastFilePath);
                var lastFileName = Path.GetFileName(m_lastFilePath);
                var tempFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + "_" + lastFileName;
                m_tempFilePath = Path.Combine(new string[] { exportDir, tempFileName });
            }
            catch(IOException){
                return 0;
            }

            if (string.IsNullOrEmpty (m_tempFilePath)) {
                return 0;
            }

            try {
                bool animOnly = exportData != null && ExportOptions.ModelAnimIncludeOption == ExportSettings.Include.Anim;
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
                    if (ExportOptions.ExportFormat == ExportSettings.ExportFormat.ASCII)
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
                    // DirectX has the same axis system, so use this constant.
                    var unityAxisSystem = FbxAxisSystem.DirectX;
                    fbxSettings.SetAxisSystem (unityAxisSystem);

                    // export set of object
                    FbxNode fbxRootNode = fbxScene.GetRootNode ();
                    // stores how many objects we have exported, -1 if export was cancelled
                    int exportProgress = 0;
                    IEnumerable<GameObject> revisedExportSet = null;

                    // Total # of objects to be exported
                    // Used by progress bar to show how many objects will be exported in total
                    // i.e. exporting x/count... 
                    int count = 0;

                    // number of object hierarchies being exported.
                    // Used to figure out exported transforms for root objects.
                    // i.e. if we are exporting a single hierarchy at local position, then it's root is set to zero,
                    // but if we are exporting multiple hierarchies at local position, then each hierarchy will be recentered according
                    // to the center of the bounding box.
                    int rootObjCount = 0; 

                    if(animOnly){
                        count = GetAnimOnlyHierarchyCount(exportData);
                        revisedExportSet = from entry in exportData select entry.Key;
                        rootObjCount = exportData.Keys.Count;
                    } else {
                        var revisedGOSet = RemoveRedundantObjects(unityExportSet);
                        count = GetHierarchyCount (revisedGOSet);
                        rootObjCount = revisedGOSet.Count;
                        revisedExportSet = revisedGOSet;
                    }

                    if(count <= 0){
                        // nothing to export
                        Debug.LogWarning("Nothing to Export");
                        return 0;
                    }

                    Vector3 center = Vector3.zero;
                    TransformExportType transformExportType = TransformExportType.Global;
                    switch(ExportOptions.ObjectPosition){
                    case ExportSettings.ObjectPosition.LocalCentered:
                        // one object to export -> move to (0,0,0)
                        if(rootObjCount == 1){
                            var tempList = new List<GameObject>(revisedExportSet);
                            center = tempList[0].transform.position;
                            break;
                        }
                        // more than one object to export -> get bounding center
                        center = FindCenter(revisedExportSet);
                        break;
                    case ExportSettings.ObjectPosition.Reset:
                        transformExportType = TransformExportType.Reset;
                        break;
                    // absolute center -> don't do anything
                    default:
                        center = Vector3.zero;
                        break;
                    }

                    foreach (var unityGo in revisedExportSet) {
                        IExportData data;
                        if(animOnly && exportData.TryGetValue(unityGo, out data)){
                            exportProgress = this.ExportAnimationOnly(unityGo, fbxScene, exportProgress, count, center, data, transformExportType);
                        }
                        else {
                            exportProgress = this.ExportTransformHierarchy (unityGo, fbxScene, fbxRootNode,
                                exportProgress, count, center, transformExportType, ExportOptions.LODExportType);
                        }
                        if (exportCancelled || exportProgress < 0) {
                            Debug.LogWarning ("Export Cancelled");
                            return 0;
                        }
                    }

                    if(!animOnly){
                        if(!ExportComponents(fbxScene)){
                            Debug.LogWarning ("Export Cancelled");
                            return 0;
                        }
                    }

                    // Export animation if any
                    if (exportData != null)
                    {
                        foreach (var unityGo in revisedExportSet)
                        {
                            IExportData iData;
                            if (!exportData.TryGetValue(unityGo, out iData))
                            {
                                continue;
                            }
                            var data = iData as AnimationOnlyExportData;
                            if (data == null)
                            {
                                Debug.LogWarningFormat("FBX Exporter: no animation export data found for {0}", unityGo.name);
                                continue;
                            }
                            // export animation
                            // export default clip first
                            if (data.defaultClip != null)
                            {
                                var defaultClip = data.defaultClip;
                                ExportAnimationClip(defaultClip, data.animationClips[defaultClip], fbxScene);
                                data.animationClips.Remove(defaultClip);
                            }

                            foreach (var animClip in data.animationClips)
                            {
                                ExportAnimationClip(animClip.Key, animClip.Value, fbxScene);
                            }
                        }
                    }
                    // Set the scene's default camera.
                    SetDefaultCamera (fbxScene);

                    // The Maya axis system has Y up, Z forward, X to the left (right handed system with odd parity).
                    // We need to export right-handed for Maya because ConvertScene (used by Maya and Max importers) can't switch handedness:
                    // https://forums.autodesk.com/t5/fbx-forum/get-confused-with-fbxaxissystem-convertscene/td-p/4265472
                    // This needs to be done last so that everything is converted properly.
                    FbxAxisSystem.MayaYUp.DeepConvertScene(fbxScene);

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

                // make a temporary copy of the original metafile
                string originalMetafilePath = "";
                if (ExportOptions.PreserveImportSettings && File.Exists(m_lastFilePath))
                {
                    originalMetafilePath = SaveMetafile();
                }

                // delete old file, move temp file
                ReplaceFile();
                AssetDatabase.Refresh();
                
                // replace with original metafile if specified to
                if (ExportOptions.PreserveImportSettings && !string.IsNullOrEmpty(originalMetafilePath))
                {
                    ReplaceMetafile(originalMetafilePath);
                }

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

        private string SaveMetafile()
        {
            var tempMetafilePath = Path.GetTempFileName();
            
            // Try as an absolute path
            var fbxPath = m_lastFilePath;
            if (AssetDatabase.LoadAssetAtPath(fbxPath, typeof(Object)) == null)
            {
                // Try as a relative path
                fbxPath = "Assets" + m_lastFilePath.Substring(Application.dataPath.Length);
                if (AssetDatabase.LoadAssetAtPath(fbxPath, typeof(Object)) == null)
                {
                    Debug.LogWarning(string.Format("Failed to find a valid asset at {0}. Import settings will be reset to default values.", m_lastFilePath));
                    return "";
                }
            }
            
            // get metafile for original fbx file
            var metafile = fbxPath + ".meta";

#if UNITY_2019_1_OR_NEWER
            metafile = VersionControl.Provider.GetAssetByPath(fbxPath).metaPath;
#endif

            // save it to a temp file
            try {
                File.Copy(metafile, tempMetafilePath, true);
            } catch(IOException) {
                Debug.LogWarning (string.Format("Failed to copy file {0} to {1}. Import settings will be reset to default values.", metafile, tempMetafilePath));
                return "";
            }

            return tempMetafilePath;
        }

        private void ReplaceMetafile(string metafilePath)
        {
            // Try as an absolute path
            var fbxPath = m_lastFilePath;
            if (AssetDatabase.LoadAssetAtPath(fbxPath, typeof(Object)) == null)
            {
                // Try as a relative path
                fbxPath = "Assets" + m_lastFilePath.Substring(Application.dataPath.Length);
                if (AssetDatabase.LoadAssetAtPath(fbxPath, typeof(Object)) == null)
                {
                    Debug.LogWarning(string.Format("Failed to find a valid asset at {0}. Import settings will be reset to default values.", m_lastFilePath));
                    return;
                }
            }
            
            // get metafile for new fbx file
            var metafile = fbxPath + ".meta";

#if UNITY_2019_1_OR_NEWER
            metafile = VersionControl.Provider.GetAssetByPath(fbxPath).metaPath;
#endif

            // replace metafile with original one in temp file
            try {
                File.Copy(metafilePath, metafile, true);
            } catch(IOException) {
                Debug.LogWarning (string.Format("Failed to copy file {0} to {1}. Import settings will be reset to default values.", metafilePath, m_lastFilePath));
            }
        }

        /// <summary>
        /// GameObject/Export Selected Timeline Clip...
        /// </summary>
        /// <param name="command"></param>
		[MenuItem(TimelineClipMenuItemName, false, 31)]
        static void OnClipContextClick(MenuCommand command)
        {
            Object[] selectedObjects = Selection.objects;

            foreach (Object editorClipSelected in selectedObjects)
            {
                // export first selected editor clip.
                if (IsEditorClip(editorClipSelected)) {
                    ExportSingleTimelineClip(editorClipSelected);
                    return;
                }
            }
        }

        /// <summary>
        /// Validate the menu item defined by the function OnClipContextClick.
        /// </summary>
        [MenuItem(TimelineClipMenuItemName, true, 31)]
        static bool ValidateOnClipContextClick()
        {
            Object[] selectedObjects = Selection.objects;

            foreach (Object editorClipSelected in selectedObjects)
            {
                if (IsEditorClip(editorClipSelected))
                {         
                    return true;
                }
            }
            return false;
        }

        internal static bool IsEditorClip(object obj)
        {
            if (obj == null)
                return false;

            return obj.GetType().Name.Contains("EditorClip");
        }

        internal static void ExportSingleTimelineClip(Object editorClipSelected)
        {
            UnityEngine.Object[] exportArray = new UnityEngine.Object[] { editorClipSelected };
            string filename = AnimationOnlyExportData.GetFileName(editorClipSelected);
            if (ExportSettings.DisplayOptionsWindow)
            {
                ExportModelEditorWindow.Init(exportArray, filename, isTimelineAnim: true);
                return;
            }

            var folderPath = ExportSettings.FbxAbsoluteSavePath;
            var filePath = System.IO.Path.Combine(folderPath, filename + ".fbx");

            if (System.IO.File.Exists(filePath))
            {
                Debug.LogErrorFormat("{0}: Failed to export to {1}, file already exists", PACKAGE_UI_NAME, filePath);
                return;
            }

            var previousInclude = ExportSettings.instance.ExportModelSettings.info.ModelAnimIncludeOption;
            ExportSettings.instance.ExportModelSettings.info.SetModelAnimIncludeOption(ExportSettings.Include.Anim);

            if (ExportObjects(filePath, exportArray, ExportSettings.instance.ExportModelSettings.info) != null)
            {
                // refresh the asset database so that the file appears in the
                // asset folder view.
                AssetDatabase.Refresh();
            }

            ExportSettings.instance.ExportModelSettings.info.SetModelAnimIncludeOption(previousInclude);
        }

        /// <summary>
        /// Add a menu item "Export Model..." to a GameObject's context menu.
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
        /// Validate the menu item defined by the function OnContextItem.
        /// </summary>
        [MenuItem (MenuItemName, true, 30)]
        internal static bool OnValidateMenuItem ()
        {
            return true;
        }

        internal static void DisplayNoSelectionDialog()
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
        internal class MeshInfo
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
                }
            }

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
                }
            }

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
                }
            }

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
                }
            }

            /// <summary>
            /// The material(s) used.
            /// Always at least one.
            /// None are missing materials (we replace missing materials with the default material).
            /// </summary>
            public Material[] Materials { get ; private set; }

            private BoneWeight[] m_boneWeights;
            public BoneWeight[] BoneWeights { get {
                    if (m_boneWeights == null) {
                        m_boneWeights = mesh.boneWeights;
                    }
                    return m_boneWeights; 
                }
            }

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
        internal static GameObject GetGameObject (Object obj)
        {
            if (obj is UnityEngine.Transform) {
                var xform = obj as UnityEngine.Transform;
                return xform.gameObject;
            }
            else if (obj is UnityEngine.SkinnedMeshRenderer)
            {
                var skinnedMeshRenderer = obj as UnityEngine.SkinnedMeshRenderer;
                return skinnedMeshRenderer.gameObject;
            }
            else if (obj is UnityEngine.GameObject)
            {
                return obj as UnityEngine.GameObject;
            } 
            else if (obj is Behaviour) 
            {
                var behaviour = obj as Behaviour;
                return behaviour.gameObject;
            }

            return null;
        }

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
        internal static void RegisterMeshCallback<T>(GetMeshForComponent<T> callback, bool replace = false)
            where T: UnityEngine.MonoBehaviour
        {
            // Under the hood we lose type safety, but don't let the user notice!
            RegisterMeshCallback (typeof(T),
                (ModelExporter exporter, MonoBehaviour component, FbxNode fbxNode) =>
                        callback (exporter, (T)component, fbxNode),
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
        internal static void RegisterMeshCallback(System.Type t,
                GetMeshForComponent callback,
                bool replace = false)
        {
            if (!t.IsSubclassOf(typeof(MonoBehaviour))) {
                throw new ModelExportException("Registering a callback for a type that isn't derived from MonoBehaviour: " + t);
            }
            if (!replace && MeshForComponentCallbacks.ContainsKey(t)) {
                throw new ModelExportException("Replacing a callback for type " + t);
            }
            MeshForComponentCallbacks[t] = callback;
        }

        /// <summary>
        /// Forget the callback linked to a component of type T.
        /// </summary>
        internal static void UnRegisterMeshCallback<T>()
        {
            MeshForComponentCallbacks.Remove(typeof(T));
        }

        /// <summary>
        /// Forget the callback linked to a component of type T.
        /// </summary>
        internal static void UnRegisterMeshCallback(System.Type t)
        {
            MeshForComponentCallbacks.Remove(t);
        }

        /// <summary>
        /// Forget the callbacks linked to components.
        /// </summary>
        internal static void UnRegisterAllMeshCallbacks()
        {
            MeshForComponentCallbacks.Clear();
        }
        
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
        internal static void RegisterMeshObjectCallback(GetMeshForObject callback)
        {
            MeshForObjectCallbacks.Add(callback);
        }

        /// <summary>
        /// Forget a GameObject-based callback.
        /// </summary>
        internal static void UnRegisterMeshObjectCallback(GetMeshForObject callback)
        {
            MeshForObjectCallbacks.Remove(callback);
        }

        /// <summary>
        /// Forget all GameObject-based callbacks.
        /// </summary>
        internal static void UnRegisterAllMeshObjectCallbacks()
        {
            MeshForObjectCallbacks.Clear();
        }

        /// <summary>
        /// Exports a mesh for a unity gameObject.
        ///
        /// This goes through the callback system to find the right mesh and
        /// allow plugins to substitute their own meshes.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
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

            // if user doesn't want to export mesh colliders, and this gameobject doesn't have a renderer
            // then don't export it.
            if (!ExportOptions.ExportUnrendered && (!gameObject.GetComponent<Renderer>() || !gameObject.GetComponent<Renderer>().enabled)) {
                return false;
            }

            var meshFilter = defaultComponent as MeshFilter;
            if (meshFilter) {
                var renderer = gameObject.GetComponent<Renderer>();
                var materials = renderer ? renderer.sharedMaterials : null;
                return ExportMesh(new MeshInfo(meshFilter.sharedMesh, materials), fbxNode);
            } else {
                var smr = defaultComponent as SkinnedMeshRenderer;
                if (smr) {
                    var result = ExportSkinnedMesh (gameObject, fbxNode.GetScene (), fbxNode);
                    if(!result){
                        // fall back to exporting as a static mesh
                        var mesh = new Mesh();
                        smr.BakeMesh(mesh);
                        var materials = smr.sharedMaterials;
                        result = ExportMesh(new MeshInfo(mesh, materials), fbxNode);
                        Object.DestroyImmediate(mesh);
                    }
                    return result;
                }
            }

            return false;
        }

        /// <summary>
        /// Number of nodes exported including siblings and decendents
        /// </summary>
        internal int NumNodes { get { return MapUnityObjectToFbxNode.Count; } }

        /// <summary>
        /// Number of meshes exported
        /// </summary>
        internal int NumMeshes { set; get; }

        /// <summary>
        /// Number of triangles exported
        /// </summary>
        internal int NumTriangles { set; get; }

        /// <summary>
        /// Cleans up this class on garbage collection
        /// </summary>
        public void Dispose ()
        {
            System.GC.SuppressFinalize(this);
        }

        internal bool Verbose { get { return ExportSettings.instance.VerboseProperty; } }

        /// <summary>
        /// manage the selection of a filename
        /// </summary>
        static string LastFilePath { get; set; }
        private string m_tempFilePath { get; set; }
        private string m_lastFilePath { get; set; }

        const string kFBXFileExtension = "fbx";
			
        private static string MakeFileName (string basename = "test", string extension = kFBXFileExtension)
        {
            return basename + "." + extension;
        }
                
        private static void OnExport ()
        {
            GameObject [] selectedGOs = Selection.GetFiltered<GameObject> (SelectionMode.TopLevel);

            var toExport = ModelExporter.RemoveRedundantObjects(selectedGOs);
            if (ExportSettings.instance.DisplayOptionsWindow)
            {
                ExportModelEditorWindow.Init(System.Linq.Enumerable.Cast<UnityEngine.Object>(toExport), isTimelineAnim: false);
                return;
            }

            var filename = "";
            if (toExport.Count == 1)
            {
                filename = toExport.ToArray()[0].name;
            }
            else
            {
                filename = "Untitled";
            }

            var folderPath = ExportSettings.FbxAbsoluteSavePath;
            var filePath = System.IO.Path.Combine(folderPath, filename + ".fbx");

            if (System.IO.File.Exists(filePath))
            {
                Debug.LogErrorFormat("{0}: Failed to export to {1}, file already exists", PACKAGE_UI_NAME, filePath);
                return;
            }

            if (ExportObjects(filePath, toExport.ToArray(), ExportSettings.instance.ExportModelSettings.info) != null)
            {
                // refresh the asset database so that the file appears in the
                // asset folder view.
                AssetDatabase.Refresh();
            }
        }

        [SecurityPermission(SecurityAction.LinkDemand)]
        internal static string ExportObject (
            string filePath, 
            UnityEngine.Object root,
            IExportOptions exportOptions = null
        )
        {
            return ExportObjects(filePath, new Object[] { root }, exportOptions);
        }

        /// <summary>
        /// Exports an array of Unity GameObjects to an FBX file.
        /// </summary>
        /// <returns>
        /// The FBX file path if successful; otherwise returns null.
        /// </returns>
        /// <param name="filePath">Absolute file path to use for the FBX file.</param>
        /// <param name="objects">Array of Unity GameObjects to export.</param>
        [SecurityPermission(SecurityAction.LinkDemand)]
        public static string ExportObjects(string filePath, UnityEngine.Object[] objects = null)
        {
            return ExportObjects(filePath, objects, exportOptions: null, exportData: null);
        }

        /// <summary>
        /// Exports a single Unity GameObject to an FBX file.
        /// </summary>
        /// <returns>
        /// The FBX file path if successful; otherwise null.
        /// </returns>
        /// <param name="filePath">Absolute file path to use for the FBX file.</param>
        /// <param name="singleObject">The Unity GameObject to export.</param>
        [SecurityPermission(SecurityAction.LinkDemand)]
        public static string ExportObject (string filePath, UnityEngine.Object singleObject)
        {
            return ExportObjects(filePath, new Object[] {singleObject}, exportOptions: null);
        }

        /// <summary>
        /// Exports a list of GameObjects to an FBX file. 
        /// <para>
        /// Use the SaveFile panel to allow the user to enter a file name.
        /// </para>
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        internal static string ExportObjects (
            string filePath,
            UnityEngine.Object[] objects = null,
            IExportOptions exportOptions = null,
            Dictionary<GameObject, IExportData> exportData = null
        )
        {
            LastFilePath = filePath;

            using (var fbxExporter = Create ()) {
                // ensure output directory exists
                EnsureDirectory (filePath);
                fbxExporter.ExportOptions = exportOptions;

                if (objects == null) {
                    objects = Selection.objects;
                }

                if (exportData==null)
                    exportData = ModelExporter.GetExportData (objects, exportOptions);

                if (fbxExporter.ExportAll (objects, exportData) > 0) {
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

        internal static string ConvertToValidFilename(string filename)
        {
            return System.Text.RegularExpressions.Regex.Replace (filename, 
                RegexCharStart + new string(Path.GetInvalidFileNameChars()) + RegexCharEnd,
                InvalidCharReplacement.ToString()
            );
        }
    }
}
