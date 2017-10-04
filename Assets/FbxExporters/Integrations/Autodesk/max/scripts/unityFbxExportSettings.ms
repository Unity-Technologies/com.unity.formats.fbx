FbxExporterSetParam "ResetExport"

-- FBX file format
FbxExporterSetParam "ASCII" false
FbxExporterSetParam "FileVersion" "FBX201600"

-- Geometry
FbxExporterSetParam "SmoothMeshExport" false
FbxExporterSetParam "Preserveinstances" true

-- Animation
FbxExporterSetParam "Animation" false

FbxExporterSetParam "Cameras" true
FbxExporterSetParam "Lights" true

FbxExporterSetParam "EmbedTextures" false

-- Units
--FbxExporterSetParam "ScaleFactor" 1
FbxExporterSetParam "ConvertUnit" "cm"

-- Axis Conversion
FbxExporterSetParam "UpAxis" "Y"