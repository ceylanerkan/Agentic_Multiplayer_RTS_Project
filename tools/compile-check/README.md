# Compile check without starting Unity

    powershell -ExecutionPolicy Bypass -File tools/compile-check/check.ps1

Compiles the rules, the game, the editor scripts, the tests and the test build against the real Unity, Fusion and package assemblies
with the compiler inside the Unity installation (about 20 seconds). Needs Unity 6000.4.9f1 and a project that was opened in Unity once
(`UnityRTS/Library`); no .NET SDK. It does not run Fusion's weaver and does not build shaders: the Unity build stays the final check.

(Until 25 November this folder compiled the scripts against hand-written stubs, because no Unity was available then.)
