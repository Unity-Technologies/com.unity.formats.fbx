import bpy

class FbxImportFunction(bpy.types.Operator):
    """FBX Import Function"""
    bl_idname = "object.importer"
    bl_label = "This specifically imports the file"

    def execute(self, context):
        context.active_object.location.x -= 1.0
        return {'FINISHED'}

def add_import_button(self, context):
    self.layout.operator(
        FbxImportFunction.bl_idname,
        text="Import From Unity",
        icon='PLUGIN'
    )

bpy.types.INFO_MT_file_import.append(add_import_button)

def register():
    bpy.utils.register_class(FbxImportFunction)

if __name__ == "__main__":
    register()