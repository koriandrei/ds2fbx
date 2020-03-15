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

public class FbxPropertyEInheritType : FbxProperty {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;

  internal FbxPropertyEInheritType(global::System.IntPtr cPtr, bool cMemoryOwn) : base(NativeMethods.FbxPropertyEInheritType_SWIGUpcast(cPtr), cMemoryOwn) {
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(FbxPropertyEInheritType obj) {
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

  public FbxPropertyEInheritType Set(FbxTransform.EInheritType pValue) {
    FbxPropertyEInheritType ret = new FbxPropertyEInheritType(NativeMethods.FbxPropertyEInheritType_Set(swigCPtr, (int)pValue), false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxTransform.EInheritType Get() {
    FbxTransform.EInheritType ret = (FbxTransform.EInheritType)NativeMethods.FbxPropertyEInheritType_Get(swigCPtr);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxTransform.EInheritType EvaluateValue(FbxTime pTime, bool pForceEval) {
    FbxTransform.EInheritType ret = (FbxTransform.EInheritType)NativeMethods.FbxPropertyEInheritType_EvaluateValue__SWIG_0(swigCPtr, FbxTime.getCPtr(pTime), pForceEval);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxTransform.EInheritType EvaluateValue(FbxTime pTime) {
    FbxTransform.EInheritType ret = (FbxTransform.EInheritType)NativeMethods.FbxPropertyEInheritType_EvaluateValue__SWIG_1(swigCPtr, FbxTime.getCPtr(pTime));
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxTransform.EInheritType EvaluateValue() {
    FbxTransform.EInheritType ret = (FbxTransform.EInheritType)NativeMethods.FbxPropertyEInheritType_EvaluateValue__SWIG_2(swigCPtr);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

}

}