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

public class FbxIOBase : FbxObject {
  internal FbxIOBase(global::System.IntPtr cPtr, bool ignored) : base(cPtr, ignored) { }

  // override void Dispose() {base.Dispose();}

  public new static FbxIOBase Create(FbxManager pManager, string pName) {
    global::System.IntPtr cPtr = NativeMethods.FbxIOBase_Create__SWIG_0(FbxManager.getCPtr(pManager), pName);
    FbxIOBase ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxIOBase(cPtr, false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public new static FbxIOBase Create(FbxObject pContainer, string pName) {
    global::System.IntPtr cPtr = NativeMethods.FbxIOBase_Create__SWIG_1(FbxObject.getCPtr(pContainer), pName);
    FbxIOBase ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxIOBase(cPtr, false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public virtual bool Initialize(string pFileName, int pFileFormat, FbxIOSettings pIOSettings) {
    bool ret = NativeMethods.FbxIOBase_Initialize__SWIG_0(swigCPtr, pFileName, pFileFormat, FbxIOSettings.getCPtr(pIOSettings));
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public virtual bool Initialize(string pFileName, int pFileFormat) {
    bool ret = NativeMethods.FbxIOBase_Initialize__SWIG_1(swigCPtr, pFileName, pFileFormat);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public virtual bool Initialize(string pFileName) {
    bool ret = NativeMethods.FbxIOBase_Initialize__SWIG_2(swigCPtr, pFileName);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public virtual string GetFileName() {
    string ret = NativeMethods.FbxIOBase_GetFileName(swigCPtr);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxStatus GetStatus() {
    FbxStatus ret = new FbxStatus(NativeMethods.FbxIOBase_GetStatus(swigCPtr), false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public override int GetHashCode(){
      return swigCPtr.Handle.GetHashCode();
  }

  public bool Equals(FbxIOBase other) {
    if (object.ReferenceEquals(other, null)) { return false; }
    return this.swigCPtr.Handle.Equals (other.swigCPtr.Handle);
  }

  public override bool Equals(object obj){
    if (object.ReferenceEquals(obj, null)) { return false; }
    /* is obj a subclass of this type; if so use our Equals */
    var typed = obj as FbxIOBase;
    if (!object.ReferenceEquals(typed, null)) {
      return this.Equals(typed);
    }
    /* are we a subclass of the other type; if so use their Equals */
    if (typeof(FbxIOBase).IsSubclassOf(obj.GetType())) {
      return obj.Equals(this);
    }
    /* types are unrelated; can't be a match */
    return false;
  }

  public static bool operator == (FbxIOBase a, FbxIOBase b) {
    if (object.ReferenceEquals(a, b)) { return true; }
    if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) { return false; }
    return a.Equals(b);
  }

  public static bool operator != (FbxIOBase a, FbxIOBase b) {
    return !(a == b);
  }

}

}
