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

public class FbxNode : FbxObject {
  internal FbxNode(global::System.IntPtr cPtr, bool ignored) : base(cPtr, ignored) { }

  // override void Dispose() {base.Dispose();}

  public new static FbxNode Create(FbxManager pManager, string pName) {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_Create__SWIG_0(FbxManager.getCPtr(pManager), pName);
    FbxNode ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxNode(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public new static FbxNode Create(FbxObject pContainer, string pName) {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_Create__SWIG_1(FbxObject.getCPtr(pContainer), pName);
    FbxNode ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxNode(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxNode GetParent() {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_GetParent(swigCPtr);
    FbxNode ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxNode(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public bool AddChild(FbxNode pNode) {
    bool ret = GlobalsPINVOKE.FbxNode_AddChild(swigCPtr, FbxNode.getCPtr(pNode));
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxNode RemoveChild(FbxNode pNode) {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_RemoveChild(swigCPtr, FbxNode.getCPtr(pNode));
    FbxNode ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxNode(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public int GetChildCount(bool pRecursive) {
    int ret = GlobalsPINVOKE.FbxNode_GetChildCount__SWIG_0(swigCPtr, pRecursive);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public int GetChildCount() {
    int ret = GlobalsPINVOKE.FbxNode_GetChildCount__SWIG_1(swigCPtr);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxNode GetChild(int pIndex) {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_GetChild(swigCPtr, pIndex);
    FbxNode ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxNode(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxNode FindChild(string pName, bool pRecursive, bool pInitial) {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_FindChild__SWIG_0(swigCPtr, pName, pRecursive, pInitial);
    FbxNode ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxNode(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxNode FindChild(string pName, bool pRecursive) {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_FindChild__SWIG_1(swigCPtr, pName, pRecursive);
    FbxNode ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxNode(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxNode FindChild(string pName) {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_FindChild__SWIG_2(swigCPtr, pName);
    FbxNode ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxNode(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetVisibility(bool pIsVisible) {
    GlobalsPINVOKE.FbxNode_SetVisibility(swigCPtr, pIsVisible);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public bool GetVisibility() {
    bool ret = GlobalsPINVOKE.FbxNode_GetVisibility(swigCPtr);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetShadingMode(FbxNode.EShadingMode pShadingMode) {
    GlobalsPINVOKE.FbxNode_SetShadingMode(swigCPtr, (int)pShadingMode);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxNode.EShadingMode GetShadingMode() {
    FbxNode.EShadingMode ret = (FbxNode.EShadingMode)GlobalsPINVOKE.FbxNode_GetShadingMode(swigCPtr);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxNodeAttribute SetNodeAttribute(FbxNodeAttribute pNodeAttribute) {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_SetNodeAttribute(swigCPtr, FbxNodeAttribute.getCPtr(pNodeAttribute));
    FbxNodeAttribute ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxNodeAttribute(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxNodeAttribute GetNodeAttribute() {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_GetNodeAttribute(swigCPtr);
    FbxNodeAttribute ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxNodeAttribute(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxSkeleton GetSkeleton() {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_GetSkeleton(swigCPtr);
    FbxSkeleton ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxSkeleton(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxGeometry GetGeometry() {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_GetGeometry(swigCPtr);
    FbxGeometry ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxGeometry(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxMesh GetMesh() {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_GetMesh(swigCPtr);
    FbxMesh ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxMesh(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxCamera GetCamera() {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_GetCamera(swigCPtr);
    FbxCamera ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxCamera(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxLight GetLight() {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_GetLight(swigCPtr);
    FbxLight ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxLight(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetTransformationInheritType(FbxTransform.EInheritType pInheritType) {
    GlobalsPINVOKE.FbxNode_SetTransformationInheritType(swigCPtr, (int)pInheritType);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetPivotState(FbxNode.EPivotSet pPivotSet, FbxNode.EPivotState pPivotState) {
    GlobalsPINVOKE.FbxNode_SetPivotState(swigCPtr, (int)pPivotSet, (int)pPivotState);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetRotationOrder(FbxNode.EPivotSet pPivotSet, FbxEuler.EOrder pRotationOrder) {
    GlobalsPINVOKE.FbxNode_SetRotationOrder(swigCPtr, (int)pPivotSet, (int)pRotationOrder);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void GetRotationOrder(FbxNode.EPivotSet pPivotSet, out int pRotationOrder) {
    GlobalsPINVOKE.FbxNode_GetRotationOrder(swigCPtr, (int)pPivotSet, out pRotationOrder);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetRotationActive(bool pVal) {
    GlobalsPINVOKE.FbxNode_SetRotationActive(swigCPtr, pVal);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public bool GetRotationActive() {
    bool ret = GlobalsPINVOKE.FbxNode_GetRotationActive(swigCPtr);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetRotationOffset(FbxNode.EPivotSet pPivotSet, FbxVector4 pVector) {
    GlobalsPINVOKE.FbxNode_SetRotationOffset(swigCPtr, (int)pPivotSet, pVector);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxVector4 GetRotationOffset(FbxNode.EPivotSet pPivotSet) {
    var ret = GlobalsPINVOKE.FbxNode_GetRotationOffset(swigCPtr, (int)pPivotSet);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetRotationPivot(FbxNode.EPivotSet pPivotSet, FbxVector4 pVector) {
    GlobalsPINVOKE.FbxNode_SetRotationPivot(swigCPtr, (int)pPivotSet, pVector);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxVector4 GetRotationPivot(FbxNode.EPivotSet pPivotSet) {
    var ret = GlobalsPINVOKE.FbxNode_GetRotationPivot(swigCPtr, (int)pPivotSet);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetPreRotation(FbxNode.EPivotSet pPivotSet, FbxVector4 pVector) {
    GlobalsPINVOKE.FbxNode_SetPreRotation(swigCPtr, (int)pPivotSet, pVector);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxVector4 GetPreRotation(FbxNode.EPivotSet pPivotSet) {
    var ret = GlobalsPINVOKE.FbxNode_GetPreRotation(swigCPtr, (int)pPivotSet);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetPostRotation(FbxNode.EPivotSet pPivotSet, FbxVector4 pVector) {
    GlobalsPINVOKE.FbxNode_SetPostRotation(swigCPtr, (int)pPivotSet, pVector);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxVector4 GetPostRotation(FbxNode.EPivotSet pPivotSet) {
    var ret = GlobalsPINVOKE.FbxNode_GetPostRotation(swigCPtr, (int)pPivotSet);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetScalingOffset(FbxNode.EPivotSet pPivotSet, FbxVector4 pVector) {
    GlobalsPINVOKE.FbxNode_SetScalingOffset(swigCPtr, (int)pPivotSet, pVector);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxVector4 GetScalingOffset(FbxNode.EPivotSet pPivotSet) {
    var ret = GlobalsPINVOKE.FbxNode_GetScalingOffset(swigCPtr, (int)pPivotSet);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetScalingPivot(FbxNode.EPivotSet pPivotSet, FbxVector4 pVector) {
    GlobalsPINVOKE.FbxNode_SetScalingPivot(swigCPtr, (int)pPivotSet, pVector);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
  }

  public FbxVector4 GetScalingPivot(FbxNode.EPivotSet pPivotSet) {
    var ret = GlobalsPINVOKE.FbxNode_GetScalingPivot(swigCPtr, (int)pPivotSet);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxAMatrix EvaluateGlobalTransform(FbxTime pTime, FbxNode.EPivotSet pPivotSet, bool pApplyTarget, bool pForceEval) {
    FbxAMatrix ret = new FbxAMatrix(GlobalsPINVOKE.FbxNode_EvaluateGlobalTransform__SWIG_0(swigCPtr, FbxTime.getCPtr(pTime), (int)pPivotSet, pApplyTarget, pForceEval), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxAMatrix EvaluateGlobalTransform(FbxTime pTime, FbxNode.EPivotSet pPivotSet, bool pApplyTarget) {
    FbxAMatrix ret = new FbxAMatrix(GlobalsPINVOKE.FbxNode_EvaluateGlobalTransform__SWIG_1(swigCPtr, FbxTime.getCPtr(pTime), (int)pPivotSet, pApplyTarget), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxAMatrix EvaluateGlobalTransform(FbxTime pTime, FbxNode.EPivotSet pPivotSet) {
    FbxAMatrix ret = new FbxAMatrix(GlobalsPINVOKE.FbxNode_EvaluateGlobalTransform__SWIG_2(swigCPtr, FbxTime.getCPtr(pTime), (int)pPivotSet), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxAMatrix EvaluateGlobalTransform(FbxTime pTime) {
    FbxAMatrix ret = new FbxAMatrix(GlobalsPINVOKE.FbxNode_EvaluateGlobalTransform__SWIG_3(swigCPtr, FbxTime.getCPtr(pTime)), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxAMatrix EvaluateGlobalTransform() {
    FbxAMatrix ret = new FbxAMatrix(GlobalsPINVOKE.FbxNode_EvaluateGlobalTransform__SWIG_4(swigCPtr), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxAMatrix EvaluateLocalTransform(FbxTime pTime, FbxNode.EPivotSet pPivotSet, bool pApplyTarget, bool pForceEval) {
    FbxAMatrix ret = new FbxAMatrix(GlobalsPINVOKE.FbxNode_EvaluateLocalTransform__SWIG_0(swigCPtr, FbxTime.getCPtr(pTime), (int)pPivotSet, pApplyTarget, pForceEval), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxAMatrix EvaluateLocalTransform(FbxTime pTime, FbxNode.EPivotSet pPivotSet, bool pApplyTarget) {
    FbxAMatrix ret = new FbxAMatrix(GlobalsPINVOKE.FbxNode_EvaluateLocalTransform__SWIG_1(swigCPtr, FbxTime.getCPtr(pTime), (int)pPivotSet, pApplyTarget), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxAMatrix EvaluateLocalTransform(FbxTime pTime, FbxNode.EPivotSet pPivotSet) {
    FbxAMatrix ret = new FbxAMatrix(GlobalsPINVOKE.FbxNode_EvaluateLocalTransform__SWIG_2(swigCPtr, FbxTime.getCPtr(pTime), (int)pPivotSet), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxAMatrix EvaluateLocalTransform(FbxTime pTime) {
    FbxAMatrix ret = new FbxAMatrix(GlobalsPINVOKE.FbxNode_EvaluateLocalTransform__SWIG_3(swigCPtr, FbxTime.getCPtr(pTime)), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxAMatrix EvaluateLocalTransform() {
    FbxAMatrix ret = new FbxAMatrix(GlobalsPINVOKE.FbxNode_EvaluateLocalTransform__SWIG_4(swigCPtr), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public int AddMaterial(FbxSurfaceMaterial pMaterial) {
    int ret = GlobalsPINVOKE.FbxNode_AddMaterial(swigCPtr, FbxSurfaceMaterial.getCPtr(pMaterial));
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxSurfaceMaterial GetMaterial(int pIndex) {
    global::System.IntPtr cPtr = GlobalsPINVOKE.FbxNode_GetMaterial(swigCPtr, pIndex);
    FbxSurfaceMaterial ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxSurfaceMaterial(cPtr, false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public int GetMaterialIndex(string pName) {
    int ret = GlobalsPINVOKE.FbxNode_GetMaterialIndex(swigCPtr, pName);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxPropertyDouble3 LclTranslation {
    get {
      FbxPropertyDouble3 ret = new FbxPropertyDouble3(GlobalsPINVOKE.FbxNode_LclTranslation_get(swigCPtr), false);
      if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public FbxPropertyDouble3 LclRotation {
    get {
      FbxPropertyDouble3 ret = new FbxPropertyDouble3(GlobalsPINVOKE.FbxNode_LclRotation_get(swigCPtr), false);
      if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public FbxPropertyDouble3 LclScaling {
    get {
      FbxPropertyDouble3 ret = new FbxPropertyDouble3(GlobalsPINVOKE.FbxNode_LclScaling_get(swigCPtr), false);
      if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public FbxPropertyBool VisibilityInheritance {
    get {
      FbxPropertyBool ret = new FbxPropertyBool(GlobalsPINVOKE.FbxNode_VisibilityInheritance_get(swigCPtr), false);
      if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public FbxPropertyEInheritType InheritType {
    get {
      FbxPropertyEInheritType ret = new FbxPropertyEInheritType(GlobalsPINVOKE.FbxNode_InheritType_get(swigCPtr), false);
      if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public override int GetHashCode(){
      return swigCPtr.Handle.GetHashCode();
  }

  public bool Equals(FbxNode other) {
    if (object.ReferenceEquals(other, null)) { return false; }
    return this.swigCPtr.Handle.Equals (other.swigCPtr.Handle);
  }

  public override bool Equals(object obj){
    if (object.ReferenceEquals(obj, null)) { return false; }
    /* is obj a subclass of this type; if so use our Equals */
    var typed = obj as FbxNode;
    if (!object.ReferenceEquals(typed, null)) {
      return this.Equals(typed);
    }
    /* are we a subclass of the other type; if so use their Equals */
    if (typeof(FbxNode).IsSubclassOf(obj.GetType())) {
      return obj.Equals(this);
    }
    /* types are unrelated; can't be a match */
    return false;
  }

  public static bool operator == (FbxNode a, FbxNode b) {
    if (object.ReferenceEquals(a, b)) { return true; }
    if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) { return false; }
    return a.Equals(b);
  }

  public static bool operator != (FbxNode a, FbxNode b) {
    return !(a == b);
  }

  public enum EShadingMode {
    eHardShading,
    eWireFrame,
    eFlatShading,
    eLightShading,
    eTextureShading,
    eFullShading
  }

  public enum EPivotSet {
    eSourcePivot,
    eDestinationPivot
  }

  public enum EPivotState {
    ePivotActive,
    ePivotReference
  }

}

}
