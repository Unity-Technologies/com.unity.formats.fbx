import bpy

class FbxExportFunction(bpy.types.Operator):
    """FBX Export Function"""
    bl_idname = "object.exporter"
    bl_label = "This specifically exports the file"

    def execute(self, context):
        context.active_object.location.x += 1.0
        return {'FINISHED'}

def add_export_button(self, context):
    self.layout.operator(
        FbxExportFunction.bl_idname,
        text="Export to Unity",
        icon='PLUGIN'
    )

bpy.types.INFO_MT_file_export.append(add_export_button)

def register():
    bpy.utils.register_class(FbxExportFunction)

if __name__ == "__main__":
    register()