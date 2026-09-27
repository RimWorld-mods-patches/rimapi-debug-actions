# RIMAPI Debug Actions

A RimWorld 1.6 companion mod for RIMAPI. It discovers methods marked with RimWorld's `DebugActionAttribute`, lists them with stable IDs, supports case-insensitive text filtering, and can invoke parameterless static actions.

RIMAPI exposes these routes under its extension namespace:

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/extensions/debug-actions/debug-actions` | List registered actions; optional `filter` query parameter filters by name, category, assembly, type, signature, or ID |
| POST | `/api/extensions/debug-actions/debug-actions/run?id={id}` | Invoke a listed runnable action |

The list marks each item with `is_runnable`. Debug actions that require parameters or an instance target are listed but cannot be invoked by this initial API. Actions run on RIMAPI's game-thread request pipeline and retain the behavior and side effects defined by their mods. The API should only be used in a trusted local development environment.

Example calls (replace the ID with one returned by the list):

```sh
curl 'http://localhost:8765/api/extensions/debug-actions/debug-actions'
curl 'http://localhost:8765/api/extensions/debug-actions/debug-actions?filter=Realistic%20Bases'
curl -X POST 'http://localhost:8765/api/extensions/debug-actions/debug-actions/run?id=BASE64URL_ID' \
  -H 'Content-Type: application/json' --data '{}'
```

## Build

The project targets RimWorld 1.6 reference assemblies from NuGet and references the installed RIMAPI 1.6 assembly. The default RIMAPI path matches the macOS Steam installation; override it when needed:

```sh
docker run --rm \
  -v "$PWD":/mod \
  -v rimworld-nuget:/root/.nuget/packages \
  -w /mod/Source/RimApiDebugActions \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet build -c Release \
    -p:RimApiDirectory="/path/to/RIMAPI/1.6/Assemblies"
```

The output assembly is written to `1.6/Assemblies/RimApiDebugActions.dll`. RimWorld and RIMAPI reference assemblies are not copied into the mod.
