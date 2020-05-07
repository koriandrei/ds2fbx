//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 4.0.1
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace Autodesk.Fbx {

public class FbxSemanticEntryView : FbxEntryView {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;

  internal FbxSemanticEntryView(global::System.IntPtr cPtr, bool cMemoryOwn) : base(NativeMethods.FbxSemanticEntryView_SWIGUpcast(cPtr), cMemoryOwn) {
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(FbxSemanticEntryView obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  protected override void Dispose(bool disposing) {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          NativeMethods.delete_FbxSemanticEntryView(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      base.Dispose(disposing);
    }
  }

  public FbxSemanticEntryView(FbxBindingTableEntry pEntry, bool pAsSource, bool pCreate) : this(NativeMethods.new_FbxSemanticEntryView__SWIG_0(FbxBindingTableEntry.getCPtr(pEntry), pAsSource, pCreate), true) {
  }

  public FbxSemanticEntryView(FbxBindingTableEntry pEntry, bool pAsSource) : this(NativeMethods.new_FbxSemanticEntryView__SWIG_1(FbxBindingTableEntry.getCPtr(pEntry), pAsSource), true) {
  }

  public void SetSemantic(string pSemantic) {
    NativeMethods.FbxSemanticEntryView_SetSemantic(swigCPtr, pSemantic);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
  }

  public string GetSemantic(bool pAppendIndex) {
    string ret = NativeMethods.FbxSemanticEntryView_GetSemantic__SWIG_0(swigCPtr, pAppendIndex);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public string GetSemantic() {
    string ret = NativeMethods.FbxSemanticEntryView_GetSemantic__SWIG_1(swigCPtr);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public int GetIndex() {
    int ret = NativeMethods.FbxSemanticEntryView_GetIndex(swigCPtr);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

}

}