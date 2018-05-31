# About the FBX SDK C# Bindings package 

__Version__: 1.6.0-preview

The FBX SDK C# Bindings provide access from Unity C# scripts to a subset of the Autodesk FBX SDK, version 2018.1.

The Autodesk® FBX® SDK is a free, easy-to-use, C++ software development platform and API toolkit that allows application and content vendors to transfer existing content into the FBX format with minimal effort.

The FBX SDK C# Bindings support the FBX Exporters package. The subset or the API that is exposed is geared towards exporting; import is not guaranteed to be possible yet.

## Requirements

The FBX SDK C# Bindings package is compatible with the following versions of the Unity Editor:

* 2018.2 and later

## Contents

The FBX Exporter package contains:

* C# bindings
* Compiled binaries for MacOS and Windows that include the FBX SDK

## Known Issues

* In this version, you cannot downcast the C# objects, which limits the use of the bindings for an importer. For example, if the FBX SDK declares in C++ that it will return an FbxDeformer, on the C++ side if you happen to know it is in fact an FbxSkinDeformer you could safely cast the deformer to a skin deformer. However, on the C# side, this is not permitted.
* While there are guards against some common errors, it is possible to crash Unity by writing C# code that directs the FBX SDK to perform invalid operations. For example, if you have an FbxProperty in C# and you delete the FbxNode that contains the property, if you try to use the FbxProperty, that will have undefined behaviour which may include crashing the Unity Editor. Make sure to read the editor log if you have unexplained crashes when writing FBX SDK C# code.

## API Documentation

There is no API documentation in the preview package. Refer to the <a href="http://help.autodesk.com/view/FBX/2018/ENU/">Autodesk FBX SDK API documentation</a>.

The bindings are in the `Unity.FbxSdk` namespace:

```
using UnityEngine.Formats.FbxSdk;
using UnityEngine;

public class HelloFbx {
    [MenuItem("Fbx/Hello")]
    public static void Hello() {
      using(var manager = FbxManager.Create()) {
        Debug.LogFormat("FBX SDK is version {0}, FbxManager.GetVersion());
      }
    }
}
```
