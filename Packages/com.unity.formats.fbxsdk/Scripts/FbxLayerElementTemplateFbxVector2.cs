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

public class FbxLayerElementTemplateFbxVector2 : FbxLayerElement {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;

  internal FbxLayerElementTemplateFbxVector2(global::System.IntPtr cPtr, bool cMemoryOwn) : base(GlobalsPINVOKE.FbxLayerElementTemplateFbxVector2_SWIGUpcast(cPtr), cMemoryOwn) {
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(FbxLayerElementTemplateFbxVector2 obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  public override void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          throw new global::System.MethodAccessException("C++ destructor does not have public access");
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
      base.Dispose();
    }
  }

  public FbxLayerElementArrayTemplateFbxVector2 GetDirectArray() {
    FbxLayerElementArrayTemplateFbxVector2 ret = new FbxLayerElementArrayTemplateFbxVector2(GlobalsPINVOKE.FbxLayerElementTemplateFbxVector2_GetDirectArray(swigCPtr), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxLayerElementArrayTemplateInt GetIndexArray() {
    FbxLayerElementArrayTemplateInt ret = new FbxLayerElementArrayTemplateInt(GlobalsPINVOKE.FbxLayerElementTemplateFbxVector2_GetIndexArray(swigCPtr), false);
    if (GlobalsPINVOKE.SWIGPendingException.Pending) throw GlobalsPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

}

}