using Unity.FbxSdk;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace FbxExporters
{
    namespace Editor
    {
        /// <summary>
        /// Base class for QuaternionCurve and EulerCurve.
        /// Provides implementation for computing keys and generating FbxAnimCurves
        /// for euler rotation.
        /// </summary>
        public abstract class RotationCurve {
            public double sampleRate;
            public AnimationCurve[] m_curves;
            public FbxNode m_fbxNode;

            private FbxProperty m_animatedProperty;
            public FbxProperty AnimatedProperty { get { return m_animatedProperty; } set { m_animatedProperty = value; } }

            public abstract string FbxPropertyName { get; }

            public struct Key {
                public FbxTime time;
                public FbxVector4 euler;
            }

            public RotationCurve() { }

            public void SetCurve(int i, AnimationCurve curve) {
                m_curves [i] = curve;
            }

            protected abstract FbxQuaternion GetConvertedQuaternionRotation (float seconds, UnityEngine.Quaternion restRotation);

            private Key [] ComputeKeys(UnityEngine.Quaternion restRotation, bool undoPrerotation = true) {
                // Get the source pivot pre-rotation if any, so we can
                // remove it from the animation we get from Unity.
                var fbxPreRotationEuler = undoPrerotation && m_fbxNode != null && m_fbxNode.GetRotationActive() 
                    ? m_fbxNode.GetPreRotation(FbxNode.EPivotSet.eSourcePivot)
                    : new FbxVector4();

                // Get the inverse of the prerotation
                var fbxPreRotationInverse = ModelExporter.EulerToQuaternion (fbxPreRotationEuler);
                fbxPreRotationInverse.Inverse();

                // Find when we have keys set.
                var keyTimes = 
                    (FbxExporters.Editor.ModelExporter.ExportSettings.BakeAnimation) 
                    ? ModelExporter.GetSampleTimes(m_curves, sampleRate) 
                    : ModelExporter.GetKeyTimes(m_curves);

                // Convert to the Key type.
                var keys = new Key[keyTimes.Count];
                int i = 0;
                foreach(var seconds in keyTimes) {
                    var fbxFinalAnimation = GetConvertedQuaternionRotation (seconds, restRotation);

                    // Cancel out the pre-rotation. Order matters. FBX reads left-to-right.
                    // When we run animation we will apply:
                    //      pre-rotation
                    //      then pre-rotation inverse
                    //      then animation.
                    var fbxFinalQuat = fbxPreRotationInverse * fbxFinalAnimation;

                    // Store the key so we can sort them later.
                    Key key;
                    key.time = FbxTime.FromSecondDouble(seconds);
                    key.euler = ModelExporter.QuaternionToEuler (fbxFinalQuat);
                    keys[i++] = key;
                }

                // Sort the keys by time
                System.Array.Sort(keys, (Key a, Key b) => a.time.CompareTo(b.time));

                return keys;
            }

            public void Animate(Quaternion restRotation, FbxAnimLayer fbxAnimLayer, bool Verbose, bool undoPreRotation = true) {
                if (!AnimatedProperty.IsValid())
                {
                    Debug.LogError("missing animatable property");
                }

                /* Find or create the three curves. */
                var fbxAnimCurveX = AnimatedProperty.GetCurve(fbxAnimLayer, Globals.FBXSDK_CURVENODE_COMPONENT_X, true);
                var fbxAnimCurveY = AnimatedProperty.GetCurve(fbxAnimLayer, Globals.FBXSDK_CURVENODE_COMPONENT_Y, true);
                var fbxAnimCurveZ = AnimatedProperty.GetCurve(fbxAnimLayer, Globals.FBXSDK_CURVENODE_COMPONENT_Z, true);

                /* set the keys */
                using (new FbxAnimCurveModifyHelper(new List<FbxAnimCurve>{fbxAnimCurveX,fbxAnimCurveY,fbxAnimCurveZ}))
                {
                    foreach (var key in ComputeKeys(restRotation, undoPreRotation)) {

                        int i = fbxAnimCurveX.KeyAdd(key.time);
                        fbxAnimCurveX.KeySet(i, key.time, (float)key.euler.X);

                        i = fbxAnimCurveY.KeyAdd(key.time);
                        fbxAnimCurveY.KeySet(i, key.time, (float)key.euler.Y);

                        i = fbxAnimCurveZ.KeyAdd(key.time);
                        fbxAnimCurveZ.KeySet(i, key.time, (float)key.euler.Z);
                    }
                }

                // Uni-35616 unroll curves to preserve continuous rotations
                var fbxCurveNode = AnimatedProperty.GetCurveNode(fbxAnimLayer, false /*should already exist*/);

                FbxAnimCurveFilterUnroll fbxAnimUnrollFilter = new FbxAnimCurveFilterUnroll();
                fbxAnimUnrollFilter.Apply(fbxCurveNode);

                if (Verbose && m_fbxNode != null) {
                    Debug.Log("Exported rotation animation for " + m_fbxNode.GetName());
                }
            }
        }

        /// <summary>
        /// Convert from ZXY to XYZ euler, and remove
        /// prerotation from animated rotation.
        /// </summary>
        public class EulerCurve : RotationCurve {
            public EulerCurve() { m_curves = new AnimationCurve[3]; }

            public override string FbxPropertyName
            {
                get
                {
                    return "Lcl Rotation";
                }
            }

            protected static int GetAxisIndex(char axis)
            {
                switch (axis)
                {
                    case 'x': return 0;
                    case 'y': return 1;
                    case 'z': return 2;
                    default: return -1;
                }
            }

            /// <summary>
            /// Gets the index of the euler curve by property name.
            /// x = 0, y = 1, z = 2
            /// </summary>
            /// <returns>The index of the curve, or -1 if property doesn't map to Euler curve.</returns>
            /// <param name="uniPropertyName">Unity property name.</param>
            public static int GetEulerIndex(string uniPropertyName) {
                System.StringComparison ct = System.StringComparison.CurrentCulture;
                bool isEulerComponent = uniPropertyName.StartsWith ("localEulerAnglesRaw.", ct);

                if (!isEulerComponent) { return -1; }

                return GetAxisIndex(uniPropertyName[uniPropertyName.Length - 1]);
            }

            protected override FbxQuaternion GetConvertedQuaternionRotation (float seconds, Quaternion restRotation)
            {
                var eulerRest = restRotation.eulerAngles;
                AnimationCurve x = m_curves [0], y = m_curves [1], z = m_curves [2];

                // The final animation, including the effect of pre-rotation.
                // If we have no curve, assume the node has the correct rotation right now.
                // We need to evaluate since we might only have keys in one of the axes.
                var unityFinalAnimation = Quaternion.Euler (
                    (x == null) ? eulerRest [0] : x.Evaluate (seconds),
                    (y == null) ? eulerRest [1] : y.Evaluate (seconds),
                    (z == null) ? eulerRest [2] : z.Evaluate (seconds)
                );

                // convert the final animation to righthanded coords
                var finalEuler = ModelExporter.ConvertQuaternionToXYZEuler(unityFinalAnimation);

                return ModelExporter.EulerToQuaternion (new FbxVector4(finalEuler));
            }
        }

        public class AimConstraintRotationCurve : EulerCurve
        {
            public override string FbxPropertyName
            {
                get
                {
                    return "RotationOffset";
                }
            }

            /// <summary>
            /// Gets the index of the rotation constraint's curve by property name.
            /// x = 0, y = 1, z = 2
            /// </summary>
            /// <returns>The index of the curve, or -1 if property doesn't map to Euler curve.</returns>
            /// <param name="uniPropertyName">Unity property name.</param>
            public static int GetRotationIndex(string uniPropertyName, Type constraintType)
            {
                System.StringComparison ct = System.StringComparison.CurrentCulture;
                bool isRotationComponent = uniPropertyName.StartsWith("m_RotationOffset.", ct) && constraintType == typeof(UnityEngine.Animations.AimConstraint);

                if (!isRotationComponent) { return -1; }

                return GetAxisIndex(uniPropertyName[uniPropertyName.Length - 1]);
            }
        }

        public class RotationConstraintCurve : EulerCurve
        {
            public override string FbxPropertyName
            {
                get
                {
                    return "Rotation";
                }
            }

            /// <summary>
            /// Gets the index of the rotation constraint's curve by property name.
            /// x = 0, y = 1, z = 2
            /// </summary>
            /// <returns>The index of the curve, or -1 if property doesn't map to Euler curve.</returns>
            /// <param name="uniPropertyName">Unity property name.</param>
            public static int GetRotationIndex(string uniPropertyName, Type constraintType)
            {
                System.StringComparison ct = System.StringComparison.CurrentCulture;
                bool isRotationComponent = uniPropertyName.StartsWith("m_RotationOffset.", ct) && constraintType == typeof(UnityEngine.Animations.RotationConstraint);

                if (!isRotationComponent) { return -1; }

                return GetAxisIndex(uniPropertyName[uniPropertyName.Length - 1]);
            }
        }

        /// <summary>
        /// Exporting rotations is more complicated. We need to convert
        /// from quaternion to euler. We use this class to help.
        /// </summary>
        public class QuaternionCurve : RotationCurve {

            public QuaternionCurve() { m_curves = new AnimationCurve[4]; }

            public override string FbxPropertyName
            {
                get
                {
                    return "Lcl Rotation";
                }
            }

            /// <summary>
            /// Gets the index of the curve by property name.
            /// x = 0, y = 1, z = 2, w = 3
            /// </summary>
            /// <returns>The index of the curve, or -1 if property doesn't map to Quaternion curve.</returns>
            /// <param name="uniPropertyName">Unity property name.</param>
            public static int GetQuaternionIndex(string uniPropertyName) {
                System.StringComparison ct = System.StringComparison.CurrentCulture;
                bool isQuaternionComponent = false;

                isQuaternionComponent |= uniPropertyName.StartsWith ("m_LocalRotation.", ct);
                isQuaternionComponent |= uniPropertyName.EndsWith ("Q.x", ct);
                isQuaternionComponent |= uniPropertyName.EndsWith ("Q.y", ct);
                isQuaternionComponent |= uniPropertyName.EndsWith ("Q.z", ct);
                isQuaternionComponent |= uniPropertyName.EndsWith ("Q.w", ct);

                if (!isQuaternionComponent) { return -1; }

                switch(uniPropertyName[uniPropertyName.Length - 1]) {
                case 'x': return 0;
                case 'y': return 1;
                case 'z': return 2;
                case 'w': return 3;
                default: return -1;
                }
            }

            protected override FbxQuaternion GetConvertedQuaternionRotation (float seconds, Quaternion restRotation)
            {
                AnimationCurve x = m_curves [0], y = m_curves [1], z = m_curves [2], w = m_curves[3];

                // The final animation, including the effect of pre-rotation.
                // If we have no curve, assume the node has the correct rotation right now.
                // We need to evaluate since we might only have keys in one of the axes.
                var fbxFinalAnimation = new FbxQuaternion(
                    (x == null) ? restRotation[0] : x.Evaluate(seconds),
                    (y == null) ? restRotation[1] : y.Evaluate(seconds),
                    (z == null) ? restRotation[2] : z.Evaluate(seconds),
                    (w == null) ? restRotation[3] : w.Evaluate(seconds));

                // convert the final animation to righthanded coords
                var finalEuler = ModelExporter.ConvertQuaternionToXYZEuler(fbxFinalAnimation);

                return ModelExporter.EulerToQuaternion (finalEuler);
            }
        }

        /// <summary>
        /// Exporting rotations is more complicated. We need to convert
        /// from quaternion to euler. We use this class to help.
        /// </summary>
        public class FbxAnimCurveModifyHelper : System.IDisposable 
        {
            public List<FbxAnimCurve> Curves { get ; private set; }

            public FbxAnimCurveModifyHelper(List<FbxAnimCurve> list)
            {
                Curves = list;

                foreach (var curve in Curves)
                    curve.KeyModifyBegin();
            }

            ~FbxAnimCurveModifyHelper() {
                Dispose();
            }

            public void Dispose() 
            {
                foreach (var curve in Curves)
                    curve.KeyModifyEnd();
            }
        }
    }
}