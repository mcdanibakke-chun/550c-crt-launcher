$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$version='0.1.0-preview.1'
$runtime=Join-Path $root ('out\550c-crt-launcher-'+$version+'-win-x64')
$packages=Join-Path $root 'out\packages'
if(Test-Path -LiteralPath $packages){throw 'Published package outputs are immutable; use a fresh build directory.'}
New-Item -ItemType Directory -Path $packages -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Assert-PublicFile([string]$Path){
 $extension=[IO.Path]::GetExtension($Path).ToLowerInvariant()
 if($extension -in @('.lnk','.log','.jsonl','.wav','.mp3','.aac','.m4a','.mp4','.ttf','.otf','.ttc','.nupkg')){throw ('Private/restricted extension: '+$Path)}
 if($extension -in @('.cs','.ps1','.md','.json','.txt','.html','.css','.js','.yml','.yaml','.manifest','.svg','')){
  $content=[IO.File]::ReadAllText($Path)
  if($content -match '(?i)[A-Z]:[\\/]Users[\\/]|[A-Z]:[\\/]CodexProjects[\\/]|BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY|sk-(proj-|[A-Za-z0-9]{20})|Authorization:\s*Bearer\s+[A-Za-z0-9]'){throw ('Private path/secret/source-media marker: '+$Path)}
 }
}
function Write-Archive([string]$Base,[string]$Name,[array]$Files){
 $staging=Join-Path $root ('build\package-'+$Name)
 if(Test-Path -LiteralPath $staging){throw 'Preserve previous packaging staging'}
 New-Item -ItemType Directory -Path $staging -Force | Out-Null
 $records=@()
 foreach($file in $Files){
  Assert-PublicFile $file.FullName
  $relative=$file.FullName.Substring($Base.Length).TrimStart([char[]]@('\','/')).Replace('\','/')
  $destination=Join-Path $staging $relative
  New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
  Copy-Item -LiteralPath $file.FullName -Destination $destination
  $records += [pscustomobject]@{path=$relative;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $destination).Hash}
 }
 $manifest=[ordered]@{version=$version;files=$records;musicIncluded=$false;fontFilesIncluded=$false;modifiedOfficialLogoIncluded=$false;humanPlaybackAcceptance='PENDING';codeLicense='MIT; third-party rights excluded'}
 $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $staging 'MANIFEST.json') -Encoding utf8
 $zip=Join-Path $packages ($Name+'.zip')
 [IO.Compression.ZipFile]::CreateFromDirectory($staging,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
 return $zip
}
$source=@(foreach($name in 'src','boot','config','themes','assets\icons','scripts','docs','tests','.github','third-party'){Get-ChildItem -LiteralPath (Join-Path $root $name) -File -Recurse};foreach($name in 'README.md','LICENSE','CREDITS.md','THIRD_PARTY_NOTICES.md','CHANGELOG.md','SECURITY.md','CONTRIBUTING.md','AGENTS.md','.gitignore','.gitattributes'){Get-Item -LiteralPath (Join-Path $root $name)})
$runtimeFiles=@(foreach($name in '550C-CRT-Launcher.exe','550C-CRT-Launcher.exe.config','550C-Boot-Preview.exe','550C-Boot-Preview.exe.config','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','WebView2-LICENSE.txt','config.json','README.md','LICENSE','CREDITS.md','THIRD_PARTY_NOTICES.md','CHANGELOG.md','SECURITY.md'){Get-Item -LiteralPath (Join-Path $runtime $name)};foreach($name in 'themes','assets\icons','docs','scripts'){Get-ChildItem -LiteralPath (Join-Path $runtime $name) -File -Recurse})
$zips=@((Write-Archive $root ('550c-crt-launcher-'+$version+'-source') $source),(Write-Archive $runtime ('550c-crt-launcher-'+$version+'-win-x64') $runtimeFiles))
$lines=@(foreach($zip in $zips){(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($zip)})
$lines | Set-Content -LiteralPath (Join-Path $packages 'SHA256SUMS.txt') -Encoding ascii
Write-Output ('Source files='+$source.Count+'; runtime files='+$runtimeFiles.Count+'; privacy/asset allowlist checks passed.')
