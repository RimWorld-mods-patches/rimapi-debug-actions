#!/usr/bin/env bash
# Generic RimWorld mod version guard for Claude's Bash pre-tool hook.
# Checks About/About.xml and, when identifiable, the assembly project that outputs to
# an Assemblies directory. See .claude/skills/version-bump/SKILL.md.
set -uo pipefail

ABOUT=About/About.xml
CONTENT_RE='^(About/|Source/|1\.[0-9]+/|Defs/|Patches/|Languages/|Textures/|Sounds/|Assemblies/)'

input=$(cat)
cmd=$(printf '%s' "$input" | jq -r '.tool_input.command // ""')

case "$cmd" in
  *"git commit"*) event=commit ;;
  *"git push"*)   event=push ;;
  *) exit 0 ;;
esac

case "$cmd" in
  *--amend*|*--no-verify*) exit 0 ;;
esac

root=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
cd "$root" || exit 0
git rev-parse HEAD >/dev/null 2>&1 || exit 0

branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)
primary_branch=$(git symbolic-ref --short refs/remotes/origin/HEAD 2>/dev/null | sed 's#^origin/##')
if [ -z "$primary_branch" ]; then
  primary_branch=$(git config --get init.defaultBranch 2>/dev/null || printf 'main')
fi

about_version_at() {
  git show "$1$ABOUT" 2>/dev/null |
    sed -n 's/.*<modVersion>\([^<]*\)<\/modVersion>.*/\1/p' | head -1
}

project_paths_at() {
  if [ "$1" = ":" ]; then
    git ls-files --cached -- '*.csproj'
  else
    git ls-tree -r --name-only "$1" 2>/dev/null | sed -n '/\.csproj$/p'
  fi
}

mod_project_version_at() {
  local revision="$1"
  local project content output_path version
  local -a versions=()

  while IFS= read -r project; do
    [ -n "$project" ] || continue
    content=$(git show "$revision$project" 2>/dev/null) || continue
    output_path=$(printf '%s\n' "$content" | sed -n 's/.*<OutputPath>\([^<]*\)<\/OutputPath>.*/\1/p' | head -1)
    case "$output_path" in
      *Assemblies*|*assemblies*) ;;
      *) continue ;;
    esac

    version=$(printf '%s\n' "$content" | sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' | head -1)
    [ -n "$version" ] && versions+=("$version")
  done <<< "$(project_paths_at "$revision")"

  if [ "${#versions[@]}" -eq 0 ]; then
    return 0
  fi

  local unique_versions
  unique_versions=$(printf '%s\n' "${versions[@]}" | sort -u)
  if [ "$(printf '%s\n' "$unique_versions" | wc -l | tr -d ' ')" -gt 1 ]; then
    printf '__AMBIGUOUS__'
  else
    printf '%s' "$unique_versions"
  fi
}

if [ "$event" = commit ]; then
  # On the primary branch, every content commit carries its own version bump.
  [ "$branch" = "$primary_branch" ] || exit 0
  git diff --cached --name-only | grep -qE "$CONTENT_RE" || exit 0
  version_rev=:
  baseline_rev=HEAD:
  baseline_desc="the previous commit on $primary_branch"
  retry="commit again"
else
  if upstream=$(git rev-parse --abbrev-ref '@{upstream}' 2>/dev/null); then
    baseline=$upstream
    baseline_desc="$upstream"
  elif baseline=$(git merge-base HEAD "$primary_branch" 2>/dev/null) && [ -n "$baseline" ]; then
    baseline_desc="where this branch left $primary_branch"
  else
    exit 0
  fi

  git diff --name-only "$baseline" HEAD 2>/dev/null | grep -qE "$CONTENT_RE" || exit 0
  version_rev=HEAD:
  baseline_rev="$baseline:"
  retry="push again"
fi

new_about=$(about_version_at "$version_rev")
base_about=$(about_version_at "$baseline_rev")
new_project=$(mod_project_version_at "$version_rev")

deny() {
  jq -n --arg r "$1" '{hookSpecificOutput: {hookEventName: "PreToolUse",
    permissionDecision: "deny", permissionDecisionReason: $r}}'
  exit 0
}

if [ -z "$new_about" ]; then
  deny "Could not read <modVersion> from $ABOUT. Fix the mod metadata, then $retry."
fi

if [ "$new_project" = "__AMBIGUOUS__" ]; then
  deny "More than one mod assembly project has a different <Version>. Identify the shipped mod project, synchronize its version with $ABOUT, rebuild, then $retry."
fi

if [ -n "$new_project" ] && [ "$new_about" != "$new_project" ]; then
  deny "The mod version in $ABOUT ($new_about) does not match the <Version> in the mod assembly project ($new_project). Synchronize both, rebuild, then $retry."
fi

if [ "$new_about" = "$base_about" ]; then
  deny "This $event changes mod content but $ABOUT still has version $new_about, unchanged from $baseline_desc. Apply the version-bump skill, rebuild if the assembly project version changes, then $retry."
fi

exit 0
