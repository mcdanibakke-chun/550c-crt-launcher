param([Parameter(Mandatory=$true)][string]$RuntimeDirectory)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $RuntimeDirectory).Path
$exe=Join-Path $root '550C-CRT-Launcher.exe'
$icon=Join-Path $root 'assets\icons\550c-terminal.ico'
if(-not(Test-Path -LiteralPath $exe) -or -not(Test-Path -LiteralPath $icon)){throw 'Expected complete extracted runtime with launcher and original terminal icon.'}
$desktop=[Environment]::GetFolderPath('Desktop')
$path=Join-Path $desktop '550C CRT Launcher.lnk'
if((Test-Path -LiteralPath $path) -or (Test-Path -LiteralPath (Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) '550C CRT Launcher.lnk'))){throw 'Existing desktop entry preserved; choose a different entry yourself.'}
$shell=New-Object -ComObject WScript.Shell
$link=$shell.CreateShortcut($path)
$link.TargetPath=$exe;$link.WorkingDirectory=$root;$link.Arguments='';$link.IconLocation=$icon+',0';$link.Description='Independent gray CRT launcher for the official ChatGPT Desktop';$link.Save()
$read=$shell.CreateShortcut($path)
if($read.TargetPath -ne $exe -or $read.IconLocation -ne ($icon+',0')){throw 'Shortcut read-back mismatch'}
Write-Output ('Created new entry: '+$path+'. Official shortcuts untouched.')
