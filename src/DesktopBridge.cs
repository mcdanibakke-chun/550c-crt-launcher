using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal sealed class ClientWindow {
    internal IntPtr Handle;
    internal int Pid;
}
internal interface IDesktopBridge {
    bool Registered();
    bool HasProcess();
    ClientWindow FindWindow();
    int Activate();
    bool Valid(ClientWindow window);
    bool Focus(ClientWindow window);
    void RaiseBehindOverlay(ClientWindow window);
    bool ForegroundIs(ClientWindow window);
}
internal sealed class DesktopBridge : IDesktopBridge {
    readonly string appId,processName,family;
    internal DesktopBridge(string id,string name){appId=id;processName=name;family=id.Split('!')[0];}
    delegate bool EnumCallback(IntPtr window,IntPtr parameter);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumCallback callback,IntPtr parameter);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window,uint command);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr window,int command);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr window,uint attribute,out int value,int size);
    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern int GetApplicationUserModelId(IntPtr process,ref uint length,StringBuilder buffer);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern int GetPackageFamilyName(IntPtr process,ref uint length,StringBuilder buffer);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr process,int flags,StringBuilder buffer,ref int length);
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern int SHParseDisplayName(string name,IntPtr context,out IntPtr item,uint mask,out uint attributes);
    [StructLayout(LayoutKind.Sequential)] struct Rect{internal int Left,Top,Right,Bottom;}
    [ComImport,Guid("2e941141-7f97-4756-ba1d-9decde894a3d"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IActivationManager {
        [PreserveSig] int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)]string appId,[MarshalAs(UnmanagedType.LPWStr)]string arguments,uint options,out uint pid);
        [PreserveSig] int ActivateForFile(string appId,IntPtr items,string verb,out uint pid);
        [PreserveSig] int ActivateForProtocol(string appId,IntPtr items,out uint pid);
    }
    public bool Registered(){IntPtr item=IntPtr.Zero;uint attributes;try{return SHParseDisplayName("shell:AppsFolder\\"+appId,IntPtr.Zero,out item,0,out attributes)>=0&&item!=IntPtr.Zero;}finally{if(item!=IntPtr.Zero)Marshal.FreeCoTaskMem(item);}}
    internal bool VerifiedPid(int pid){
        var handle=OpenProcess(0x1000,false,pid);if(handle==IntPtr.Zero)return false;
        try{
            uint length=0;GetPackageFamilyName(handle,ref length,null);if(length==0)return false;
            var package=new StringBuilder((int)length);if(GetPackageFamilyName(handle,ref length,package)!=0||package.ToString()!=family)return false;
            length=0;GetApplicationUserModelId(handle,ref length,null);
            if(length>0){var identity=new StringBuilder((int)length);if(GetApplicationUserModelId(handle,ref length,identity)==0&&identity.ToString()!=appId)return false;}
            int size=32768;var path=new StringBuilder(size);
            return QueryFullProcessImageName(handle,0,path,ref size)&&String.Equals(Path.GetFileNameWithoutExtension(path.ToString()),processName,StringComparison.OrdinalIgnoreCase);
        }catch{return false;}finally{CloseHandle(handle);}
    }
    public bool HasProcess(){foreach(var process in Process.GetProcessesByName(processName)){using(process){if(VerifiedPid(process.Id))return true;}}return false;}
    public ClientWindow FindWindow(){
        ClientWindow found=null;
        EnumWindows((window,p)=>{
            if(!IsWindowVisible(window)||GetWindow(window,4)!=IntPtr.Zero)return true;
            int cloaked;if(DwmGetWindowAttribute(window,14,out cloaked,4)==0&&cloaked!=0)return true;
            Rect rect;if(!IsIconic(window)&&(!GetWindowRect(window,out rect)||rect.Right-rect.Left<200||rect.Bottom-rect.Top<120))return true;
            uint pid;GetWindowThreadProcessId(window,out pid);if(!VerifiedPid((int)pid))return true;
            found=new ClientWindow{Handle=window,Pid=(int)pid};return false;
        },IntPtr.Zero);return found;
    }
    public bool Valid(ClientWindow window){if(window==null||!IsWindowVisible(window.Handle))return false;uint pid;GetWindowThreadProcessId(window.Handle,out pid);int cloaked;return pid==window.Pid&&VerifiedPid((int)pid)&&!(DwmGetWindowAttribute(window.Handle,14,out cloaked,4)==0&&cloaked!=0);}
    public int Activate(){
        var instance=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")));
        try{uint pid;var hr=((IActivationManager)instance).ActivateApplication(appId,null,2,out pid);Marshal.ThrowExceptionForHR(hr);return (int)pid;}finally{Marshal.FinalReleaseComObject(instance);}
    }
    public void RaiseBehindOverlay(ClientWindow window){if(!Valid(window))return;if(IsIconic(window.Handle))ShowWindowAsync(window.Handle,9);SetWindowPos(window.Handle,IntPtr.Zero,0,0,0,0,0x0013);}
    public bool Focus(ClientWindow window){if(!Valid(window))return false;RaiseBehindOverlay(window);return SetForegroundWindow(window.Handle);}
    public bool ForegroundIs(ClientWindow window){return window!=null&&GetForegroundWindow()==window.Handle;}
}
