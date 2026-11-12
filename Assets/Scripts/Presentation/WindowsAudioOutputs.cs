using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace EmberBastion {
 // MMDevice enumeration plus process-scoped Windows audio preferences. Never changes the system default.
 // Policy ABI references: EarTrumpet (MIT); see Docs/ASSET-CREDITS.md.
 public static class WindowsAudioOutputs {
  public sealed class Output {public string Id,Name;public Output(string id,string name){Id=id;Name=name;}}
  [StructLayout(LayoutKind.Sequential)]struct Key {public Guid fmt;public uint id;}
  [StructLayout(LayoutKind.Explicit,Size=24)]struct Value {[FieldOffset(0)]public ushort type;[FieldOffset(8)]public IntPtr text;}
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int EnumDevices(IntPtr self,int flow,uint state,out IntPtr devices);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int DefaultDevice(IntPtr self,int flow,int role,out IntPtr device);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Count(IntPtr self,out uint count);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Item(IntPtr self,uint index,out IntPtr device);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Id(IntPtr self,out IntPtr text);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Store(IntPtr self,uint access,out IntPtr store);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetValue(IntPtr self,ref Key key,out Value value);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int SetOutput(IntPtr self,uint pid,int flow,int role,IntPtr id);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetOutput(IntPtr self,uint pid,int flow,int role,out IntPtr id);
  [DllImport("ole32.dll")]static extern int CoInitializeEx(IntPtr reserved,uint mode);
  [DllImport("ole32.dll")]static extern void CoUninitialize();
  [DllImport("ole32.dll")]static extern int CoCreateInstance(ref Guid cls,IntPtr outer,uint context,ref Guid iid,out IntPtr obj);
  [DllImport("ole32.dll")]static extern int PropVariantClear(ref Value value);
  [DllImport("combase.dll",CharSet=CharSet.Unicode)]static extern int WindowsCreateString(string value,uint length,out IntPtr text);
  [DllImport("combase.dll")]static extern int WindowsDeleteString(IntPtr text);
  [DllImport("combase.dll")]static extern IntPtr WindowsGetStringRawBuffer(IntPtr text,out uint length);
  [DllImport("combase.dll")]static extern int RoGetActivationFactory(IntPtr name,ref Guid iid,out IntPtr factory);
  static T Method<T>(IntPtr self,int slot){return (T)(object)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(self),slot*IntPtr.Size),typeof(T));}
  static void Check(int hr){Marshal.ThrowExceptionForHR(hr);}
  static void Release(IntPtr ptr){if(ptr!=IntPtr.Zero)Marshal.Release(ptr);}
  static IntPtr Enumerator(){Guid cls=new Guid("bcde0395-e52f-467c-8e3d-c4579291692e"),iid=new Guid("a95664d2-9614-4f35-a746-de8db63617e6");IntPtr p;Check(CoCreateInstance(ref cls,IntPtr.Zero,1,ref iid,out p));return p;}
  static string DeviceId(IntPtr device){IntPtr text;Check(Method<Id>(device,5)(device,out text));try{return Marshal.PtrToStringUni(text);}finally{Marshal.FreeCoTaskMem(text);}}
  public static List<Output> List(){
   int init=CoInitializeEx(IntPtr.Zero,2);IntPtr enumerator=IntPtr.Zero,collection=IntPtr.Zero;var result=new List<Output>();
   try{enumerator=Enumerator();Check(Method<EnumDevices>(enumerator,3)(enumerator,0,1,out collection));uint count;Check(Method<Count>(collection,3)(collection,out count));
    for(uint i=0;i<count;i++){IntPtr device=IntPtr.Zero,store=IntPtr.Zero;try{Check(Method<Item>(collection,4)(collection,i,out device));Check(Method<Store>(device,4)(device,0,out store));Key key=new Key{fmt=new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),id=14};Value value;Check(Method<GetValue>(store,5)(store,ref key,out value));string name;try{name=value.type==31?Marshal.PtrToStringUni(value.text):"오디오 출력 장치";}finally{PropVariantClear(ref value);}result.Add(new Output(DeviceId(device),name));}finally{Release(store);Release(device);}}
    return result;
   }finally{Release(collection);Release(enumerator);if(init>=0)CoUninitialize();}
  }
  static IntPtr Policy(){IntPtr name=IntPtr.Zero,policy;string cls="Windows.Media.Internal.AudioPolicyConfig";Check(WindowsCreateString(cls,(uint)cls.Length,out name));try{Guid iid=new Guid("ab3d4648-e242-459f-b02f-541c70306324");int hr=RoGetActivationFactory(name,ref iid,out policy);if(hr<0){iid=new Guid("2a59116d-6c4f-45e0-a74f-707e3fef9258");Check(RoGetActivationFactory(name,ref iid,out policy));}return policy;}finally{WindowsDeleteString(name);}}
  const string Prefix=@"\\?\SWD#MMDEVAPI#",Suffix="#{e6327cad-dcec-4949-ae8a-991e976a79d2}";
  static string Read(IntPtr policy,int role){IntPtr id;int hr=Method<GetOutput>(policy,26)(policy,(uint)Process.GetCurrentProcess().Id,0,role,out id);if(hr==unchecked((int)0x80070057)||hr==unchecked((int)0x80070490))return "";Check(hr);try{if(id==IntPtr.Zero)return "";uint n;string s=Marshal.PtrToStringUni(WindowsGetStringRawBuffer(id,out n),(int)n)??"";if(s.StartsWith(Prefix,StringComparison.OrdinalIgnoreCase))s=s.Substring(Prefix.Length);if(s.EndsWith(Suffix,StringComparison.OrdinalIgnoreCase))s=s.Substring(0,s.Length-Suffix.Length);return s;}finally{if(id!=IntPtr.Zero)WindowsDeleteString(id);}}
  static void Write(IntPtr policy,string id,int role){IntPtr value=IntPtr.Zero;try{if(!string.IsNullOrEmpty(id)){string full=Prefix+id+Suffix;Check(WindowsCreateString(full,(uint)full.Length,out value));}int hr=Method<SetOutput>(policy,25)(policy,(uint)Process.GetCurrentProcess().Id,0,role,value);if(hr<0)throw new InvalidOperationException("Audio routing 0x"+hr.ToString("X8")+" role "+role+" device "+id);}finally{if(value!=IntPtr.Zero)WindowsDeleteString(value);}}
  public static string Current(){int init=CoInitializeEx(IntPtr.Zero,2);IntPtr policy=IntPtr.Zero;try{policy=Policy();return Read(policy,1);}finally{Release(policy);if(init>=0)CoUninitialize();}}
  public static void Select(string id){id=id??"";if(id.Length>0&&!List().Exists(x=>x.Id==id))throw new InvalidOperationException("장치가 연결되어 있지 않습니다.");int init=CoInitializeEx(IntPtr.Zero,2);IntPtr policy=IntPtr.Zero;try{policy=Policy();string old0=Read(policy,0),old1=Read(policy,1);try{Write(policy,id,0);Write(policy,id,1);if(Read(policy,0)!=id||Read(policy,1)!=id)throw new InvalidOperationException("출력 장치 적용을 확인하지 못했습니다.");}catch{Write(policy,old0,0);Write(policy,old1,1);throw;}}finally{Release(policy);if(init>=0)CoUninitialize();}}
 }
}
