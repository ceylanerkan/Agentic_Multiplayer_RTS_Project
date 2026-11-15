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
   It is stored in `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`, which is git-ignored so the
   App ID never reaches the (public) repository. After cloning, paste your own App ID in the Fusion Hub.

## 3. Two instances on one machine
- ParrelSync → *Clones Manager* → *Add new clone* → open the clone. One editor uses **Host**, the other **Join**.
- Alternatively: *File → Build And Run* (Windows) and use the editor as the second player.

## 4. Python AI side (from milestone M4)
```bash
cd ai
python -m venv .venv
.venv\Scripts\activate          # Windows PowerShell: .venv\Scripts\Activate.ps1
pip install -r requirements.txt   # sentence-transformers is large; without it retrieval uses BM25 only
copy ..\.env.example ..\.env     # then put your GEMINI_API_KEY in .env (the gateway and run_eval load it)
python rag/build_index.py        # optional: embeddings for hybrid retrieval
```
A Gemini API key can be created at https://aistudio.google.com/apikey (free tier).

## 5. Run and test
```bash
# from the repository root
python -m pytest -q ai evaluation                           # 55 tests, no network, no API key needed
uvicorn --factory ai.gateway.app:create_app --port 8765     # AI gateway for the Unity AI panel (needs GEMINI_API_KEY)
python evaluation/run_eval.py                               # offline mock run; add --real for Gemini
dotnet build tools/compile-check/CompileCheck.csproj        # optional: compile Unity scripts against stubs (no Unity needed)
```
Set `MCP_TRANSPORT=stdio` (in `.env`) to run the MCP server as a subprocess with the real MCP protocol.
C# rule tests: Unity > Window > General > Test Runner > EditMode (`RulesTests`).

## 6. Unity scenes and prefabs
The scripts need scenes, prefabs and components that only the editor can create. The exact steps (scene objects, prefab
components, field values) are in [TODO.md](../TODO.md), sections 1-4.

