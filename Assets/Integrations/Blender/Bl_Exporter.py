import bpy
from Bl_ExportFunction import FbxExportFunction
from Bl_ImportFunction import FbxImportFunction
import Bl_ExportFunction
import Bl_ImportFunction

class FbxExporter(bpy.types.Operator):
    """FBX Exporter"""
    bl_idname = "object.exporty"
    bl_label = "export the file, yo"

Bl_ExportFunction.register()
Bl_ImportFunction.register()

def register():
    bpy.utils.register_class(FbxExporter)

if __name__ == "__main__":
    register()