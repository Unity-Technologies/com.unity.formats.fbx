# FBX Recorder

With the FBX Exporter and the [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@2.2/index.html), it is possible to export animations (including [Cinemachine](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.3/manual/index.html) camera animations) directly to FBX in a few easy steps:

1. Install the Unity Recorder package from the [Package Manager](https://docs.unity3d.com/Manual/upm-ui-install.html) if it is not already in the project
2. Open the Recorder window by selecting (Window > General > Recorder > Recorder Window)
3. In the Recorder window add a new FBX recorder

![](images/FBXExporter_AddRecorder.png)

3. Set the GameObject to record as well as any other desired settings
4. Click "Start Recording" in the Recorder Window

Alternatively, the FBX Recorder can be added as a track in the Timeline.

## Fbx Recorder Settings

![](images/FBXExporter_RecorderSettings.png)

| Property:                     | Function:                                                    |
| :---------------------------- | :----------------------------------------------------------- |
| __Export Geometry__              | Check this option to export the geometry of the recorded GameObject to FBX, if any. |
| __File Name__            | The filename for the exported FBX. |
| __Path__                 | The path to export the FBX to. Can be outside of the Assets folder. |
| __Take Number__          | The take number can be set and used in the filename. It automatically increments after each recording. |
| __Game Object__ | Set the Game Object in the scene to record. |
| __Recorded Target(s)__     | Select which components of the selected GameObject to record. |
| __Recorded Hierarchy__     | Check to record other objects in the hierarchy of the GameObject in addition to the selected GameObject. |
| __Source__                | Transfer the transform animation from this object to the __Destination__ transform. <br/><br/>**NOTES:**<br/> - __Source__ must be an ancestor of __Destination__<br/> - __Source__ may be an ancestor of the selected object. |
| __Destination__           | Which object to transfer the transform animation to.<br/><br/>This object receives the transform animation on objects between __Source__ and __Destination__ as well as the animation on the Source itself. |