# RIMAPI Debug Actions

A RimWorld 1.6 companion mod for [RIMAPI](https://github.com/IlyaChichkov/RIMAPI). It discovers methods marked
with RimWorld's `LudeonTK.DebugActionAttribute`, lists them with stable IDs, supports case-insensitive text
filtering, invokes parameterless static actions and saves the running game over HTTP. It is test automation for mod development,
for example the live tests of [Realistic Bases](https://github.com/RimWorld-mods-patches/realistic-bases).

## Install

1. Install [RIMAPI](https://steamcommunity.com/sharedfiles/filedetails/?id=3593423732) and its dependency Harmony.
2. Download `RimApiDebugActions-<version>.zip` from the
   [releases](https://github.com/RimWorld-mods-patches/rimapi-debug-actions/releases) and extract it into
   RimWorld's `Mods` folder (`Mods/RimApiDebugActions/About/About.xml`). The rolling `main-latest` release
   follows the `main` branch.
3. Load it after RIMAPI.

## API

RIMAPI 1.10 mounts the extension under `/api/v1/debug-actions`:

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/v1/debug-actions/debug-actions` | List registered actions; the optional `filter` query parameter matches name, category, assembly, type, signature or ID |
| POST | `/api/v1/debug-actions/debug-actions/run?id={id}` | Invoke a listed runnable action |
| POST | `/api/v1/debug-actions/save?name={name}` | Save the running game as `{name}.rws` in the profile's save folder; `name` must be a plain file name |

Each listed action has `id`, `name`, `category`, `assembly`, `declaring_type`, `signature` and `is_runnable`.
Actions that need parameters or a map/pawn target are listed but cannot be invoked. A run returns
`success: false` with an `error` when the action is unknown, not runnable or throws. Actions run on RIMAPI's
game-thread request pipeline with the behaviour and side effects their mods define. **Use the API only in a
trusted local development environment.**

```sh
curl 'http://localhost:8765/api/v1/debug-actions/debug-actions?filter=Realistic%20Bases'
curl -X POST 'http://localhost:8765/api/v1/debug-actions/debug-actions/run?id=BASE64URL_ID' \
  -H 'Content-Type: application/json' --data '{}'
```

The save route answers `success: true` with the written `path`, `bytes` and `tick` only once the file
exists; the shared `setup-dev.py` uses it to create a profile's baseline save.

Use `localhost` rather than `127.0.0.1`; the macOS listener is IPv6-backed.

## Build

RIMAPI is not published on NuGet. The project downloads the pinned RIMAPI GitHub release
(`RimApiVersion` in the project file), verifies its SHA-256 and references the assemblies from `obj/`.
No local RimWorld or RIMAPI installation is needed, and nothing from RIMAPI is copied into this mod.

```sh
dotnet build Source/RimApiDebugActions/RimApiDebugActions.csproj -c Release
```

or without a local SDK:

```sh
docker run --rm -v "$PWD":/mod -v rimworld-nuget:/root/.nuget/packages \
  -w /mod/Source/RimApiDebugActions mcr.microsoft.com/dotnet/sdk:8.0 dotnet build -c Release
```

The output is `1.6/Assemblies/RimApiDebugActions.dll`. To move to a new RIMAPI release, update
`RimApiVersion` and `RimApiSha256` together.

CI builds every pull request into `main` on Linux, Windows and macOS. Pushes to `main` publish the
`main-latest` pre-release, and a `v<version>` tag publishes a release; the tag must match `<modVersion>` in
`About/About.xml` and `<Version>` in the project.

## License

[MIT](LICENSE). RIMAPI itself is GPL-3.0 and is only downloaded at build time, never redistributed here.
