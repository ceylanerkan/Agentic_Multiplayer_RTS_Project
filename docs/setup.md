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
- If the ParrelSync clone does not open (it can close itself during its first script compile), use a Windows build as the host
  and the editor as the second player:
  1. `Tools > RTS > Set build window (1280x720, windowed)` once (windowed, resizable, Run In Background, so the unfocused
     window keeps simulating).
  2. *File → Build Profiles* (or *Build Settings*): platform Windows, scenes `Menu` (index 0) and `Game` (index 1) ticked.
     *Build And Run*, choose the folder `UnityRTS/Builds/Win` (git-ignored; never inside `Assets`).
  3. In the build window: type a session name (e.g. `test1`) and press **Host**. The status line says it is waiting.
  4. In the editor: press Play (starts in `Menu`), type the same session name and press **Join**.
  5. Expected: both windows show their own Town Center + 3 Villagers (blue) and, once scouted, the enemy (red).
     The build's log is `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\Player.log`.
  Rebuild the player after every code change; an old build and a new editor do not match (different network prefabs).
  Test builds use the default Mono backend (fast). The final IL2CPP build (TODO.md [You] 4) also needs Visual Studio's
  "Desktop development with C++" workload installed.
- Second alternative, no build needed: Unity 6's **Multiplayer Play Mode** (Package Manager > Unity Registry >
  "Multiplayer Play Mode", `com.unity.multiplayer.playmode`; Photon's Fusion 2 samples use it). *Window > Multiplayer >
  Multiplayer Play Mode*, tick Player 2, press Play: a second window opens. Host in one window, Join with the same session name in
  the other. Not tried with this project yet; if Player 2 does not start in `Menu`, use the build path above. Remove ParrelSync
  only if this works.
- With only one instance, Host alone still gets its Town Center + 3 Villagers and can play/ask the AI; the match "waits for P2".
- Play always starts in the `Menu` scene, whichever scene is open (`Tools > RTS > Play from Menu scene`, on by default).
- Game view resolution: **Full HD (1920x1080)**; for two windows side by side **1280x720**. The UI scales with the window.

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
python -m pytest -q ai evaluation                           # 87 tests, no network, no API key needed
uvicorn --factory ai.gateway.app:create_app --port 8765     # AI gateway for the Unity AI panel (needs GEMINI_API_KEY)
python evaluation/run_eval.py                               # offline mock run; add --real for Gemini
dotnet build tools/compile-check/CompileCheck.csproj        # optional: compile Unity scripts against stubs (no Unity needed)
```
Windows shortcut: double-click `tools/run_gateway.bat` (same command as the uvicorn line).
Set `MCP_TRANSPORT=stdio` (in `.env`) to run the MCP server as a subprocess with the real MCP protocol.
C# rule tests: Unity > Window > General > Test Runner > EditMode (`RulesTests`).

## 6. Unity scenes and prefabs
The scripts need scenes, prefabs and components that only the editor can create. The exact steps (scene objects, prefab
components, field values) are in [TODO.md](../TODO.md), sections 1-4.
The `Menu` and `Game` scenes and `Assets/Prefabs` are generated by `Tools > RTS > 0 Create Menu and Game scenes`; after a change to
`Assets/Scripts/Editor/RtsSetupTools.cs` use `Tools > RTS > Rebuild generated scenes and prefabs` (it deletes and recreates them).

