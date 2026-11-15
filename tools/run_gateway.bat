@echo off
rem Starts the AI gateway for the Unity AI panel (needs GEMINI_API_KEY in .env, packages from ai/requirements.txt).
cd /d "%~dp0.."
python -m uvicorn --factory ai.gateway.app:create_app --port 8765
