//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 3.0.12
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace UnityEngine.Formats.FbxSdk {

public class FbxSurfaceMaterial : FbxObject {
  internal FbxSurfaceMaterial(global::System.IntPtr cPtr, bool ignored) : base(cPtr, ignored) { }

  // override void Dispose() {base.Dispose();}

  public new static FbxSurfaceMaterial Create(FbxManager pManager, string pName) {
    global::System.IntPtr cPtr = NativeMethods.FbxSurfaceMaterial_Create__SWIG_0(FbxManager.getCPtr(pManager), pName);
    FbxSurfaceMaterial ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxSurfaceMaterial(cPtr, false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public new static FbxSurfaceMaterial Create(FbxObject pContainer, string pName) {
    global::System.IntPtr cPtr = NativeMethods.FbxSurfaceMaterial_Create__SWIG_1(FbxObject.getCPtr(pContainer), pName);
    FbxSurfaceMaterial ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxSurfaceMaterial(cPtr, false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public static string sShadingModel {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sShadingModel_get();
      return ret;
    } 
  }

  public static string sMultiLayer {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sMultiLayer_get();
      return ret;
    } 
  }

  public static string sEmissive {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sEmissive_get();
      return ret;
    } 
  }

  public static string sEmissiveFactor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sEmissiveFactor_get();
      return ret;
    } 
  }

  public static string sAmbient {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sAmbient_get();
      return ret;
    } 
  }

  public static string sAmbientFactor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sAmbientFactor_get();
      return ret;
    } 
  }

  public static string sDiffuse {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sDiffuse_get();
      return ret;
    } 
  }

  public static string sDiffuseFactor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sDiffuseFactor_get();
      return ret;
    } 
  }

  public static string sSpecular {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sSpecular_get();
      return ret;
    } 
  }

  public static string sSpecularFactor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sSpecularFactor_get();
      return ret;
    } 
  }

  public static string sShininess {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sShininess_get();
      return ret;
    } 
  }

  public static string sBump {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sBump_get();
      return ret;
    } 
  }

  public static string sNormalMap {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sNormalMap_get();
      return ret;
    } 
  }

  public static string sBumpFactor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sBumpFactor_get();
      return ret;
    } 
  }

  public static string sTransparentColor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sTransparentColor_get();
      return ret;
    } 
  }

  public static string sTransparencyFactor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sTransparencyFactor_get();
      return ret;
    } 
  }

  public static string sReflection {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sReflection_get();
      return ret;
    } 
  }

  public static string sReflectionFactor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sReflectionFactor_get();
      return ret;
    } 
  }

  public static string sDisplacementColor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sDisplacementColor_get();
      return ret;
    } 
  }

  public static string sDisplacementFactor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sDisplacementFactor_get();
      return ret;
    } 
  }

  public static string sVectorDisplacementColor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sVectorDisplacementColor_get();
      return ret;
    } 
  }

  public static string sVectorDisplacementFactor {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sVectorDisplacementFactor_get();
      return ret;
    } 
  }

  public FbxPropertyString ShadingModel {
    get {
      FbxPropertyString ret = new FbxPropertyString(NativeMethods.FbxSurfaceMaterial_ShadingModel_get(swigCPtr), false);
      if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public FbxPropertyBool MultiLayer {
    get {
      FbxPropertyBool ret = new FbxPropertyBool(NativeMethods.FbxSurfaceMaterial_MultiLayer_get(swigCPtr), false);
      if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public static bool sMultiLayerDefault {
    get {
      bool ret = NativeMethods.FbxSurfaceMaterial_sMultiLayerDefault_get();
      return ret;
    } 
  }

  public static string sShadingModelDefault {
    get {
      string ret = NativeMethods.FbxSurfaceMaterial_sShadingModelDefault_get();
      return ret;
    } 
  }

  public override int GetHashCode(){
      return swigCPtr.Handle.GetHashCode();
  }

  public bool Equals(FbxSurfaceMaterial other) {
    if (object.ReferenceEquals(other, null)) { return false; }
    return this.swigCPtr.Handle.Equals (other.swigCPtr.Handle);
  }

  public override bool Equals(object obj){
    if (object.ReferenceEquals(obj, null)) { return false; }
    /* is obj a subclass of this type; if so use our Equals */
    var typed = obj as FbxSurfaceMaterial;
    if (!object.ReferenceEquals(typed, null)) {
      return this.Equals(typed);
    }
    /* are we a subclass of the other type; if so use their Equals */
    if (typeof(FbxSurfaceMaterial).IsSubclassOf(obj.GetType())) {
      return obj.Equals(this);
    }
    /* types are unrelated; can't be a match */
    return false;
  }

  public static bool operator == (FbxSurfaceMaterial a, FbxSurfaceMaterial b) {
    if (object.ReferenceEquals(a, b)) { return true; }
    if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) { return false; }
    return a.Equals(b);
  }

  public static bool operator != (FbxSurfaceMaterial a, FbxSurfaceMaterial b) {
    return !(a == b);
  }

}

}
