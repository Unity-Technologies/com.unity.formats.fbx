# Export with relevant system units

Learn about how the FBX Exporter package manages system units and how to keep unit compatibility with other software.

## Meters to centimeters

The FBX Exporter exports in centimeter units (cm) with the Mesh set to real world meter (m) scale.

For example, if vertex[0] is at [1, 1, 1] m, the FBX Exporter converts it to [100, 100, 100] cm.

## Autodesk® software unit compatibility

In Autodesk® 3ds Max®, it is recommended to set the system units to centimeters to avoid any scaling on Model import and export.

There are no specific import options to adjust between the Unity Editor and Autodesk® Maya® and Autodesk® Maya LT™. When you work in Autodesk® Maya® and Autodesk® Maya LT™, you can set the working units to meters if you prefer.

## Unit recommendation for large models

When you work with large models in Autodesk® Maya® and Autodesk® Maya LT™, to ensure that the models clip to meters, adjust the scale of the near and far clipping planes for all cameras by 100x. In addition, you should scale lights and cameras by 100x so that objects display in the viewport.
