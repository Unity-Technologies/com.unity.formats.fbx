# FbxExporters

Copyright (c) 2018 Unity Technologies. All rights reserved.

See LICENSE.md file in the project root for full license information.

Requirements
------------

* [FBX SDK C# Bindings](https://github.com/Unity-Technologies/FbxSharp) package

Packaging
---------

Get the source the first time:
```
# clone the source
git clone https://github.com/Unity-Technologies/FbxExporters.git
cd FbxExporters
```

Build the package.

**On Windows**
```
build.cmd
```

**On Mac/Linux**

```
./build.sh
```

The package will be built in build/install/com.unity.formats.fbx.

Testing
-------

Open `TestProjects/FbxTests` in Unity 2018.2+ and run using the Test Runner.
