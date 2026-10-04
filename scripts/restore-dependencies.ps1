$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$version='1.0.4258.31'
$expected='56F7F4B8BF9AEE4B8EFEFBBDD4F67D5F74EBD1B100ED0806DA71BF76AF481AA9'
$vendor=Join-Path $root ('vendor\webview2-'+$version)
if(Test-Path -LiteralPath $vendor){throw 'Vendor directory exists; preserve it and verify instead of replacing.'}
$cache=Join-Path $root 'build\dependencies';New-Item -ItemType Directory -Path $cache -Force | Out-Null
$package=Join-Path $cache ('microsoft.web.webview2.'+$version+'.nupkg')
if(-not(Test-Path -LiteralPath $package)){
 [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
 Invoke-WebRequest -UseBasicParsing -Uri ('https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/'+$version+'/microsoft.web.webview2.'+$version+'.nupkg') -OutFile $package
}
if((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $expected){throw 'Pinned package SHA256 mismatch'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($package,$vendor)
Write-Output 'Pinned WebView2 SDK interface restored. No runtime/SDK/environment installation performed.'
