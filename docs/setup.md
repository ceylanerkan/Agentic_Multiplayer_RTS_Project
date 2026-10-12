# Setup

## 1. Unity project
1. Install **Unity 6 LTS** (6000.x) via Unity Hub with *Windows Build Support (IL2CPP)*.
2. Unity Hub → New project → **Universal 3D (URP)** → name `UnityRTS`, location = this repository root.
3. Package Manager → *Add package by name*:
   - `com.unity.ai.navigation` (NavMesh Surface)
   - `com.unity.nuget.newtonsoft-json`
   - ParrelSync (Add package from git URL): `https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync`
4. Project Settings → Player → *Run In Background* = on (needed for two local instances).

## 2. Photon Fusion 2
1. Create a free account at https://dashboard.photonengine.com → *Create a new app* → type **Fusion** → SDK **Fusion 2**.
   Copy the **App ID**.
2. Download the Fusion 2 SDK (`.unitypackage`) from
   https://doc.photonengine.com/fusion/current/getting-started/sdk-download and import it into `UnityRTS`.
3. The Fusion Hub opens automatically (otherwise *Tools → Fusion → Fusion Hub*). Paste the App ID.
   It is stored in `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`. Do **not** push a public repo with
   the real App ID; reset it to empty before submission or keep the repo private.

## 3. Two instances on one machine
- ParrelSync → *Clones Manager* → *Add new clone* → open the clone. One editor uses **Host**, the other **Join**.
- Alternatively: *File → Build And Run* (Windows) and use the editor as the second player.

## 4. Python AI side (from milestone M4)
```bash
cd ai
python -m venv .venv
.venv\Scripts\activate          # Windows PowerShell: .venv\Scripts\Activate.ps1
pip install -r requirements.txt
copy ..\.env.example ..\.env     # then put your GEMINI_API_KEY in .env
python rag/build_index.py
```
A Gemini API key can be created at https://aistudio.google.com/apikey (free tier).
