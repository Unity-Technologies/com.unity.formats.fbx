using UnityEngine;
using Autodesk.Fbx;
using UnityEditor.Formats.Fbx.Exporter.CustomExtensions;
using System.Collections.Generic;

namespace UnityEditor.Formats.Fbx.Exporter
{
    namespace Visitors
    {
        internal static class CameraVisitor
        {
            private static Dictionary<Camera.GateFitMode, FbxCamera.EGateFit> s_mapGateFit = new Dictionary<Camera.GateFitMode, FbxCamera.EGateFit>()
            {
                { Camera.GateFitMode.Fill, FbxCamera.EGateFit.eFitFill },
                { Camera.GateFitMode.Horizontal, FbxCamera.EGateFit.eFitHorizontal },
                { Camera.GateFitMode.None, FbxCamera.EGateFit.eFitNone },
                { Camera.GateFitMode.Overscan, FbxCamera.EGateFit.eFitOverscan },
                { Camera.GateFitMode.Vertical, FbxCamera.EGateFit.eFitVertical }
            };

            /// <summary>
            /// Visit Object and configure FbxCamera
            /// </summary>
            public static void ConfigureCamera(Camera unityCamera, FbxCamera fbxCamera)
            {
                if (unityCamera.usePhysicalProperties)
                    ConfigurePhysicalCamera(fbxCamera, unityCamera);
                else
                    ConfigureGameCamera(fbxCamera, unityCamera);
            }

            /// <summary>
            /// Configure FbxCameras from GameCamera
            /// </summary>
            private static void ConfigureGameCamera(FbxCamera fbxCamera, Camera unityCamera)
            {
                // Configure FilmBack settings as a 35mm TV Projection (0.816 x 0.612)
                float aspectRatio = unityCamera.aspect;

                float apertureHeightInInches = 0.612f;
                float apertureWidthInInches = aspectRatio * apertureHeightInInches;

                FbxCamera.EProjectionType projectionType =
                    unityCamera.orthographic ? FbxCamera.EProjectionType.eOrthogonal : FbxCamera.EProjectionType.ePerspective;

                fbxCamera.ProjectionType.Set(projectionType);
                fbxCamera.FilmAspectRatio.Set(aspectRatio);
                fbxCamera.SetApertureWidth(apertureWidthInInches);
                fbxCamera.SetApertureHeight(apertureHeightInInches);
                fbxCamera.SetApertureMode(FbxCamera.EApertureMode.eVertical);

                // Focal Length
                double focalLength = fbxCamera.ComputeFocalLength(unityCamera.fieldOfView);

                fbxCamera.FocalLength.Set(focalLength);

                // Field of View
                fbxCamera.FieldOfView.Set(unityCamera.fieldOfView);

                // NearPlane
                fbxCamera.SetNearPlane(unityCamera.nearClipPlane.Meters().ToCentimeters());

                // FarPlane
                fbxCamera.SetFarPlane(unityCamera.farClipPlane.Meters().ToCentimeters());

                return;
            }

            public static Vector2 GetSizeOfMainGameView()
            {
#if UNITY_2020_1_OR_NEWER
                return Handles.GetMainGameViewSize();
#else
                System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
                return (Vector2)Res;
#endif // UNITY_2020_1_OR_NEWER
            }

            /// <summary>
            /// Configure FbxCameras from a Physical Camera
            /// </summary>
            private static void ConfigurePhysicalCamera(FbxCamera fbxCamera, Camera unityCamera)
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

                Vector2 gameViewSize = GetSizeOfMainGameView();
                fbxCamera.SetAspect(FbxCamera.EAspectRatioMode.eFixedRatio, gameViewSize.x / gameViewSize.y, 1.0);
                fbxCamera.SetApertureWidth(apertureWidthInInches);
                fbxCamera.SetApertureHeight(apertureHeightInInches);

                // Fit the resolution gate horizontally within the film gate.
                fbxCamera.GateFit.Set(s_mapGateFit[unityCamera.gateFit]);

                // Lens Shift ( Film Offset ) as a percentage 0..1
                // FBX FilmOffset is in inches
                fbxCamera.FilmOffsetX.Set(apertureWidthInInches * Mathf.Clamp(Mathf.Abs(unityCamera.lensShift.x), 0f, 1f) * Mathf.Sign(unityCamera.lensShift.x));
                fbxCamera.FilmOffsetY.Set(apertureHeightInInches * Mathf.Clamp(Mathf.Abs(unityCamera.lensShift.y), 0f, 1f) * Mathf.Sign(unityCamera.lensShift.y));

                // Focal Length
                fbxCamera.SetApertureMode(FbxCamera.EApertureMode.eFocalLength);

                double focalLength = (double)unityCamera.focalLength;
                fbxCamera.FocalLength.Set(focalLength); /* in millimeters */

                // NearPlane
                fbxCamera.SetNearPlane((double)unityCamera.nearClipPlane.Meters().ToCentimeters());

                // FarPlane
                fbxCamera.SetFarPlane((float)unityCamera.farClipPlane.Meters().ToCentimeters());

#if UNITY_2022_2_OR_NEWER
                fbxCamera.UseDepthOfField.Set(true);
                fbxCamera.FocusDistance.Set(unityCamera.focusDistance.Meters().ToCentimeters());
#endif
                return;
            }
        }
    }
}
