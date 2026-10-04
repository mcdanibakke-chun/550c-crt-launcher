// Compiled only into the isolated G2_TEST harness, never the released launcher.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
internal sealed class TestDesktopBridge : IDesktopBridge {
    readonly string mode;
    readonly Stopwatch clock=Stopwatch.StartNew();
    Form target;
    bool requested,vanished;
    int registrationChecks;
    TestDesktopBridge(string name){mode=name;}
    internal static IDesktopBridge Create(string[] args,IDesktopBridge ignored){string mode="fresh";for(int i=0;i<args.Length-1;i++)if(args[i]=="--test-mode")mode=args[i+1];return new TestDesktopBridge(mode);}
    public bool Registered(){return mode!="missing"&&mode!="close"&&(mode!="retry"||++registrationChecks>1);}
    public bool HasProcess(){return mode=="already"||mode=="background";}
    public int Activate(){if(mode=="activation-error")throw new InvalidOperationException("Isolated activation failure");requested=true;return Process.GetCurrentProcess().Id;}
    public ClientWindow FindWindow(){
        if(vanished)return null;
        bool due=mode=="already"||(requested&&mode!="timeout"&&clock.ElapsedMilliseconds>=(mode=="slow"?18000:mode=="skip"?3500:800));
        if(!due)return null;
        if(target==null){target=new Form{Text="G2 TEST TARGET — NOT CHATGPT",BackColor=Color.FromArgb(18,36,24),Width=900,Height=650,StartPosition=FormStartPosition.Manual,Location=new Point(-10000,-10000),ShowInTaskbar=false};target.Show();}
        return new ClientWindow{Handle=target.Handle,Pid=Process.GetCurrentProcess().Id};
    }
    public bool Valid(ClientWindow w){return !vanished&&target!=null&&!target.IsDisposed&&w!=null&&w.Handle==target.Handle;}
    public bool Focus(ClientWindow w){if(mode=="vanish"){vanished=true;target.Hide();return false;}if(!Valid(w))return false;return true;}
    public void RaiseBehindOverlay(ClientWindow w){}
    public bool ForegroundIs(ClientWindow w){return Valid(w)&&Form.ActiveForm==target;}
}
