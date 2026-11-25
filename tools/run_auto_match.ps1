# Automatic two-player match: starts the test build twice without a window (host, client) and compares what both saw.
# First build it (editor closed):
#   Unity.exe -batchmode -quit -projectPath UnityRTS -executeMethod AutoBuild.TestBuild -logFile build.log
# Needs the Photon App ID and internet. Report: evaluation/unity_two_players.md, logs and pictures: UnityRTS/Logs/match
$root = Split-Path $PSScriptRoot -Parent
$exe = Join-Path $root 'UnityRTS\Builds\AutoTest\UnityRTS.exe'
$logs = Join-Path $root 'UnityRTS\Logs\match'
if (-not (Test-Path $exe)) { 'No test build: run AutoBuild.TestBuild first.'; exit 1 }
New-Item -ItemType Directory -Force $logs | Out-Null
Remove-Item "$logs\*.txt", "$logs\*.png" -ErrorAction SilentlyContinue
$session = 'automatch-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$both = foreach ($role in 'host', 'client') {
    $p = Start-Process $exe -PassThru -WindowStyle Hidden -ArgumentList '-batchmode', '-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0',
        '-rtsauto', $role, '-rtssession', $session, '-rtsout', "`"$logs\$role.txt`"", '-logFile', "`"$logs\$role.log`""
    $p.PriorityClass = 'BelowNormal'   # quiet: the match runs beside other work
    $p
    Start-Sleep 8
}
$end = (Get-Date).AddMinutes(10)
while (($both | Where-Object { -not $_.HasExited }) -and (Get-Date) -lt $end) { Start-Sleep 2 }
$both | Where-Object { -not $_.HasExited } | ForEach-Object { 'killed ' + $_.Id; $_.Kill() }
python (Join-Path $root 'evaluation\two_players_report.py') "$logs\host.txt" "$logs\client.txt"
exit $LASTEXITCODE
