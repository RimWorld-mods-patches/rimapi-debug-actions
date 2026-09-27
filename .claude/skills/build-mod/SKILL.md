---
name: build-mod
description: Build or inspect a RimWorld mod's C# assembly and check vanilla RimWorld APIs against the installed game source. Use after changing mod C# or when asked to build, install, or diagnose a mod.
---

# Build and inspect a RimWorld mod

Use the current repository's project files and build instructions. Do not assume its mod name, source project path, supported RimWorld versions, output folder, or dependency set.

## Build

1. Read applicable `AGENTS.md`, `README`, and build notes. Locate the mod `.csproj` with `rg --files -g '*.csproj'`; distinguish it from test or tool projects by checking its assembly name and output path.
2. Read the project file for its target framework, RimWorld reference package version, build configuration, and any required local mod assemblies or MSBuild properties. Keep the game's and other mods' assemblies as compile references; do not copy them into the mod's output.
3. On this workstation, the host has no .NET SDK. Build with the .NET SDK Docker image and mount the repository at `/mod`. Confirm the Docker daemon with `docker version --format '{{.Server.Version}}'`. If it is unavailable, ask the user to start it; do not try to start Docker or switch to a host compiler.

Example, after replacing the project directory with the path discovered above:

```sh
docker run --rm \
  -v "$PWD":/mod \
  -v rimworld-nuget:/root/.nuget/packages \
  -w /mod/Source/ProjectDirectory \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet build -c Release
```

`Source/ProjectDirectory` is a placeholder; replace it with the discovered `.csproj` directory.

If the project references a locally installed dependency mod, mount its assembly folder read-only and pass the MSBuild property expected by its `.csproj`. For example, a project declaring `RimApiDirectory` can use `-v "/path/to/dependency/Assemblies":/dependency:ro` and `-p:RimApiDirectory=/dependency`. Do not assume a particular dependency or Steam Workshop ID.

Use the project's configured output path. Keep `bin/` and `obj/` out of version control. Follow the repo's `.gitignore` and release workflow for generated mod assemblies; don't add a binary merely because a local build produced it. Never ship RimWorld, Unity, or dependency assemblies as part of the mod output.

## Install for local game use

When asked to install or symlink a mod, use the exact Mods folder requested by the user or identified for the installed game. Choose a link name from the mod folder or package ID. Check whether the destination already exists before creating or replacing a link, and verify the resulting link points to this repository. Do not modify other mods.

## Inspect vanilla RimWorld code

Do not guess type namespaces, member accessibility, signatures, or behavior when they affect an implementation. Inspect the installed game's `Assembly-CSharp.dll`, not the reference package, because reference assemblies have method bodies stripped.

This repo includes a small decompiler helper at `.claude/skills/build-mod/decompile`. When it can resolve the installed game assembly, run it from Docker, mounting the game's `Managed` directory read-only and the helper directory. Pass the installed `Assembly-CSharp.dll` and fully qualified type name. If decompilation cannot resolve the target framework or dependencies, inspect available local metadata or source; do not present guessed details as verified.

The installed game log on macOS is under `~/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`. Locate the equivalent log for other platforms instead of assuming this path applies everywhere.
