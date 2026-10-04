param([switch]$TestBuild)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler=Join-Path $framework 'csc.exe'
$vendor=Join-Path $root 'vendor\webview2-1.0.4258.31'
if(-not(Test-Path -LiteralPath $compiler)){throw 'Existing x64 .NET Framework compiler missing. No installation performed.'}
if(-not(Test-Path -LiteralPath (Join-Path $vendor 'lib\net462\Microsoft.Web.WebView2.Core.dll'))){throw 'Run scripts/restore-dependencies.ps1 explicitly once, or supply the verified pinned vendor package.'}
$output=Join-Path $root $(if($TestBuild){'out\tests'}else{'out\550c-crt-launcher-0.1.0-preview.1-win-x64'})
if(Test-Path -LiteralPath $output){throw 'Versioned output exists; preserve it. Use a fresh checkout/build directory to rebuild.'}
New-Item -ItemType Directory -Path $output -Force | Out-Null
$icon=Join-Path $root 'assets\icons\550c-terminal.ico'
$build=Join-Path $root 'build';New-Item -ItemType Directory -Path $build -Force | Out-Null
if(-not(Test-Path -LiteralPath $icon)){
 & $compiler /nologo /target:exe /reference:System.Drawing.dll ('/out:'+(Join-Path $build 'PublicIcon.exe')) (Join-Path $root 'src\PublicIcon.cs');if($LASTEXITCODE -ne 0){throw 'Icon raster exporter failed'}
 & (Join-Path $build 'PublicIcon.exe') (Join-Path $root 'assets\icons\550c-terminal.png');if($LASTEXITCODE -ne 0){throw 'Icon render failed'}
 & $compiler /nologo /target:exe /reference:System.Drawing.dll ('/out:'+(Join-Path $build 'IconPack.exe')) (Join-Path $root 'src\IconPack.cs');if($LASTEXITCODE -ne 0){throw 'ICO compiler failed'}
 & (Join-Path $build 'IconPack.exe') (Join-Path $root 'assets\icons\550c-terminal.png') $icon (Join-Path $build 'icon-preview');if($LASTEXITCODE -ne 0){throw 'ICO packaging failed'}
}
$common=@('/nologo','/target:winexe','/platform:x64','/optimize+','/utf8output',('/win32manifest:'+(Join-Path $root 'src\app.manifest')),('/win32icon:'+$icon),
 '/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/reference:System.Web.Extensions.dll','/reference:System.Xml.dll','/reference:System.Xml.Linq.dll','/reference:Microsoft.CSharp.dll',
 ('/reference:'+(Join-Path $framework 'WPF\PresentationCore.dll')),('/reference:'+(Join-Path $framework 'WPF\WindowsBase.dll')),
 ('/reference:'+(Join-Path $vendor 'lib\net462\Microsoft.Web.WebView2.Core.dll')),('/reference:'+(Join-Path $vendor 'lib\net462\Microsoft.Web.WebView2.WinForms.dll')),
 ('/resource:'+(Join-Path $root 'boot\index.html')+',boot.html'),('/resource:'+(Join-Path $root 'boot\boot.css')+',boot.css'),('/resource:'+(Join-Path $root 'boot\boot.js')+',boot.js'))
$launcher=Join-Path $output $(if($TestBuild){'LifecycleTest.exe'}else{'550C-CRT-Launcher.exe'})
$compile=$common+@(('/out:'+$launcher),(Join-Path $root 'src\LauncherHost.cs'),(Join-Path $root 'src\DesktopBridge.cs'),(Join-Path $root 'src\ClientDiscovery.cs'))
if($TestBuild){$compile+='/define:G2_TEST';$compile+=(Join-Path $root 'tests\TestDesktopBridge.cs')}
& $compiler @compile;if($LASTEXITCODE -ne 0){throw 'Launcher build failed'}
if(-not $TestBuild){
 $preview=Join-Path $output '550C-Boot-Preview.exe'
 & $compiler @common ('/out:'+$preview) (Join-Path $root 'src\ProofHost.cs');if($LASTEXITCODE -ne 0){throw 'Standalone preview build failed'}
}
foreach($dll in 'lib\net462\Microsoft.Web.WebView2.Core.dll','lib\net462\Microsoft.Web.WebView2.WinForms.dll','runtimes\win-x64\native\WebView2Loader.dll'){Copy-Item -LiteralPath (Join-Path $vendor $dll) -Destination $output}
Copy-Item -LiteralPath (Join-Path $root 'config\default.json') -Destination (Join-Path $output 'config.json')
Copy-Item -LiteralPath (Join-Path $root 'third-party\WebView2-LICENSE.txt') -Destination $output
foreach($exe in Get-ChildItem -LiteralPath $output -Filter '*.exe'){
 [IO.File]::WriteAllText(($exe.FullName+'.config'),'<?xml version="1.0"?><configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8"/></startup></configuration>')
}
if(-not $TestBuild){
 foreach($file in 'README.md','LICENSE','CREDITS.md','THIRD_PARTY_NOTICES.md','CHANGELOG.md','SECURITY.md'){Copy-Item -LiteralPath (Join-Path $root $file) -Destination $output}
 New-Item -ItemType Directory -Path (Join-Path $output 'scripts') -Force | Out-Null;Copy-Item -LiteralPath (Join-Path $root 'scripts\create-shortcut.ps1') -Destination (Join-Path $output 'scripts')
 foreach($dir in 'themes','assets\icons','docs'){New-Item -ItemType Directory -Path (Join-Path $output $dir) -Force | Out-Null;Copy-Item -LiteralPath (Join-Path $root $dir) -Destination (Split-Path (Join-Path $output $dir) -Parent) -Recurse -Force}
}
Write-Output ('Built '+$output+'; no desktop/official settings changes.')
