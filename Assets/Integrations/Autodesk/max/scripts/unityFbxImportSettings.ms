FbxImporterSetParam "ResetImport"

FbxImporterSetParam "Mode" #merge -- update animation

-- Geometry
FbxImporterSetParam "SmoothingGroups" true
FbxImporterSetParam "Shape" true
FbxImporterSetParam "Skin" true

-- Animation
FbxImporterSetParam "Animation" true
FbxImporterSetParam "ImportBoneAsDummy" true
FbxImporterSetParam "KeepFrameRate" true

FbxImporterSetParam "Cameras" true
FbxImporterSetParam "Lights" true

-- Axis Conversion
FbxImporterSetParam "UpAxis" "Y"