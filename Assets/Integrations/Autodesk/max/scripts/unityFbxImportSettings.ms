FbxImporterSetParam "ResetImport"

FbxImporterSetParam "Mode" #exmerge -- add and update animation

-- Geometry
FbxImporterSetParam "SmoothingGroups" true
FbxImporterSetParam "Shape" true
FbxImporterSetParam "Skin" true

-- Animation
FbxImporterSetParam "Animation" true
FbxImporterSetParam "ImportBoneAsDummy" false
FbxImporterSetParam "KeepFrameRate" true

FbxImporterSetParam "Cameras" true
FbxImporterSetParam "Lights" true

-- Axis Conversion
FbxImporterSetParam "UpAxis" "Y"