---
name: use-rimapi-debug-actions
description: Use RIMAPI's debug-actions extension to discover, filter, and invoke supported RimWorld debug actions from HTTP clients or automation.
---

# Use the RIMAPI debug-actions API

Use this skill when automating or troubleshooting the RIMAPI debug-actions extension. It applies to debug actions from any loaded RimWorld mod; do not assume an action belongs to a particular mod.

## Requirements

- RimWorld must be running with a game loaded and RIMAPI enabled.
- The debug-actions companion mod must be enabled after RIMAPI.
- Use RIMAPI's configured HTTP address and port. Its default is `http://localhost:8765`; it can be changed in RIMAPI settings.
- The extension is namespaced under `/api/extensions/debug-actions`.

## Discover and filter actions

List all actions:

```sh
curl --fail-with-body 'http://localhost:8765/api/extensions/debug-actions/debug-actions'
```

Filter by a case-insensitive substring in the action name, category, assembly, declaring type, signature, or ID:

```sh
curl --fail-with-body --get \
  --data-urlencode 'filter=Realistic Bases' \
  'http://localhost:8765/api/extensions/debug-actions/debug-actions'
```

Each action record includes a stable `id`, `name`, `category`, `assembly`, `declaring_type`, `signature`, and `is_runnable`. Use the returned ID as the execution selector; do not derive it from the display name. IDs are URL-safe Base64 identifiers and should still be passed as URL-encoded query values.

## Invoke a listed action

Only run an action when its record has `is_runnable: true`. Invoke it with POST and an empty JSON object body:

```sh
curl --fail-with-body -X POST \
  -H 'Content-Type: application/json' \
  --data '{}' \
  'http://localhost:8765/api/extensions/debug-actions/debug-actions/run?id=ACTION_ID_FROM_LIST'
```

RIMAPI requires a request body on POST requests; `{}` provides the required content length. A successful response contains `success: true` and the action record. Invocation errors contain `success: false` and an error string.

## Supported actions and effects

The extension discovers `[DebugAction]` methods in loaded assemblies. It directly invokes parameterless, static methods returning `void`. It reports other discovered actions with `is_runnable: false`; these can require a cell, a target, an instance, a menu selection, or other input that this endpoint does not provide.

Running an action executes that mod's code in the live game and can change the world or game state. Check its category and signature, use a development save, and invoke only the intended action. The endpoint is an automation interface, not a safety filter.

If the extension route is missing, confirm both mods are enabled, restart/load a game so RIMAPI initializes, and check RimWorld's log for extension registration errors. If no actions appear, check that the desired mods are loaded and that their methods use RimWorld's `[DebugAction]` attribute.
