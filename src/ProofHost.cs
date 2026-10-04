using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

// G1 ONLY. No ChatGPT discovery, launch, focus, injection or integration code.
internal static class Program {
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [STAThread] static int Main(string[] args) {
        try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { }
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try { Application.Run(new ProofWindow(args)); return 0; }
        catch(Exception e) { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"proof-error.log"),e.ToString()); return 1; }
    }
}
internal sealed class ProofWindow : Form {
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr window);
    readonly JavaScriptSerializer json = new JavaScriptSerializer();
    readonly Stopwatch elapsed = Stopwatch.StartNew();
    readonly Dictionary<string,object> config;
    readonly Dictionary<string,object> text;
    readonly string root, logPath, cachePath;
    readonly string[] arguments;
    readonly Timer watchdog = new Timer();
    readonly Timer nativeIntro = new Timer();
    readonly Timer audioRamp = new Timer();
    System.Windows.Media.MediaPlayer music;
    double musicGain, musicFadeInMs, musicFadeStart;
    bool musicOpened, musicStarted, musicPositionLogged;
    double timelineStart;
    WebView2 web;
    CoreWebView2Environment environment;
    bool closing, ready, nativeCaptured, ownNavigationPending;
    TaskCompletionSource<bool> browserExited = new TaskCompletionSource<bool>();
    int browserPid;
    int fadeMs,skipFadeMs,duration;
    bool qa;
    string selfTest;
    double captureAt=-1;
    string captureFile;
    int exportFps;
    string exportDir;
    internal ProofWindow(string[] args) {
        arguments=args; root=AppDomain.CurrentDomain.BaseDirectory;
        var configPath=Path.Combine(root,"config.json");
        if(!File.Exists(configPath))throw new FileNotFoundException("Keep config.json next to the proof executable.");
        var raw=File.ReadAllText(configPath,Encoding.UTF8).TrimStart('\uFEFF');
        config=json.Deserialize<Dictionary<string,object>>(raw);text=(Dictionary<string,object>)config["text"];
        duration=Number("durationMs",4000,120000);fadeMs=Number("fadeMs",300,600);skipFadeMs=Number("skipFadeMs",50,300);
        // 'playWhenAlreadyRunning' is future policy only; G1 does not inspect any application.
        Directory.CreateDirectory(Path.Combine(root,"evidence"));
        var run=DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Process.GetCurrentProcess().Id;
        logPath=Path.Combine(root,"evidence","run-"+run+".jsonl");
        cachePath=Path.Combine(root,"runtime","proof-"+run);
        for(int i=0;i<args.Length;i++) {
            if(args[i]=="--qa")qa=true;
            if(args[i]=="--self-test"&&i+1<args.Length){selfTest=args[++i];if(selfTest!="buttons"&&selfTest!="esc"&&selfTest!="click")throw new ArgumentException("Unknown local test");}
            if(args[i]=="--snapshot"&&i+2<args.Length){captureAt=double.Parse(args[++i],System.Globalization.CultureInfo.InvariantCulture);captureFile=Path.GetFullPath(args[++i]);}
            if(args[i]=="--export-frames"&&i+2<args.Length){exportFps=int.Parse(args[++i]);exportDir=Path.GetFullPath(args[++i]);if(exportFps<1||exportFps>60)throw new ArgumentException("Export FPS 1..60");}
        }
        if(captureAt>=0||exportFps>0)duration=Math.Max(duration,16000);
        Text=Path.GetFileNameWithoutExtension(Application.ExecutablePath)+" — standalone visual proof";FormBorderStyle=FormBorderStyle.None;
        var palette=(Dictionary<string,object>)config["colors"];
        BackColor=ColorTranslator.FromHtml(Convert.ToString(palette["background"]));ForeColor=ColorTranslator.FromHtml(Convert.ToString(palette["text"]));ShowInTaskbar=false;KeyPreview=true;
        StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.None;
        var window=(Dictionary<string,object>)config["window"];
        TopMost=(bool)window["topMost"];
        var target=(string)window["monitor"]=="cursor"?Screen.FromPoint(Cursor.Position):Screen.PrimaryScreen;
        Bounds=target.Bounds;
        if((string)window["style"]=="centered") {
            Size=new Size(Math.Min(1280,target.WorkingArea.Width),Math.Min(800,target.WorkingArea.Height));
            Location=new Point(target.WorkingArea.Left+(target.WorkingArea.Width-Width)/2,target.WorkingArea.Top+(target.WorkingArea.Height-Height)/2);
        }
        if(exportFps>0){TopMost=false;Size=new Size(1280,720);Location=new Point(-10000,-10000);}
        if(selfTest!=null||Array.IndexOf(args,"--offscreen")>=0){TopMost=false;Size=new Size(1280,720);Location=new Point(-10000,-10000);}
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint,true);
        MouseDown+=(s,e)=>Skip("native-click");
        nativeIntro.Interval=16;nativeIntro.Tick+=(s,e)=>{if(!ready&&!closing)Invalidate();else nativeIntro.Stop();};
        Shown+=async(s,e)=>{timelineStart=elapsed.Elapsed.TotalMilliseconds;nativeIntro.Start();await Initialize();};
        watchdog.Interval=250;watchdog.Tick+=(s,e)=>{if(elapsed.ElapsedMilliseconds>duration+30000&&!closing){Log("watchdog",null);BeginClose(skipFadeMs);}};
        FormClosing+=(s,e)=>{if(!closing){e.Cancel=true;BeginClose(skipFadeMs);}};
        Log("created",new { pid=Process.GetCurrentProcess().Id,durationMs=duration,bounds=new {Bounds.X,Bounds.Y,Bounds.Width,Bounds.Height},g1Only=true });
    }
    int Number(string key,int min,int max){int value=Convert.ToInt32(config[key]);if(value<min||value>max)throw new ArgumentException(key+" outside allowed bounds");return value;}
    string Str(string key){return Convert.ToString(text[key]);}
    void Log(string name,object details){File.AppendAllText(logPath,json.Serialize(new {eventName=name,hostMs=elapsed.Elapsed.TotalMilliseconds,details=details})+Environment.NewLine,Encoding.UTF8);}
    void StartMusic() {
        if(closing)return;
        if(captureAt>=0||exportFps>0){Log("audio-disabled",new {reason="visual-export"});return;}
        object value;
        if(!config.TryGetValue("audio",out value)){Log("audio-disabled",new {reason="no-config"});return;}
        var settings=(Dictionary<string,object>)value;
        if(!Convert.ToBoolean(settings["enabled"])){Log("audio-disabled",new {reason="config"});return;}
        try {
            var relative=Convert.ToString(settings["file"]);
            var localRoot=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            var file=Path.GetFullPath(Path.Combine(root,relative));
            if(Path.IsPathRooted(relative)||!file.StartsWith(localRoot,StringComparison.OrdinalIgnoreCase)||!File.Exists(file))throw new ArgumentException("Audio must be an existing local file inside this proof folder.");
            musicGain=Convert.ToDouble(settings["volume"]);musicFadeInMs=Convert.ToDouble(settings["fadeInMs"]);
            if(musicGain<0||musicGain>1||musicFadeInMs<0||musicFadeInMs>2000)throw new ArgumentException("Invalid audio gain/fade configuration.");
            music=new System.Windows.Media.MediaPlayer();music.Volume=0;
            music.MediaOpened+=(s,e)=>{
                if(closing){StopMusic();return;}
                musicOpened=true;PlayMusic();
            };
            music.MediaFailed+=(s,e)=>{Log("audio-error",new {message=e.ErrorException.Message});StopMusic();};
            music.MediaEnded+=(s,e)=>{Log("audio-ended",null);StopMusic();};
            audioRamp.Interval=20;audioRamp.Tick+=(s,e)=>{
                if(music==null||closing){audioRamp.Stop();return;}
                if(!musicStarted)return;
                var time=elapsed.Elapsed.TotalMilliseconds-timelineStart;
                music.Volume=musicGain*(musicFadeInMs==0?1:Math.Min(1,(elapsed.Elapsed.TotalMilliseconds-musicFadeStart)/musicFadeInMs));
                if(!musicPositionLogged&&time>=2000){musicPositionLogged=true;Log("audio-position",new {positionSeconds=music.Position.TotalSeconds,gain=music.Volume});}
            };
            music.Open(new Uri(file,UriKind.Absolute));audioRamp.Start();
            Log("audio-requested",new {file=relative,gain=musicGain,fadeInMs=musicFadeInMs});
        }catch(Exception e){Log("audio-error",new {message=e.Message});StopMusic();}
    }
    void PlayMusic() {
        if(music==null||!musicOpened||musicStarted||closing||timelineStart<=0)return;
        var offset=Math.Max(0,(elapsed.Elapsed.TotalMilliseconds-timelineStart)/1000);
        if(music.NaturalDuration.HasTimeSpan&&offset>=music.NaturalDuration.TimeSpan.TotalSeconds){Log("audio-too-late",new {offsetSeconds=offset});StopMusic();return;}
        music.Position=TimeSpan.FromSeconds(offset);musicFadeStart=elapsed.Elapsed.TotalMilliseconds;musicStarted=true;music.Play();
        Log("audio-opened",new {offsetSeconds=offset,durationSeconds=music.NaturalDuration.HasTimeSpan?music.NaturalDuration.TimeSpan.TotalSeconds:0,gain=musicGain});
    }
    void StopMusic() {
        audioRamp.Stop();if(music==null)return;
        var player=music;music=null;
        try{var position=player.Position.TotalSeconds;player.Volume=0;player.Stop();player.Close();Log("audio-stopped",new {positionSeconds=position,gain=0});}
        catch(Exception e){Log("audio-cleanup-error",new {message=e.Message});}
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);e.Graphics.Clear(BackColor);
        // One bright layer only. A shared clock hands this native ribbon reveal to WebView2.
        float dpi=GetDpiForWindow(Handle)/96f;
        float width=Math.Min(680*dpi,Width*.9f),factor=width/800f;
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        var saved=e.Graphics.Save();e.Graphics.TranslateTransform((Width-width)/2,(Height-230*factor)/2-8*dpi);e.Graphics.ScaleTransform(factor,factor);
        var paths=new GraphicsPath[6];for(int i=0;i<6;i++)paths[i]=new GraphicsPath();
        paths[0].AddLines(new[]{new PointF(230,42),new PointF(167,42),new PointF(148,98)});
        paths[0].AddBezier(148,98,175,78,216,88,232,113);paths[0].AddBezier(232,113,250,137,236,160,261,188);paths[0].AddBezier(261,188,283,209,307,218,333,211);
        paths[1].AddBezier(140,175,153,215,199,218,225,187);
        paths[2].AddLines(new[]{new PointF(367,42),new PointF(296,42),new PointF(277,98)});
        paths[2].AddBezier(277,98,311,76,352,88,363,122);paths[2].AddBezier(363,122,380,150,367,171,390,195);paths[2].AddBezier(390,195,414,218,452,222,477,209);
        paths[3].AddBezier(400,99,424,34,490,15,536,66);paths[3].AddBezier(536,66,505,110,503,155,527,192);paths[3].AddBezier(527,192,555,163,558,127,545,103);
        paths[4].AddBezier(549,49,586,17,639,23,665,58);paths[5].AddBezier(551,207,596,232,638,227,667,197);
        var colors=(Dictionary<string,object>)config["colors"];
        double time=(elapsed.Elapsed.TotalMilliseconds-timelineStart)*16000/duration;
        if(Str("symbol")=="550C")for(int i=0;i<6;i++){
            double u=Math.Max(0,Math.Min(1,(time-250-i*300)/650));u=u*u*(3-2*u);
            if(u<=0)continue;var box=paths[i].GetBounds();var clip=e.Graphics.Save();
            e.Graphics.SetClip(new RectangleF(box.Left-10,0,(box.Width+20)*(float)u,230));
            using(var pen=new Pen(ColorTranslator.FromHtml(Convert.ToString(colors[i==3?"logoRed":"logoWhite"])),17)){pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;e.Graphics.DrawPath(pen,paths[i]);}
            e.Graphics.Restore(clip);
        }
        else if(time>250)using(var font=new Font("Consolas",140,FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(Color.White))e.Graphics.DrawString(Str("symbol"),font,brush,140,30);
        foreach(var path in paths)path.Dispose();e.Graphics.Restore(saved);
        if(!nativeCaptured){nativeCaptured=true;Log("native-first-paint",null);BeginInvoke(new Action(StartMusic));if(qa)BeginInvoke(new Action(()=>{using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,Size));image.Save(Path.Combine(root,"evidence","native-first-paint.png"),System.Drawing.Imaging.ImageFormat.Png);}}));}
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==Keys.Escape){Skip("native-esc");return true;}return base.ProcessCmdKey(ref msg,keyData);}
    string Resource(string name){using(var reader=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name),Encoding.UTF8))return reader.ReadToEnd();}
    async Task Initialize() {
        try {
            watchdog.Start();
            web=new WebView2();web.Dock=DockStyle.Fill;web.DefaultBackgroundColor=BackColor;web.Visible=false;
            Controls.Add(web);
            var options=new CoreWebView2EnvironmentOptions();
            options.AdditionalBrowserArguments="--disable-background-networking --disable-component-update --disable-domain-reliability --disable-sync --no-first-run --no-proxy-server --host-resolver-rules=\"MAP * ~NOTFOUND\" --force-gpu-mem-available-mb=64 --gpu-program-cache-size-kb=4096 --disable-features=msEdgeSidebarV2";
            environment=await CoreWebView2Environment.CreateAsync(null,cachePath,options);
            environment.BrowserProcessExited+=(s,e)=>browserExited.TrySetResult(true);
            if(closing)return;
            await web.EnsureCoreWebView2Async(environment);
            if(closing){web.Dispose();return;}
            if(exportFps>0)web.ZoomFactor=96.0/GetDpiForWindow(Handle);
            browserPid=(int)web.CoreWebView2.BrowserProcessId;
            web.CoreWebView2.Settings.AreDevToolsEnabled=false;
            web.CoreWebView2.Settings.AreDefaultContextMenusEnabled=false;
            web.CoreWebView2.Settings.IsStatusBarEnabled=false;
            web.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled=false;
            web.CoreWebView2.Settings.IsPasswordAutosaveEnabled=false;
            web.CoreWebView2.Settings.IsGeneralAutofillEnabled=false;
            web.CoreWebView2.Settings.IsZoomControlEnabled=false;
            web.CoreWebView2.Settings.IsBuiltInErrorPageEnabled=false;
            web.CoreWebView2.NewWindowRequested+=(s,e)=>e.Handled=true;
            web.CoreWebView2.PermissionRequested+=(s,e)=>e.State=CoreWebView2PermissionState.Deny;
            web.CoreWebView2.DownloadStarting+=(s,e)=>e.Cancel=true;
            web.CoreWebView2.NavigationStarting+=(s,e)=>{var local=e.Uri=="about:blank"||e.Uri.StartsWith("data:text/html",StringComparison.OrdinalIgnoreCase);var allowed=ownNavigationPending&&local;ownNavigationPending=false;e.Cancel=!allowed;Log("navigation",new {localMarkup=local,allowed=allowed});};
            web.CoreWebView2.NavigationCompleted+=async(s,e)=>{Log("navigation-completed",new {success=e.IsSuccess,status=e.WebErrorStatus.ToString()});if(!closing){var diagnostic=await web.CoreWebView2.ExecuteScriptAsync("JSON.stringify({config:typeof BOOT_CONFIG,proof:typeof proof,ready:document.readyState,scripts:document.scripts.length,body:document.body.innerHTML.length})");Log("dom-diagnostic",new {result=diagnostic});}};
            web.CoreWebView2.AddWebResourceRequestedFilter("*",CoreWebView2WebResourceContext.All);
            web.CoreWebView2.WebResourceRequested+=(s,e)=>{
                if(e.Request.Uri.StartsWith("http:",StringComparison.OrdinalIgnoreCase)||e.Request.Uri.StartsWith("https:",StringComparison.OrdinalIgnoreCase)){
                    Log("blocked-network",new {uri=e.Request.Uri});e.Response=environment.CreateWebResourceResponse(new MemoryStream(),403,"Offline proof","Content-Type: text/plain");
                }
            };
            web.CoreWebView2.WebMessageReceived+=async(s,e)=>await Message(e);
            web.CoreWebView2.ProcessFailed+=(s,e)=>{Log("webview-failed",new {kind=e.ProcessFailedKind.ToString()});BeginClose(skipFadeMs);};
            var html=Resource("boot.html").Replace("/*__CSS__*/",Resource("boot.css")).Replace("/*__JS__*/",Resource("boot.js"));
            // Escape HTML-sensitive characters in configurable text; JSON remains script-safe.
            var safeConfig=json.Serialize(config).Replace("<","\\u003c").Replace(">","\\u003e").Replace("&","\\u0026").Replace("\u2028","\\u2028").Replace("\u2029","\\u2029");
            html=html.Replace("/*__CONFIG__*/",safeConfig);
            ownNavigationPending=true;web.CoreWebView2.NavigateToString(html);
            Log("webview-created",new {browserPid=browserPid,runtime=environment.BrowserVersionString});
        }catch(Exception e){Log(closing?"initialize-cancelled":"initialize-error",new {message=e.ToString()});BeginClose(skipFadeMs);}
    }
    async Task Message(CoreWebView2WebMessageReceivedEventArgs e) {
        if(closing)return;
        var message=json.Deserialize<Dictionary<string,object>>(e.WebMessageAsJson);var name=(string)message["event"];
        Log("web-"+name,message);
        if(name=="ready"&&!ready) {
            ready=true;nativeIntro.Stop();
            if(selfTest!=null){web.Visible=true;var testResult=await web.CoreWebView2.ExecuteScriptAsync("window.proof.testInteractions("+json.Serialize(selfTest)+")");Log("local-self-test",json.DeserializeObject(testResult));BeginClose(skipFadeMs);}
            else if(captureAt>=0){await web.CoreWebView2.ExecuteScriptAsync("window.proof.renderAt("+captureAt.ToString(System.Globalization.CultureInfo.InvariantCulture)+")");web.Visible=true;await Task.Delay(600);await CaptureImage(captureFile);BeginClose(skipFadeMs);}
            else if(exportFps>0){web.Visible=true;await ExportFrames();BeginClose(skipFadeMs);}
            else {var offset=(elapsed.Elapsed.TotalMilliseconds-timelineStart).ToString(System.Globalization.CultureInfo.InvariantCulture);await web.CoreWebView2.ExecuteScriptAsync("window.proof.start("+offset+")");if(closing)return;web.Visible=true;web.Focus();if(qa)CaptureBeats();}
        }
        if(name=="complete")BeginClose(fadeMs);
        if(name=="skip"&&selfTest==null)BeginClose(skipFadeMs);
        if(name=="error"){Log("animation-error",message);BeginClose(skipFadeMs);}
    }
    async void CaptureBeats(){try{foreach(int at in new[]{2700,4700,7800,10300,14600}){await Task.Delay(Math.Max(0,at-(int)(elapsed.Elapsed.TotalMilliseconds-timelineStart)));if(closing)return;await CaptureImage(Path.Combine(root,"evidence","beat-"+at+".png"));}}catch(Exception e){Log("capture-error",new {message=e.Message});}}
    async Task CaptureImage(string file){Directory.CreateDirectory(Path.GetDirectoryName(file));using(var stream=File.Create(file))await web.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,stream);}
    void CaptureScreen(string file){using(var image=new Bitmap(Width,Height))using(var g=Graphics.FromImage(image)){g.CopyFromScreen(Location,Point.Empty,Size);image.Save(file,System.Drawing.Imaging.ImageFormat.Png);}}
    async Task ExportFrames(){
        Directory.CreateDirectory(exportDir);watchdog.Stop();
        int total=(int)Math.Ceiling(duration/1000.0*exportFps);
        // Export uses the exact runtime DOM and timeline, with transitions settled per sampled frame.
        await web.CoreWebView2.ExecuteScriptAsync("document.head.insertAdjacentHTML('beforeend','<style>*{transition:none!important;animation-play-state:paused!important}</style>')");
        for(int i=0;i<total;i++){if(closing)return;double at=i*1000.0/exportFps;var time=at.ToString(System.Globalization.CultureInfo.InvariantCulture);await web.CoreWebView2.ExecuteScriptAsync("window.proof.renderAt("+time+");document.getAnimations().forEach(a=>{a.pause();a.currentTime="+time+";})");await Task.Delay(20);await CaptureImage(Path.Combine(exportDir,i.ToString("D5")+".png"));}
        Log("export-complete",new {frames=total,fps=exportFps});
    }
    void Skip(string source){if(!closing){Log("skip-request",new {source=source});BeginClose(skipFadeMs);}}
    async void BeginClose(int fade) {
        if(closing)return;closing=true;watchdog.Stop();nativeIntro.Stop();audioRamp.Stop();Log("fade-start",new {durationMs=fade});
        var startGain=music!=null?music.Volume:0;
        if(music!=null)Log("audio-fade",new {durationMs=fade,startGain=startGain});
        var clock=Stopwatch.StartNew();
        while(clock.ElapsedMilliseconds<fade){var remaining=1-Math.Min(1,clock.Elapsed.TotalMilliseconds/fade);Opacity=remaining;if(music!=null)music.Volume=startGain*remaining;await Task.Delay(15);}
        StopMusic();Opacity=0;Hide();Log("window-hidden",null);
        try{if(web!=null)web.Dispose();}catch(Exception e){Log("dispose-error",new {message=e.Message});}
        var exit=await Task.WhenAny(browserExited.Task,Task.Delay(4000));
        Log("browser-disposed",new {confirmed=browserPid==0||exit==browserExited.Task,browserPid=browserPid});
        // Delete ONLY this run's newly generated cache, after the browser has exited and path validation.
        if(browserPid==0||exit==browserExited.Task){try{var runtimeRoot=Path.GetFullPath(Path.Combine(root,"runtime"))+Path.DirectorySeparatorChar;var own=Path.GetFullPath(cachePath);if(own.StartsWith(runtimeRoot,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(own))Directory.Delete(own,true);}catch(Exception e){Log("cache-cleanup-pending",new {message=e.Message});}}
        Log("exit",null);watchdog.Dispose();nativeIntro.Dispose();audioRamp.Dispose();Close();
    }
}
