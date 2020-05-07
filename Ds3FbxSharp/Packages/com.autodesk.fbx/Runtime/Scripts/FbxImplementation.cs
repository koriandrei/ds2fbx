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

public class FbxImplementation : FbxObject {
  internal FbxImplementation(global::System.IntPtr cPtr, bool ignored) : base(cPtr, ignored) { }

  // override void Dispose() {base.Dispose();}

  public new static FbxImplementation Create(FbxManager pManager, string pName) {
    global::System.IntPtr cPtr = NativeMethods.FbxImplementation_Create__SWIG_0(FbxManager.getCPtr(pManager), pName);
    FbxImplementation ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxImplementation(cPtr, false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public new static FbxImplementation Create(FbxObject pContainer, string pName) {
    global::System.IntPtr cPtr = NativeMethods.FbxImplementation_Create__SWIG_1(FbxObject.getCPtr(pContainer), pName);
    FbxImplementation ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxImplementation(cPtr, false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxPropertyString Language {
    get {
      FbxPropertyString ret = new FbxPropertyString(NativeMethods.FbxImplementation_Language_get(swigCPtr), false);
      if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public FbxPropertyString LanguageVersion {
    get {
      FbxPropertyString ret = new FbxPropertyString(NativeMethods.FbxImplementation_LanguageVersion_get(swigCPtr), false);
      if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public FbxPropertyString RenderAPI {
    get {
      FbxPropertyString ret = new FbxPropertyString(NativeMethods.FbxImplementation_RenderAPI_get(swigCPtr), false);
      if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public FbxPropertyString RenderAPIVersion {
    get {
      FbxPropertyString ret = new FbxPropertyString(NativeMethods.FbxImplementation_RenderAPIVersion_get(swigCPtr), false);
      if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public FbxPropertyString RootBindingName {
    get {
      FbxPropertyString ret = new FbxPropertyString(NativeMethods.FbxImplementation_RootBindingName_get(swigCPtr), false);
      if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
      return ret;
    } 
  }

  public FbxBindingTable AddNewTable(string pTargetName, string pTargetType) {
    global::System.IntPtr cPtr = NativeMethods.FbxImplementation_AddNewTable(swigCPtr, pTargetName, pTargetType);
    FbxBindingTable ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxBindingTable(cPtr, false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public FbxBindingTable GetRootTable() {
    global::System.IntPtr cPtr = NativeMethods.FbxImplementation_GetRootTable(swigCPtr);
    FbxBindingTable ret = (cPtr == global::System.IntPtr.Zero) ? null : new FbxBindingTable(cPtr, false);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public override int GetHashCode(){
      return swigCPtr.Handle.GetHashCode();
  }

  public bool Equals(FbxImplementation other) {
    if (object.ReferenceEquals(other, null)) { return false; }
    return this.swigCPtr.Handle.Equals (other.swigCPtr.Handle);
  }

  public override bool Equals(object obj){
    if (object.ReferenceEquals(obj, null)) { return false; }
    /* is obj a subclass of this type; if so use our Equals */
    var typed = obj as FbxImplementation;
    if (!object.ReferenceEquals(typed, null)) {
      return this.Equals(typed);
    }
    /* are we a subclass of the other type; if so use their Equals */
    if (typeof(FbxImplementation).IsSubclassOf(obj.GetType())) {
      return obj.Equals(this);
    }
    /* types are unrelated; can't be a match */
    return false;
  }

  public static bool operator == (FbxImplementation a, FbxImplementation b) {
    if (object.ReferenceEquals(a, b)) { return true; }
    if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) { return false; }
    return a.Equals(b);
  }

  public static bool operator != (FbxImplementation a, FbxImplementation b) {
    return !(a == b);
  }

}

}
