# VRCFaceTracking reference assemblies

The module compiles against two assemblies that the VRCFaceTracking host supplies at
runtime. They are **not** redistributed here. Drop these two files into this folder
before building the module:

- `VRCFaceTracking.Core.dll`
- `VRCFaceTracking.SDK.dll`

Both ship with VRCFaceTracking. Copy them from your VRCFaceTracking installation
(the folder that contains `VRCFaceTracking.exe`). If you installed from the Store/MSIX
and cannot reach that folder, grab the matching versions from the VRCFaceTracking
release you run.

Then build:

```
dotnet build src/QpEnhance.Module/QpEnhance.Module.csproj -c Release
```

Or point the build at the DLLs without copying them:

```
dotnet build src/QpEnhance.Module/QpEnhance.Module.csproj -c Release -p:VrcftLibs="C:\path\to\VRCFaceTracking"
```

These references are compile-time only (`<Private>false</Private>`), so they are never
copied into the module output.
