param([string[]]$Cases=@('fresh','slow','skip','already','missing','timeout','retry','close'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$test=Join-Path $root 'out\tests';$exe=Join-Path $test 'LifecycleTest.exe'
if(-not(Test-Path -LiteralPath $exe)){throw 'Build -TestBuild first'}
$evidence=Join-Path $root 'build\test-evidence';New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$originalConfig=Get-Content -LiteralPath (Join-Path $root 'config\default.json') -Raw -Encoding utf8
$all=@()
foreach($case in $Cases){
 if($case -notin @('fresh','slow','skip','already','missing','timeout','retry','close')){throw 'Unknown isolated case'}
 $config=$originalConfig | ConvertFrom-Json
 $config.window.style='centered';$config.window.topMost=$false
 $config.chatgpt.windowTimeoutMs=$(if($case -eq 'timeout'){2000}else{25000});$config.chatgpt.failureAutoCloseMs=2000
 $config.text.launcher.title='550C ISOLATED TEST';$config.text.launcher.failure='[TEST] EXPECTED FAILURE';$config.text.launcher.failureDetail='Local mock target only. No official activation.'
 [IO.File]::WriteAllText((Join-Path $test 'config.json'),($config | ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))
 $before=@(Get-Process ChatGPT -ErrorAction SilentlyContinue | ForEach-Object Id)
 $p=Start-Process -FilePath $exe -ArgumentList @('--test-mode',$case) -WindowStyle Hidden -PassThru
 $clock=[Diagnostics.Stopwatch]::StartNew();$known=[Collections.Generic.HashSet[int]]::new();[void]$known.Add($p.Id)
 while(-not $p.HasExited -and $clock.Elapsed.TotalSeconds -lt 45){
  $tree=@(Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId)
  for($pass=0;$pass -lt 4;$pass++){foreach($item in $tree){if($known.Contains([int]$item.ParentProcessId)){[void]$known.Add([int]$item.ProcessId)}}}
  Start-Sleep -Milliseconds 300;$p.Refresh()
 }
 Start-Sleep -Milliseconds 650
 $residual=@(foreach($pidValue in $known){Get-Process -Id $pidValue -ErrorAction SilentlyContinue | Select-Object Id,ProcessName})
 $after=@(Get-Process ChatGPT -ErrorAction SilentlyContinue | ForEach-Object Id)
 $logs=@(Get-ChildItem -LiteralPath (Join-Path $test 'evidence') -Filter ('run-*-'+$p.Id+'.jsonl'))
 if($logs.Count -ne 1){throw 'Expected one isolated run log'}
 $events=@(Get-Content -LiteralPath $logs[0].FullName -Encoding utf8 | ConvertFrom-Json)
 $failure=@($events | Where-Object eventName -eq 'launch-failure')
 $wait=@($events | Where-Object eventName -eq 'waiting')
 $reveal=@($events | Where-Object eventName -eq 'reveal-complete')
 $bad=@($events | Where-Object eventName -in @('initialize-error','animation-error','web-error','webview-failed'))
 $activationOwn=@($events | Where-Object {$_.eventName -eq 'activation-success' -and $_.details.pid -ne $p.Id}).Count -eq 0
 $pass=$activationOwn -and $p.HasExited -and $p.ExitCode -eq 0 -and $residual.Count -eq 0 -and $bad.Count -eq 0
 if($case -eq 'already'){$pass=$pass -and @($events | Where-Object eventName -eq 'already-running-focus').Count -eq 1 -and @($events | Where-Object eventName -eq 'activation-request').Count -eq 0}
 elseif($case -in @('missing','timeout','close')){$pass=$pass -and $failure.Count -gt 0}else{$pass=$pass -and $reveal.Count -eq 1}
 if($case -in @('slow','skip')){$pass=$pass -and $wait.Count -gt 0}
 $result=[ordered]@{case=$case;pass=$pass;seconds=$clock.Elapsed.TotalSeconds;officialActivation=$false;officialProcessSetUnchanged=(@(Compare-Object $before $after).Count -eq 0);residual=$residual;waitingEvents=$wait.Count;failureEvents=$failure.Count;revealEvents=$reveal.Count;log=$logs[0].Name;realForegroundTest=$false;activationTargetsOwnMockOnly=$activationOwn}
 $result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidence ($case+'.json')) -Encoding utf8
 $all+=[pscustomobject]$result;Write-Output ($case+': '+$(if($pass){'PASS'}else{'FAIL'}))
 if(-not $pass){throw ('Isolated lifecycle failed: '+$case)}
}
$all | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidence 'summary.json') -Encoding utf8
& (Join-Path $PSScriptRoot 'test-preview.ps1')
