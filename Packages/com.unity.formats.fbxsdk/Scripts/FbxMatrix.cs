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

public class FbxMatrix : FbxDouble4x4 {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;

  internal FbxMatrix(global::System.IntPtr cPtr, bool cMemoryOwn) : base(GlobalsPINVOKE.FbxMatrix_SWIGUpcast(cPtr), cMemoryOwn) {
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(FbxMatrix obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~FbxMatrix() {
    Dispose();
  }

  public override void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          GlobalsPINVOKE.delete_FbxMatrix(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
      base.Dispose();
    }
  }

  public FbxMatrix() : this(GlobalsPINVOKE.new_FbxMatrix__SWIG_0(), true) {
  }

  public FbxMatrix(FbxMatrix pM) : this(GlobalsPINVOKE.new_FbxMatrix__SWIG_1(FbxMatrix.getCPtr(pM)), true) {
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxMatrix(FbxAMatrix pM) : this(GlobalsPINVOKE.new_FbxMatrix__SWIG_2(FbxAMatrix.getCPtr(pM)), true) {
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxMatrix(FbxVector4 pT, FbxVector4 pR, FbxVector4 pS) : this(GlobalsPINVOKE.new_FbxMatrix__SWIG_3(pT, pR, pS), true) {
  }

  public FbxMatrix(FbxVector4 pT, FbxQuaternion pQ, FbxVector4 pS) : this(GlobalsPINVOKE.new_FbxMatrix__SWIG_4(pT, FbxQuaternion.getCPtr(pQ), pS), true) {
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxMatrix(double p00, double p10, double p20, double p30, double p01, double p11, double p21, double p31, double p02, double p12, double p22, double p32, double p03, double p13, double p23, double p33) : this(GlobalsPINVOKE.new_FbxMatrix__SWIG_5(p00, p10, p20, p30, p01, p11, p21, p31, p02, p12, p22, p32, p03, p13, p23, p33), true) {
  }

  public double Get(int pY, int pX) {
    double ret = GlobalsPINVOKE.FbxMatrix_Get(swigCPtr, pY, pX);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxVector4 GetRow(int pY) {
    var ret = GlobalsPINVOKE.FbxMatrix_GetRow(swigCPtr, pY);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxVector4 GetColumn(int pX) {
    var ret = GlobalsPINVOKE.FbxMatrix_GetColumn(swigCPtr, pX);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void Set(int pY, int pX, double pValue) {
    GlobalsPINVOKE.FbxMatrix_Set(swigCPtr, pY, pX, pValue);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetTRS(FbxVector4 pT, FbxVector4 pR, FbxVector4 pS) {
    GlobalsPINVOKE.FbxMatrix_SetTRS(swigCPtr, pT, pR, pS);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetTQS(FbxVector4 pT, FbxQuaternion pQ, FbxVector4 pS) {
    GlobalsPINVOKE.FbxMatrix_SetTQS(swigCPtr, pT, FbxQuaternion.getCPtr(pQ), pS);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetRow(int pY, FbxVector4 pRow) {
    GlobalsPINVOKE.FbxMatrix_SetRow(swigCPtr, pY, pRow);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetColumn(int pX, FbxVector4 pColumn) {
    GlobalsPINVOKE.FbxMatrix_SetColumn(swigCPtr, pX, pColumn);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void GetElements(out FbxVector4 pTranslation, FbxQuaternion pRotation, out FbxVector4 pShearing, out FbxVector4 pScaling, out double pSign) {
    GlobalsPINVOKE.FbxMatrix_GetElements__SWIG_0(swigCPtr, out pTranslation, FbxQuaternion.getCPtr(pRotation), out pShearing, out pScaling, out pSign);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void GetElements(out FbxVector4 pTranslation, out FbxVector4 pRotation, out FbxVector4 pShearing, out FbxVector4 pScaling, out double pSign) {
    GlobalsPINVOKE.FbxMatrix_GetElements__SWIG_1(swigCPtr, out pTranslation, out pRotation, out pShearing, out pScaling, out pSign);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  private FbxMatrix operator_Negate() {
    FbxMatrix ret = new FbxMatrix(GlobalsPINVOKE.FbxMatrix_operator_Negate(swigCPtr), true);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  private FbxMatrix operator_Add(FbxMatrix pMatrix) {
    FbxMatrix ret = new FbxMatrix(GlobalsPINVOKE.FbxMatrix_operator_Add(swigCPtr, FbxMatrix.getCPtr(pMatrix)), true);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  private FbxMatrix operator_Sub(FbxMatrix pMatrix) {
    FbxMatrix ret = new FbxMatrix(GlobalsPINVOKE.FbxMatrix_operator_Sub(swigCPtr, FbxMatrix.getCPtr(pMatrix)), true);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  private FbxMatrix operator_Mul(FbxMatrix pMatrix) {
    FbxMatrix ret = new FbxMatrix(GlobalsPINVOKE.FbxMatrix_operator_Mul(swigCPtr, FbxMatrix.getCPtr(pMatrix)), true);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  private bool _equals(FbxMatrix pM) {
    bool ret = GlobalsPINVOKE.FbxMatrix__equals(swigCPtr, FbxMatrix.getCPtr(pM));
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxMatrix Inverse() {
    FbxMatrix ret = new FbxMatrix(GlobalsPINVOKE.FbxMatrix_Inverse(swigCPtr), true);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxMatrix Transpose() {
    FbxMatrix ret = new FbxMatrix(GlobalsPINVOKE.FbxMatrix_Transpose(swigCPtr), true);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetIdentity() {
    GlobalsPINVOKE.FbxMatrix_SetIdentity(swigCPtr);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetLookToLH(FbxVector4 pEyePosition, FbxVector4 pEyeDirection, FbxVector4 pUpDirection) {
    GlobalsPINVOKE.FbxMatrix_SetLookToLH(swigCPtr, pEyePosition, pEyeDirection, pUpDirection);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetLookToRH(FbxVector4 pEyePosition, FbxVector4 pEyeDirection, FbxVector4 pUpDirection) {
    GlobalsPINVOKE.FbxMatrix_SetLookToRH(swigCPtr, pEyePosition, pEyeDirection, pUpDirection);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetLookAtLH(FbxVector4 pEyePosition, FbxVector4 pLookAt, FbxVector4 pUpDirection) {
    GlobalsPINVOKE.FbxMatrix_SetLookAtLH(swigCPtr, pEyePosition, pLookAt, pUpDirection);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetLookAtRH(FbxVector4 pEyePosition, FbxVector4 pLookAt, FbxVector4 pUpDirection) {
    GlobalsPINVOKE.FbxMatrix_SetLookAtRH(swigCPtr, pEyePosition, pLookAt, pUpDirection);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxVector4 MultNormalize(FbxVector4 pVector) {
    var ret = GlobalsPINVOKE.FbxMatrix_MultNormalize(swigCPtr, pVector);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public static FbxMatrix operator - (FbxMatrix a) {
    return a.operator_Negate();
  }

  public static FbxMatrix operator + (FbxMatrix a, FbxMatrix b) {
    return a.operator_Add(b);
  }

  public static FbxMatrix operator - (FbxMatrix a, FbxMatrix b) {
    return a.operator_Sub(b);
  }

  public static FbxMatrix operator * (FbxMatrix a, FbxMatrix b) {
    return a.operator_Mul(b);
  }

  public bool Equals(FbxMatrix other) {
    if (object.ReferenceEquals(other, null)) { return false; }
    return _equals(other);
  }

  public override bool Equals(object obj){
    if (object.ReferenceEquals(obj, null)) { return false; }
    /* is obj a subclass of this type; if so use our Equals */
    var typed = obj as FbxMatrix;
    if (!object.ReferenceEquals(typed, null)) {
      return this.Equals(typed);
    }
    /* are we a subclass of the other type; if so use their Equals */
    if (typeof(FbxMatrix).IsSubclassOf(obj.GetType())) {
      return obj.Equals(this);
    }
    /* types are unrelated; can't be a match */
    return false;
  }

  public static bool operator == (FbxMatrix a, FbxMatrix b) {
    if (object.ReferenceEquals(a, b)) { return true; }
    if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) { return false; }
    return a.Equals(b);
  }

  public static bool operator != (FbxMatrix a, FbxMatrix b) {
    return !(a == b);
  }

  public override int GetHashCode() {
    int ret = GlobalsPINVOKE.FbxMatrix_GetHashCode(swigCPtr);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

}

}