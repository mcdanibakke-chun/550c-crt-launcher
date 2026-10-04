param([string[]]$Cases=@('full','buttons','esc','click'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$runtime=Join-Path $root 'out\550c-crt-launcher-0.1.0-preview.1-win-x64'
$exe=Join-Path $runtime '550C-Boot-Preview.exe'
$evidence=Join-Path $root 'build\test-evidence';New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$all=@()
foreach($case in $Cases){
 if($case -notin @('full','buttons','esc','click')){throw 'Unknown standalone case'}
 $argsList=$(if($case -eq 'full'){@('--offscreen')}else{@('--self-test',$case)})
 $p=Start-Process -FilePath $exe -ArgumentList $argsList -WindowStyle Hidden -PassThru
 $clock=[Diagnostics.Stopwatch]::StartNew();$known=[Collections.Generic.HashSet[int]]::new();[void]$known.Add($p.Id)
 $peak=0.0
 while(-not $p.HasExited -and $clock.Elapsed.TotalSeconds -lt 40){
  $tree=@(Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId)
  for($pass=0;$pass -lt 4;$pass++){foreach($item in $tree){if($known.Contains([int]$item.ParentProcessId)){[void]$known.Add([int]$item.ProcessId)}}}
  $live=@(foreach($pidValue in $known){Get-Process -Id $pidValue -ErrorAction SilentlyContinue})
  $peak=[Math]::Max($peak,($live | Measure-Object WorkingSet64 -Sum).Sum/1MB)
  Start-Sleep -Milliseconds 300;$p.Refresh()
 }
 Start-Sleep -Milliseconds 650
 $remaining=@(foreach($pidValue in $known){Get-Process -Id $pidValue -ErrorAction SilentlyContinue | Select-Object Id,ProcessName})
 $log=Get-ChildItem -LiteralPath (Join-Path $runtime 'evidence') -Filter ('run-*-'+$p.Id+'.jsonl') | Select-Object -First 1
 $events=@(Get-Content -LiteralPath $log.FullName -Encoding utf8 | ConvertFrom-Json)
 $complete=($events | Where-Object eventName -eq 'web-complete' | Select-Object -First 1).details
 $self=($events | Where-Object eventName -eq 'local-self-test' | Select-Object -First 1).details
 $bad=@($events | Where-Object eventName -in @('initialize-error','animation-error','web-error','webview-failed'))
 $pass=$p.HasExited -and $p.ExitCode -eq 0 -and $remaining.Count -eq 0 -and $bad.Count -eq 0
 if($case -eq 'full'){$pass=$pass -and $complete.elapsedMs -ge 15900 -and $complete.elapsedMs -le 16500}else{$pass=$pass -and $self.pass}
 $result=[ordered]@{case=$case;pass=$pass;seconds=$clock.Elapsed.TotalSeconds;peakTreeRamMiB=$peak;complete=$complete;selfTest=$self;residual=$remaining;officialActivation=$false;log=$log.Name;nativeFirstPaintMs=($events | Where-Object eventName -eq 'native-first-paint' | Select-Object -First 1).hostMs}
 $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidence ('preview-'+$case+'.json')) -Encoding utf8
 $all+=[pscustomobject]$result;Write-Output ('preview '+$case+': '+$(if($pass){'PASS'}else{'FAIL'}))
 if(-not $pass){throw ('Standalone preview failed: '+$case)}
}
$all | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidence 'preview-summary.json') -Encoding utf8
