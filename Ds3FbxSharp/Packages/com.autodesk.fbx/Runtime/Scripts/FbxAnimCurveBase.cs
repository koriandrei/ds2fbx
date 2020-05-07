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

public class FbxAnimCurveBase : FbxObject {
  internal FbxAnimCurveBase(global::System.IntPtr cPtr, bool ignored) : base(cPtr, ignored) { }

  // override void Dispose() {base.Dispose();}

  public virtual int KeyGetCount() {
    int ret = NativeMethods.FbxAnimCurveBase_KeyGetCount(swigCPtr);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public virtual FbxTime KeyGetTime(int arg0) {
    FbxTime ret = new FbxTime(NativeMethods.FbxAnimCurveBase_KeyGetTime(swigCPtr, arg0), true);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public override int GetHashCode(){
      return swigCPtr.Handle.GetHashCode();
  }

  public bool Equals(FbxAnimCurveBase other) {
    if (object.ReferenceEquals(other, null)) { return false; }
    return this.swigCPtr.Handle.Equals (other.swigCPtr.Handle);
  }

  public override bool Equals(object obj){
    if (object.ReferenceEquals(obj, null)) { return false; }
    /* is obj a subclass of this type; if so use our Equals */
    var typed = obj as FbxAnimCurveBase;
    if (!object.ReferenceEquals(typed, null)) {
      return this.Equals(typed);
    }
    /* are we a subclass of the other type; if so use their Equals */
    if (typeof(FbxAnimCurveBase).IsSubclassOf(obj.GetType())) {
      return obj.Equals(this);
    }
    /* types are unrelated; can't be a match */
    return false;
  }

  public static bool operator == (FbxAnimCurveBase a, FbxAnimCurveBase b) {
    if (object.ReferenceEquals(a, b)) { return true; }
    if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) { return false; }
    return a.Equals(b);
  }

  public static bool operator != (FbxAnimCurveBase a, FbxAnimCurveBase b) {
    return !(a == b);
  }

  public static new FbxAnimCurveBase Create(FbxManager pManager, string pName) {
    throw new System.NotImplementedException("FbxAnimCurveBase is abstract; create FbxAnimCurve instead");
  }
  public static new FbxAnimCurveBase Create(FbxObject pContainer, string pName) {
    throw new System.NotImplementedException("FbxAnimCurveBase is abstract; create FbxAnimCurve instead");
  }

}

}
