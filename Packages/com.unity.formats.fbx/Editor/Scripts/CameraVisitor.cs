using UnityEngine;
using Autodesk.Fbx;
using UnityEditor.Formats.Fbx.Exporter.CustomExtensions;

namespace UnityEditor.Formats.Fbx.Exporter
{
    namespace Visitors
    {
        internal static class CameraVisitor
        {
            /// <summary>
            /// Visit Object and configure FbxCamera
            /// </summary>
            public static void ConfigureCamera (Camera unityCamera, FbxCamera fbxCamera)
            {
                if (unityCamera.usePhysicalProperties) 
                    ConfigurePhysicalCamera(fbxCamera, unityCamera);
                else 
                    ConfigureGameCamera(fbxCamera, unityCamera);
            }

            /// <summary>
            /// Configure FbxCameras from GameCamera 
            /// </summary>
            private static void ConfigureGameCamera (FbxCamera fbxCamera, Camera unityCamera)
            {
                // Configure FilmBack settings as a 35mm TV Projection (0.816 x 0.612)
                float aspectRatio = unityCamera.aspect;

                float apertureHeightInInches = 0.612f;
                float apertureWidthInInches = aspectRatio * apertureHeightInInches;

                FbxCamera.EProjectionType projectionType =
                    unityCamera.orthographic ? FbxCamera.EProjectionType.eOrthogonal : FbxCamera.EProjectionType.ePerspective;

                fbxCamera.ProjectionType.Set(projectionType);
                fbxCamera.FilmAspectRatio.Set(aspectRatio);
                fbxCamera.SetApertureWidth (apertureWidthInInches);
                fbxCamera.SetApertureHeight (apertureHeightInInches);
                fbxCamera.SetApertureMode (FbxCamera.EApertureMode.eVertical);

                // Focal Length
                double focalLength = fbxCamera.ComputeFocalLength (unityCamera.fieldOfView);

                fbxCamera.FocalLength.Set(focalLength);

                // Field of View
                fbxCamera.FieldOfView.Set (unityCamera.fieldOfView);

                // NearPlane
                fbxCamera.SetNearPlane (unityCamera.nearClipPlane.Meters().ToCentimeters());

                // FarPlane
                fbxCamera.SetFarPlane (unityCamera.farClipPlane.Meters().ToCentimeters());

                return ;
            }

            /// <summary>
            /// Configure FbxCameras from a Physical Camera 
            /// </summary>
            private static void ConfigurePhysicalCamera (FbxCamera fbxCamera, Camera unityCamera)
            {
                Debug.Assert(unityCamera.usePhysicalProperties);

                // Configure FilmBack settings
                float apertureHeightInInches = unityCamera.sensorSize.y.Millimeters().ToInches();
                float apertureWidthInInches = unityCamera.sensorSize.x.Millimeters().ToInches();
                float aspectRatio = apertureWidthInInches / apertureHeightInInches;

                FbxCamera.EProjectionType projectionType = unityCamera.orthographic 
                    ? FbxCamera.EProjectionType.eOrthogonal 
                    : FbxCamera.EProjectionType.ePerspective;

                // NOTE: it is possible to match some of the sensor sizes to the  
                // predefined EApertureFormats : e16mmTheatrical, eSuper16mm, 
                // e35mmFullAperture, eIMAX. However the round in the sizes is not
                // consistent between Unity and FBX so we choose
                // to leave the values as a eCustomAperture setting.

                fbxCamera.ProjectionType.Set(projectionType);
                fbxCamera.FilmAspectRatio.Set(aspectRatio);
                fbxCamera.SetApertureWidth (apertureWidthInInches);
                fbxCamera.SetApertureHeight (apertureHeightInInches);

                // Fit the resolution gate horizontally within the film gate.
                fbxCamera.GateFit.Set(FbxCamera.EGateFit.eFitHorizontal);

                // Lens Shift ( Film Offset ) as a percentage 0..1
                // FBX FilmOffset is in inches
                fbxCamera.FilmOffsetX.Set(apertureWidthInInches * Mathf.Clamp(unityCamera.lensShift.x, 0f, 1f));
                fbxCamera.FilmOffsetY.Set(apertureHeightInInches * Mathf.Clamp(unityCamera.lensShift.y, 0f, 1f));

                // Focal Length
                fbxCamera.SetApertureMode (FbxCamera.EApertureMode.eFocalLength); 

                double focalLength = (double)unityCamera.focalLength;
                fbxCamera.FocalLength.Set(focalLength); /* in millimeters */

                // NearPlane
                fbxCamera.SetNearPlane ((double)unityCamera.nearClipPlane.Meters().ToCentimeters());

                // FarPlane
                fbxCamera.SetFarPlane ((float)unityCamera.farClipPlane.Meters().ToCentimeters());

                return ;
            }
        }
    }
}

