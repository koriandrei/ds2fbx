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

public class FbxPropertyEWrapMode : FbxProperty {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;

  internal FbxPropertyEWrapMode(global::System.IntPtr cPtr, bool cMemoryOwn) : base(NativeMethods.FbxPropertyEWrapMode_SWIGUpcast(cPtr), cMemoryOwn) {
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(FbxPropertyEWrapMode obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  protected override void Dispose(bool disposing) {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          throw new global::System.MethodAccessException("C++ destructor does not have public access");
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      base.Dispose(disposing);
    }
  }

  public FbxPropertyEWrapMode Set(FbxTexture.EWrapMode pValue) {
    FbxPropertyEWrapMode ret = new FbxPropertyEWrapMode(NativeMethods.FbxPropertyEWrapMode_Set(swigCPtr, (int)pValue), false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxTexture.EWrapMode Get() {
    FbxTexture.EWrapMode ret = (FbxTexture.EWrapMode)NativeMethods.FbxPropertyEWrapMode_Get(swigCPtr);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxTexture.EWrapMode EvaluateValue(FbxTime pTime, bool pForceEval) {
    FbxTexture.EWrapMode ret = (FbxTexture.EWrapMode)NativeMethods.FbxPropertyEWrapMode_EvaluateValue__SWIG_0(swigCPtr, FbxTime.getCPtr(pTime), pForceEval);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxTexture.EWrapMode EvaluateValue(FbxTime pTime) {
    FbxTexture.EWrapMode ret = (FbxTexture.EWrapMode)NativeMethods.FbxPropertyEWrapMode_EvaluateValue__SWIG_1(swigCPtr, FbxTime.getCPtr(pTime));
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxTexture.EWrapMode EvaluateValue() {
    FbxTexture.EWrapMode ret = (FbxTexture.EWrapMode)NativeMethods.FbxPropertyEWrapMode_EvaluateValue__SWIG_2(swigCPtr);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

}

}
