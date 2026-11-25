@echo off
rem Starts the AI gateway for the Unity AI panel (needs a running Ollama with qwen2.5:7b; packages from ai/requirements.txt).
cd /d "%~dp0.."
rem use the virtual environment from docs/setup.md section 4 when it exists (its packages are not in the global Python)
set PY=python
rem the Laya environment (docs/setup.md section 4) sees the packages of the normal Python too; needed for TOOL_ROUTER=laya
if exist "%USERPROFILE%\.venvs\laya\Scripts\python.exe" set PY="%USERPROFILE%\.venvs\laya\Scripts\python.exe"
if exist "ai\.venv\Scripts\python.exe" set PY=ai\.venv\Scripts\python.exe
%PY% -m uvicorn --factory ai.gateway.app:create_app --port 8765
rem keep the window open after a crash or Ctrl+C, so the error message can be read
pause
