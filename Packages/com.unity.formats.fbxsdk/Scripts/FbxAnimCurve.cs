//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 3.0.12
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace Unity.FbxSdk {

public class FbxAnimCurve : FbxAnimCurveBase {
  internal FbxAnimCurve(global::System.IntPtr cPtr, bool ignored) : base(cPtr, ignored) { }

  // override void Dispose() {base.Dispose();}

  public static FbxAnimCurve Create(FbxScene pContainer, string pName) {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxAnimCurve_Create(FbxScene.getCPtr(pContainer), pName);
    FbxAnimCurve ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxAnimCurve(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public virtual void KeyModifyBegin() {
    GlobalsPINVOKE.FbxAnimCurve_KeyModifyBegin(swigCPtr);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual void KeyModifyEnd() {
    GlobalsPINVOKE.FbxAnimCurve_KeyModifyEnd(swigCPtr);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual int KeyAdd(FbxTime pTime, ref int pLast) {
    int ret = GlobalsPINVOKE.FbxAnimCurve_KeyAdd__SWIG_0(swigCPtr, FbxTime.getCPtr(pTime), ref pLast);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public virtual int KeyAdd(FbxTime pTime) {
    int ret = GlobalsPINVOKE.FbxAnimCurve_KeyAdd__SWIG_1(swigCPtr, FbxTime.getCPtr(pTime));
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public virtual void KeySet(int pKeyIndex, FbxTime pTime, float pValue, FbxAnimCurveDef.EInterpolationType pInterpolation, FbxAnimCurveDef.ETangentMode pTangentMode, float pData0, float pData1, FbxAnimCurveDef.EWeightedMode pTangentWeightMode, float pWeight0, float pWeight1, float pVelocity0, float pVelocity1) {
    GlobalsPINVOKE.FbxAnimCurve_KeySet__SWIG_0(swigCPtr, pKeyIndex, FbxTime.getCPtr(pTime), pValue, (int)pInterpolation, (int)pTangentMode, pData0, pData1, (int)pTangentWeightMode, pWeight0, pWeight1, pVelocity0, pVelocity1);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual void KeySet(int pKeyIndex, FbxTime pTime, float pValue, FbxAnimCurveDef.EInterpolationType pInterpolation, FbxAnimCurveDef.ETangentMode pTangentMode, float pData0, float pData1, FbxAnimCurveDef.EWeightedMode pTangentWeightMode, float pWeight0, float pWeight1, float pVelocity0) {
    GlobalsPINVOKE.FbxAnimCurve_KeySet__SWIG_1(swigCPtr, pKeyIndex, FbxTime.getCPtr(pTime), pValue, (int)pInterpolation, (int)pTangentMode, pData0, pData1, (int)pTangentWeightMode, pWeight0, pWeight1, pVelocity0);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual void KeySet(int pKeyIndex, FbxTime pTime, float pValue, FbxAnimCurveDef.EInterpolationType pInterpolation, FbxAnimCurveDef.ETangentMode pTangentMode, float pData0, float pData1, FbxAnimCurveDef.EWeightedMode pTangentWeightMode, float pWeight0, float pWeight1) {
    GlobalsPINVOKE.FbxAnimCurve_KeySet__SWIG_2(swigCPtr, pKeyIndex, FbxTime.getCPtr(pTime), pValue, (int)pInterpolation, (int)pTangentMode, pData0, pData1, (int)pTangentWeightMode, pWeight0, pWeight1);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual void KeySet(int pKeyIndex, FbxTime pTime, float pValue, FbxAnimCurveDef.EInterpolationType pInterpolation, FbxAnimCurveDef.ETangentMode pTangentMode, float pData0, float pData1, FbxAnimCurveDef.EWeightedMode pTangentWeightMode, float pWeight0) {
    GlobalsPINVOKE.FbxAnimCurve_KeySet__SWIG_3(swigCPtr, pKeyIndex, FbxTime.getCPtr(pTime), pValue, (int)pInterpolation, (int)pTangentMode, pData0, pData1, (int)pTangentWeightMode, pWeight0);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual void KeySet(int pKeyIndex, FbxTime pTime, float pValue, FbxAnimCurveDef.EInterpolationType pInterpolation, FbxAnimCurveDef.ETangentMode pTangentMode, float pData0, float pData1, FbxAnimCurveDef.EWeightedMode pTangentWeightMode) {
    GlobalsPINVOKE.FbxAnimCurve_KeySet__SWIG_4(swigCPtr, pKeyIndex, FbxTime.getCPtr(pTime), pValue, (int)pInterpolation, (int)pTangentMode, pData0, pData1, (int)pTangentWeightMode);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual void KeySet(int pKeyIndex, FbxTime pTime, float pValue, FbxAnimCurveDef.EInterpolationType pInterpolation, FbxAnimCurveDef.ETangentMode pTangentMode, float pData0, float pData1) {
    GlobalsPINVOKE.FbxAnimCurve_KeySet__SWIG_5(swigCPtr, pKeyIndex, FbxTime.getCPtr(pTime), pValue, (int)pInterpolation, (int)pTangentMode, pData0, pData1);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual void KeySet(int pKeyIndex, FbxTime pTime, float pValue, FbxAnimCurveDef.EInterpolationType pInterpolation, FbxAnimCurveDef.ETangentMode pTangentMode, float pData0) {
    GlobalsPINVOKE.FbxAnimCurve_KeySet__SWIG_6(swigCPtr, pKeyIndex, FbxTime.getCPtr(pTime), pValue, (int)pInterpolation, (int)pTangentMode, pData0);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual void KeySet(int pKeyIndex, FbxTime pTime, float pValue, FbxAnimCurveDef.EInterpolationType pInterpolation, FbxAnimCurveDef.ETangentMode pTangentMode) {
    GlobalsPINVOKE.FbxAnimCurve_KeySet__SWIG_7(swigCPtr, pKeyIndex, FbxTime.getCPtr(pTime), pValue, (int)pInterpolation, (int)pTangentMode);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual void KeySet(int pKeyIndex, FbxTime pTime, float pValue, FbxAnimCurveDef.EInterpolationType pInterpolation) {
    GlobalsPINVOKE.FbxAnimCurve_KeySet__SWIG_8(swigCPtr, pKeyIndex, FbxTime.getCPtr(pTime), pValue, (int)pInterpolation);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual void KeySet(int pKeyIndex, FbxTime pTime, float pValue) {
    GlobalsPINVOKE.FbxAnimCurve_KeySet__SWIG_9(swigCPtr, pKeyIndex, FbxTime.getCPtr(pTime), pValue);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public virtual float KeyGetValue(int pKeyIndex) {
    float ret = GlobalsPINVOKE.FbxAnimCurve_KeyGetValue(swigCPtr, pKeyIndex);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public override int GetHashCode(){
      return swigCPtr.Handle.GetHashCode();
  }

  public bool Equals(FbxAnimCurve other) {
    if (object.ReferenceEquals(other, null)) { return false; }
    return this.swigCPtr.Handle.Equals (other.swigCPtr.Handle);
  }

  public override bool Equals(object obj){
    if (object.ReferenceEquals(obj, null)) { return false; }
    /* is obj a subclass of this type; if so use our Equals */
    var typed = obj as FbxAnimCurve;
    if (!object.ReferenceEquals(typed, null)) {
      return this.Equals(typed);
    }
    /* are we a subclass of the other type; if so use their Equals */
    if (typeof(FbxAnimCurve).IsSubclassOf(obj.GetType())) {
      return obj.Equals(this);
    }
    /* types are unrelated; can't be a match */
    return false;
  }

  public static bool operator == (FbxAnimCurve a, FbxAnimCurve b) {
    if (object.ReferenceEquals(a, b)) { return true; }
    if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) { return false; }
    return a.Equals(b);
  }

  public static bool operator != (FbxAnimCurve a, FbxAnimCurve b) {
    return !(a == b);
  }

  public static new FbxAnimCurve Create(FbxManager pManager, string pName) {
    throw new System.NotImplementedException("FbxAnimCurve can only be created with a scene as argument.");
  }
  public static new FbxAnimCurve Create(FbxObject pContainer, string pName) {
    throw new System.NotImplementedException("FbxAnimCurve can only be created with a scene as argument.");
  }

}

}