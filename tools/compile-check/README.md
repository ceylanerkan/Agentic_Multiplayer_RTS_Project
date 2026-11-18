# Compile check without Unity

`UnityFusionStubs.cs` contains minimal fake `UnityEngine`, `TMPro` and `Fusion` types. `CompileCheck.csproj` compiles our
gameplay, networking and UI scripts against them, so typos and type errors are found without the editor.
It does NOT check real Unity or Fusion APIs (the stubs are written from memory; member names used by our scripts were also checked against `UnityRTS/Assets/Photon/Fusion/Assemblies/Fusion.Runtime.xml`): a clean build here is necessary, not sufficient.

    dotnet build tools/compile-check/CompileCheck.csproj

(`GameLauncher.cs`, `MenuUI.cs`, editor scripts and `AiGatewayClient.cs` are not included; the stub declares a small `GameLauncher`.)
