---
name: version-bump
description: Choose and apply a RimWorld mod version bump based on compatibility and player impact, keeping About metadata and the mod assembly project synchronized.
---

# Version a RimWorld mod change

Use the mod's existing versioning convention when one is documented. Before committing or pushing a mod-content change, inspect the staged changes, `About/About.xml`, and the `.csproj` that builds the actual game mod assembly. Repositories may contain separate test or tooling projects; do not pick one by filename alone. Locate the mod project from its assembly name, target framework, and configured output path.

## Choose the version level

Base the change on its effect on existing saves and the mod's supported API or content:

- **MAJOR** for a breaking save or public API change, such as removing or renaming saved definitions, changing serialized data incompatibly, or removing a promised API contract.
- **MINOR** for a backward-compatible feature or new player-facing content, such as adding a building, definition, setting, or endpoint while existing saves and clients continue to work.
- **PATCH** for compatible fixes, balance adjustments, translation or asset updates, and internal refactors.

When changes span levels, use the highest applicable level. Follow the repository's pre-1.0 policy if it has one; otherwise, preserve its established convention rather than imposing a new scheme.

## Apply and verify the bump

1. Read the current mod version and any repo-specific release notes or scripts.
2. Update `<modVersion>` in `About/About.xml` and the `<Version>` in the project that produces the shipped mod assembly. If the build has multiple target configurations, keep their effective assembly versions aligned.
3. Preserve the version format used by the project and make corresponding values match exactly where the repo expects them to match.
4. Rebuild the mod with the `build-mod` skill when a project version changed. The DLL may be generated and ignored in a source-first repository; follow its release convention rather than committing build output by default.
5. Check the staged diff and report the level chosen and the compatibility reason.

The `.claude/hooks/require-version-bump.sh` guard checks mod-content commits on the primary branch and mod-content pushes on other branches. If it blocks an operation, read the reason, update the right mod metadata and project version, rebuild if needed, then retry. Do not bypass the guard just to avoid doing the bump; use its documented bypass only when the content change intentionally requires no release version.
