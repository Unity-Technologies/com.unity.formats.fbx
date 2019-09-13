# FbxExporters

Copyright (c) 2018 Unity Technologies. All rights reserved.

See LICENSE.md file in the project root for full license information.

Requirements
------------

* [FBX SDK C# Bindings](https://github.com/Unity-Technologies/com.autodesk.fbx) package

Packaging
---------

Get the source the first time:
```
# clone the source
git clone https://github.com/Unity-Technologies/com.unity.formats.fbx.git
cd com.unity.formats.fbx
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

The package will be built in-place in com.unity.formats.fbx.

Testing
-------

Open `TestProjects/FbxTests` in Unity 2018.2+ and run using the Test Runner.

Reporting Bugs
--------------

Please create a minimal project that reproduces the bug and use the Unity Bug Report (built in to the Unity Editor).
