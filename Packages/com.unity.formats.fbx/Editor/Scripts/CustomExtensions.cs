using System;
using UnityEngine;
using UnityEngine.Formats.FbxSdk;

namespace UnityEditor.Formats.Fbx.Exporter
{ 
    namespace CustomExtensions
    {
        public class MetricDistance : object {

            public static readonly MetricDistance Millimeter = new MetricDistance(0.001f);
            public static readonly MetricDistance Centimeter = new MetricDistance(0.01f);
            public static readonly MetricDistance Meter = new MetricDistance(1.0f);

            private float _meters;

            public MetricDistance(float m) {
                this._meters = m;
            }

            public float ToMeters() {
                return this._meters;
            }

            public float ToCentimeters() {
                return this._meters / Centimeter._meters;
            }

            public float ToMillimeters() {
                return this._meters / Millimeter._meters;
            }

            public ImperialDistance ToImperial() {
                return new ImperialDistance(this._meters * 39.3701f);
            }

            public float ToInches() {
                return ToImperial().ToInches();
            }

            public override int GetHashCode() {
                return _meters.GetHashCode();
            }

            public override bool Equals(object obj) {
                var o = obj as MetricDistance;
                if (o == null) return false;
                return _meters.Equals(o._meters);
            }

            public static bool operator ==(MetricDistance a, MetricDistance b) {
                // If both are null, or both are same instance, return true
                if (ReferenceEquals(a, b)) return true;

                // if either one or the other are null, return false
                if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;

                return a._meters == b._meters;
            }

            public static bool operator !=(MetricDistance a, MetricDistance b) {
                return !(a == b);
            }

            public static MetricDistance operator +(MetricDistance a, MetricDistance b) {
                if (a == null) throw new ArgumentNullException("a");
                if (b == null) throw new ArgumentNullException("b");
                return new MetricDistance(a._meters + b._meters);
            }

            public static MetricDistance Add(MetricDistance a, MetricDistance b)
            {
                return a + b;
            }

            public static MetricDistance operator -(MetricDistance a, MetricDistance b) {
                if (a == null) throw new ArgumentNullException("a");
                if (b == null) throw new ArgumentNullException("b");
                return new MetricDistance(a._meters - b._meters);
            }

            public static MetricDistance Subtract(MetricDistance a, MetricDistance b)
            {
                return a - b;
            }

            public static MetricDistance operator *(MetricDistance a, MetricDistance b) {
                if (a == null) throw new ArgumentNullException("a");
                if (b == null) throw new ArgumentNullException("b");
                return new MetricDistance(a._meters * b._meters);
            }

            public static MetricDistance Multiply(MetricDistance a, MetricDistance b)
            {
                return a * b;
            }

            public static MetricDistance operator /(MetricDistance a, MetricDistance b) {
                if (a == null) throw new ArgumentNullException("a");
                if (b == null) throw new ArgumentNullException("b");
                return new MetricDistance(a._meters / b._meters);
            }

            public static MetricDistance Divide(MetricDistance a, MetricDistance b)
            {
                return a / b;
            }

        }

        public class ImperialDistance {

            public static readonly ImperialDistance Inch = new ImperialDistance(1.0f);
            public static readonly ImperialDistance Foot = new ImperialDistance(12.0f);

            private float _inches;

            public ImperialDistance(float m) {
                this._inches = m;
            }

            public MetricDistance ToMetric() {
                return new MetricDistance(this._inches * 0.0254f);
            }

            public float ToMeters() {
                return this.ToMetric().ToMeters();
            }

            public float ToInches() {
                return _inches;
            }

            public override int GetHashCode() {
                return _inches.GetHashCode();
            }

            public override bool Equals(object obj) {
                var o = obj as ImperialDistance;
                if (o == null) return false;
                return _inches.Equals(o._inches);
            }

            public static bool operator ==(ImperialDistance a, ImperialDistance b) {
                // If both are null, or both are same instance, return true
                if (ReferenceEquals(a, b)) return true;

                // if either one or the other are null, return false
                if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;

                return a._inches == b._inches;
            }

            public static bool operator !=(ImperialDistance a, ImperialDistance b) {
                return !(a == b);
            }

            public static ImperialDistance operator +(ImperialDistance a, ImperialDistance b) {
                if (a == null) throw new ArgumentNullException("a");
                if (b == null) throw new ArgumentNullException("b");
                return new ImperialDistance(a._inches + b._inches);
            }

            public static ImperialDistance operator -(ImperialDistance a, ImperialDistance b) {
                if (a == null) throw new ArgumentNullException("a");
                if (b == null) throw new ArgumentNullException("b");
                return new ImperialDistance(a._inches - b._inches);
            }

            public static ImperialDistance operator *(ImperialDistance a, ImperialDistance b) {
                if (a == null) throw new ArgumentNullException("a");
                if (b == null) throw new ArgumentNullException("b");
                return new ImperialDistance(a._inches * b._inches);
            }

            public static ImperialDistance operator /(ImperialDistance a, ImperialDistance b) {
                if (a == null) throw new ArgumentNullException("a");
                if (b == null) throw new ArgumentNullException("b");
                return new ImperialDistance(a._inches / b._inches);
            }
        }

        //Extension methods must be defined in a static class
        public static class FloatExtension
        {
            public static MetricDistance Meters(this float that) {
                return new MetricDistance(that);
            }
            public static MetricDistance Millimeters(this float that) {
                return new MetricDistance(MetricDistance.Millimeter.ToMeters() * that);
            }
            public static MetricDistance Centimeters(this float that) {
                return new MetricDistance(MetricDistance.Centimeter.ToMeters() * that);
            }
            public static ImperialDistance Inches(this float that) {
                return new ImperialDistance(that);
            }
            public static ImperialDistance Feet(this float that) {
                return new ImperialDistance(ImperialDistance.Foot.ToInches() * that);
            }
        }

        //Extension methods must be defined in a static class
        public static class Vector3Extension
        {
            public static Vector3 RightHanded (this Vector3 leftHandedVector)
            {
                // negating the x component of the vector converts it from left to right handed coordinates
                return new Vector3 (
                    -leftHandedVector [0],
                    leftHandedVector [1],
                    leftHandedVector [2]);
            }

            public static FbxVector4 FbxVector4 (this Vector3 uniVector)
            {
                return new FbxVector4 (
                    uniVector [0],
                    uniVector [1],
                    uniVector [2]);
            }
        }

        //Extension methods must be defined in a static class
        public static class AnimationCurveExtension
        {
            // This is an extension method for the AnimationCurve class
            // The first parameter takes the "this" modifier
            // and specifies the type for which the method is defined.
            public static void Dump (this AnimationCurve animCurve, string message="", float[] keyTimesExpected = null, float[] keyValuesExpected = null)
            {
                int idx = 0;
                foreach (var key in animCurve.keys) {
                    if (keyTimesExpected != null && keyValuesExpected != null && keyTimesExpected.Length==keyValuesExpected.Length) {
                        Debug.Log (string.Format ("{5} keys[{0}] {1}({3}) {2} ({4})",
                            idx, key.time, key.value,
                            keyTimesExpected [idx], keyValuesExpected [idx],
                            message));
                    } else {
                        Debug.Log (string.Format ("{3} keys[{0}] {1} {2}", idx, key.time, key.value, message));
                    }
                    idx++;
                }
            }
        }
    }       
}

