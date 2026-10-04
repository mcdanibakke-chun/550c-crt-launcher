using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

// Query registered Shell entries and the actual package manifest. Never launch a version path.
internal static class ClientDiscovery {
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]
    static extern int GetPackagePathByFullName(string fullName,ref uint length,StringBuilder path);
    internal static void Resolve(Dictionary<string,object> config) {
        var client=(Dictionary<string,object>)config["chatgpt"];
        var requested=Convert.ToString(client["appId"]);
        var matches=new List<Tuple<string,string>>();
        dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
        dynamic folder=shell.NameSpace("shell:AppsFolder");
        dynamic items=folder.Items();
        try {
            foreach(dynamic item in items) {
                try {
                    if(!String.Equals(Convert.ToString(item.Name),"ChatGPT",StringComparison.OrdinalIgnoreCase))continue;
                    var id=Convert.ToString(item.ExtendedProperty("System.AppUserModel.ID"));
                    var full=Convert.ToString(item.ExtendedProperty("System.AppUserModel.PackageFullName"));
                    var family=Convert.ToString(item.ExtendedProperty("System.AppUserModel.PackageFamilyName"));
                    if(String.IsNullOrEmpty(id)||String.IsNullOrEmpty(full)||String.IsNullOrEmpty(family))continue;
                    // Publisher ID audited from the actual official Store registration, not an installation path.
                    if(!family.EndsWith("_2p2nqsd0c76g0",StringComparison.OrdinalIgnoreCase)||!id.StartsWith(family+"!",StringComparison.Ordinal))continue;
                    if(!String.IsNullOrEmpty(requested)&&id!=requested)continue;
                    uint length=0;GetPackagePathByFullName(full,ref length,null);
                    if(length==0)continue;
                    var path=new StringBuilder((int)length);
                    if(GetPackagePathByFullName(full,ref length,path)!=0)continue;
                    XDocument manifest;
                    using(var reader=XmlReader.Create(Path.Combine(path.ToString(),"AppxManifest.xml"),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))manifest=XDocument.Load(reader);
                    var ns=manifest.Root.Name.Namespace;
                    var applications=manifest.Root.Element(ns+"Applications");
                    if(applications==null)continue;
                    foreach(var app in applications.Elements(ns+"Application")) {
                        if((string)app.Attribute("Id")!=id.Substring(family.Length+1))continue;
                        var executable=(string)app.Attribute("Executable");
                        if(String.IsNullOrEmpty(executable)||Path.IsPathRooted(executable))continue;
                        var root=Path.GetFullPath(path.ToString()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                        var entry=Path.GetFullPath(Path.Combine(root,executable));
                        if(!entry.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!File.Exists(entry))continue;
                        matches.Add(Tuple.Create(id,Path.GetFileNameWithoutExtension(entry)));
                    }
                } finally {if(Marshal.IsComObject(item))Marshal.FinalReleaseComObject(item);}
            }
        } finally {
            Marshal.FinalReleaseComObject(items);Marshal.FinalReleaseComObject(folder);Marshal.FinalReleaseComObject(shell);
        }
        if(matches.Count!=1)throw new InvalidOperationException("Expected one official registered ChatGPT MSIX entry. Found "+matches.Count+". Keep the official app installed; Win32/ARM64 are not supported by this preview. If multiple official entries exist, choose an audited chatgpt.appId in config.json.");
        client["appId"]=matches[0].Item1;client["processName"]=matches[0].Item2;
    }
}
