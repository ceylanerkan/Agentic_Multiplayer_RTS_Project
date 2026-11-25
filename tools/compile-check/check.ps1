# Compiles all C# scripts of the project against the real Unity, Fusion and package assemblies, without starting Unity (a few seconds).
#   powershell -ExecutionPolicy Bypass -File tools/compile-check/check.ps1
# Uses the compiler inside the Unity installation; no .NET SDK is needed. The project must have been opened in Unity once (Library folder).
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$project = Join-Path $root 'UnityRTS'
$version = ((Get-Content (Join-Path $project 'ProjectSettings\ProjectVersion.txt'))[0] -split ': ')[1].Trim()
$data = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Data"
$dotnet = "$data\NetCoreRuntime\dotnet.exe"; $csc = "$data\DotNetSdkRoslyn\csc.dll"
$built = Join-Path $project 'Library\ScriptAssemblies'
if (-not (Test-Path $csc)) { "Unity $version not found in $data"; exit 1 }
if (-not (Test-Path $built)) { 'Open the project in Unity once: UnityRTS\Library\ScriptAssemblies does not exist.'; exit 1 }
$out = Join-Path $env:TEMP 'rts-compile-check'
New-Item -ItemType Directory -Force $out | Out-Null
Remove-Item "$out\*.dll" -ErrorAction SilentlyContinue

$ours = 'RTS.Core', 'RTS.Tests.EditMode', 'Assembly-CSharp', 'Assembly-CSharp-Editor'
$refs = @()
$refs += Get-ChildItem "$data\UnityReferenceAssemblies\unity-4.8-api\*.dll", "$data\UnityReferenceAssemblies\unity-4.8-api\Facades\*.dll"
$refs += Get-ChildItem "$data\Managed\UnityEngine\*.dll"
$refs += Get-ChildItem "$built\*.dll" | Where-Object { $ours -notcontains $_.BaseName }
$refs += Get-ChildItem "$project\Assets\Photon\Fusion\Assemblies\*.dll"
$refs += Get-ChildItem "$project\Library\PackageCache\com.unity.nuget.newtonsoft-json*\Runtime\Newtonsoft.Json.dll"
$refs += Get-ChildItem "$project\Library\PackageCache\com.unity.ext.nunit*" -Recurse -Filter nunit.framework.dll | Select-Object -First 1

function Compile($name, $files, $needs, $defines) {
    $rsp = Join-Path $out "$name.rsp"
    $lines = @('-nologo', '-target:library', '-preferreduilang:en', '-langversion:9', '-nostdlib', '-unsafe', '-nowarn:0649,0169,0414,0108,0114,1701,1702,0436,0618', "-out:`"$out\$name.dll`"")
    $lines += '-define:' + (('UNITY_6000_0_OR_NEWER;UNITY_2022_3_OR_NEWER;UNITY_2021_2_OR_NEWER;ENABLE_INPUT_SYSTEM;UNITY_STANDALONE;FUSION_WEAVER;FUSION2;' + $defines).Trim(';'))
    $lines += $refs | ForEach-Object { "-r:`"$($_.FullName)`"" }
    $lines += $needs | Where-Object { $_ } | ForEach-Object { "-r:`"$out\$_.dll`"" }
    $lines += $files | ForEach-Object { "`"$($_.FullName)`"" }
    Set-Content $rsp $lines -Encoding UTF8
    $text = & $dotnet exec $csc "@$rsp"
    $ok = $LASTEXITCODE -eq 0
    if ($ok) { "ok    $name ($(@($files).Count) files)" } else { "FAIL  $name"; $text | Where-Object { $_ -match 'error' } | Select-Object -First 20; $script:failed = $true }
}

$failed = $false
$scripts = Get-ChildItem "$project\Assets\Scripts" -Recurse -Filter *.cs
$core = $scripts | Where-Object { $_.FullName -like '*\Scripts\Core\*' }
$editor = $scripts | Where-Object { $_.FullName -like '*\Scripts\Editor\*' }
$game = $scripts | Where-Object { $_.FullName -notlike '*\Scripts\Core\*' -and $_.FullName -notlike '*\Scripts\Editor\*' }
Compile 'RTS.Core' $core @() 'UNITY_EDITOR'
Compile 'Assembly-CSharp' $game @('RTS.Core') 'UNITY_EDITOR'
Compile 'Assembly-CSharp-Editor' $editor @('RTS.Core', 'Assembly-CSharp') 'UNITY_EDITOR'
Compile 'RTS.Tests.EditMode' (Get-ChildItem "$project\Assets\Tests" -Recurse -Filter *.cs) @('RTS.Core', 'Assembly-CSharp') 'UNITY_EDITOR;UNITY_INCLUDE_TESTS'
Compile 'AutoTest' $game @('RTS.Core') 'RTS_AUTOTEST'   # the test build with the automatic two-player match
if ($failed) { exit 1 }
'all scripts compile'
