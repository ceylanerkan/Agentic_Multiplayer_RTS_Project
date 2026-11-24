@echo off
rem Starts the Windows build twice for a two-player match on one machine: one window presses Host, the other Join.
rem Each instance gets its own log (by default both write the same Player.log and overwrite each other's lines).
set EXE=%~dp0..\UnityRTS\Builds\UnityRTS.exe
if not exist "%EXE%" set EXE=%~dp0..\UnityRTS\Builds\Win\UnityRTS.exe
if not exist "%EXE%" (
  echo No build found in UnityRTS\Builds. Unity: File - Build Profiles - Windows - Build.
  pause
  exit /b 1
)
set LOGS=%USERPROFILE%\AppData\LocalLow\DefaultCompany\UnityRTS
start "" "%EXE%" -logFile "%LOGS%\Player-A.log"
start "" "%EXE%" -logFile "%LOGS%\Player-B.log"
