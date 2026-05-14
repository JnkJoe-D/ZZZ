---
name: character-config-codex
description: |-
  Use when working with CharacterConfigAsset, ActionConfigAsset, HitReactionConfig, ActionRouteSetAsset command-set files, per-character template cache folders, or the compact character/action ID registry cache. This skill is for Codex-agent-driven create/edit/sync flows: scaffold a new role template folder from the base template, validate or diff an existing folder template against the actual asset files, allocate RoleID and ActionID values, create or edit per-role command-set assets, sync create/update/delete operations from template to assets, and refresh the compact registry after the write.
---

# Character Config Codex Skill

## Purpose

Use this skill as the **control plane** for character-config work driven by Codex.

Prefer this skill when the task is any of:

- create a new character template folder
- customize and sync one character's config files from a template
- modify an existing role by editing its cached template first
- rebuild one role's template cache from actual files
- create or edit per-role command-set files (`ActionRouteSetAsset`)
- allocate or inspect `RoleID` / `ActionID`
- refresh the compact registry after writes

This skill is for **per-role folder workflows**, not for scanning the whole config tree on every run.

## Core Model

Treat one role as one isolated unit:

- one actual asset folder: `Assets/Resources/Serializations/ScriptableObjects/Action/<RoleName>/`
- one cache template folder: `<templateCacheRoot>/<RoleName>/`
- one cache template file in that folder

The cache template file is the source of truth for routine create/edit work.

The file has two layers:

- `character` / `hitReaction` / `params` / `actions`: user-editable desired state
- `resolved`: agent-maintained snapshot of the actual files that currently exist

Normal runs should read:

1. the compact registry
2. the role's cache template file
3. the role's actual asset folder only when validating or syncing

Only do a full registry rebuild when the registry is missing, stale, corrupted, or explicitly requested.

## Files And Paths

- Registry: `ProjectSettings/Codex/character_action_registry.json`
- Registry setting for cache root: `paths.templateCacheRoot`
- Default cache root if the registry field is missing: `Configs/CharacterTemplates`
- Base template asset to copy for new roles: `.agents/skills/character-config-codex/assets/base.character-template.json`
- Per-role cache template: `<templateCacheRoot>/<RoleName>/<RoleName>.character-template.json`
- Actual role asset root: `Assets/Resources/Serializations/ScriptableObjects/Action/<RoleName>/`
- Helper script: `.agents/skills/character-config-codex/scripts/character_config_tool.py`

If `paths.templateCacheRoot` does not exist in the registry, use the fallback path above and note that the registry should eventually be updated to include the explicit setting.

## Folder Contract

Assume every role's config stays in one dedicated folder.

That folder may contain:

- one `CharacterConfigAsset`
- zero or one hit-reaction asset
- many action assets
- zero or many command-set assets (`ActionRouteSetAsset`)

Mirror that structure in the cache root:

- cache folder name must match the role folder name
- cache template file name should also match the role name
- all per-role operations must stay inside that single role folder

Do not modify sibling role folders during a one-role sync.

## Cache Template Contract

The per-role cache template file is both:

- the editable plan for what should exist
- the persisted snapshot of what currently exists

Use the schema in [template-schema.md](./references/template-schema.md).

Important rules:

- keep concrete `RoleID` / `ActionID` values out of the user-editable `character` and `actions` sections
- store concrete IDs and actual asset paths under `resolved`
- preserve user ordering in `actions`; use that order during new ID allocation
- keep `resolved` in sync after every successful write
- keep command-set asset paths in `resolved.routeSetAssetPaths`

If the user edits `resolved` directly, treat that as suspicious input and verify it against actual files before trusting it.

## Command-Set Scope

`ActionRouteSetAsset` command-set files are now in scope for this skill.

Treat them as per-role reusable route bundles.

Normal command-set work should:

- stay inside the target role folder
- reuse an existing role's command-set naming scheme when one already exists
- preserve references from action assets to command-set assets
- keep command-set asset paths in the role template's `resolved.routeSetAssetPaths`

Preferred file pattern:

- `<RoleName>_指令集_<group>_<index>_<label>.asset`

Common operations:

- create missing command-set assets for a role
- rename command-set assets and keep action references in sync
- retarget `NextAction` references inside one command-set asset
- replace an action asset's `RouteSets` list with the intended role-local command-set set

When the user asks to "edit command-set files", treat that as a request to inspect both:

- the command-set assets themselves
- the action assets that reference them

## Registry Model

The registry remains intentionally compact.

It stores:

- global ID rules
- global `RoleID` max
- per-role `ActionID` max
- optional path settings such as `paths.templateCacheRoot`

It does **not** need to store every asset record because the per-role cache template now holds the detailed per-role snapshot.

The registry is still monotonic by default:

- create/update flows can move maxima forward
- delete flows do not decrease maxima
- exact recomputation still requires a rebuild

## Default Workflow

### 1. Resolve paths and intent

Resolve:

- `roleName`
- cache folder path
- cache template path
- actual asset folder path

Interpret the user request into one of four intents:

- `scaffold-template`: create a new role template folder only
- `sync-create`: create actual assets from a completed template
- `sync-edit`: apply template changes to an existing role
- `rebuild-cache`: reconstruct or repair the cache template from actual files

### 2. Read the registry first

Read `ProjectSettings/Codex/character_action_registry.json`.

If the file does not exist, run:

```powershell
python .agents/skills/character-config-codex/scripts/character_config_tool.py rebuild-registry --project-root .
```

### 3. Validate folder state before touching files

Use this decision table:

- cache folder missing + actual folder missing: treat as new role flow
- cache folder exists + actual folder missing: allow scaffold or create flow
- cache folder missing + actual folder exists: stop normal edit flow and rebuild cache first
- cache folder exists + actual folder exists: normal edit/sync flow

If the user says "modify existing role" but the cache template is missing, rebuild the cache template before editing actual files.

### 4. Validate the template before allocation

Run:

```powershell
python .agents/skills/character-config-codex/scripts/character_config_tool.py validate-template --template <templatePath>
```

Validation should also check:

- cache folder name matches `character.roleName` unless this is an intentional rename
- `resolved.roleName` matches the current folder or is refreshed before sync
- every `resolved.actions[].assetPath` stays inside the one-role folder

### 5. Preview ID allocation before any write

Run:

```powershell
python .agents/skills/character-config-codex/scripts/character_config_tool.py allocate-from-template --registry ProjectSettings/Codex/character_action_registry.json --template <templatePath>
```

Allocation rules:

- for a new role, allocate the next `RoleID`
- for an existing role, preserve `resolved.roleId` when confirmed by actual files
- for existing actions, preserve current `ActionID` when matching by stable action key
- for newly added actions, assign IDs in `actions` list order
- never recycle deleted IDs unless the user explicitly requests a rebuild-and-renumber workflow

### 6. Apply file changes from the template

After preview, create or edit actual files in the one-role folder.

During sync:

- create missing assets declared by the template
- update assets whose template fields changed
- mark assets missing from the template as deletion candidates
- only delete actual files after the diff has been checked against the target role folder
- create or update command-set assets when the target role needs reusable route bundles
- update action-asset `RouteSets` references when command-set membership changes

### 7. Reconcile template and actual files after the write

After file edits:

- rescan only the one-role folder
- rebuild the `resolved` section from the actual files
- verify that the user-editable sections and actual files agree
- verify that allocated IDs match written files

Do not update the registry until this reconciliation succeeds.

### 8. Update the registry last

After a successful sync-create or sync-edit flow, update the compact registry.

For create/update:

```powershell
python .agents/skills/character-config-codex/scripts/character_config_tool.py record-create --registry ProjectSettings/Codex/character_action_registry.json --role-id <roleId> --role-name <roleName> --action-ids <comma-separated-ids>
```

For rename:

```powershell
python .agents/skills/character-config-codex/scripts/character_config_tool.py rename-role --registry ProjectSettings/Codex/character_action_registry.json --role-id <roleId> --role-name <newRoleName>
```

For delete:

- do not decrease cached maxima
- update or rebuild the per-role cache template
- rebuild the registry only if exact recomputation is requested

## Intent-Specific Procedures

### A. Scaffold a new role template

Use this when the user asks for things like:

- `create new role template`
- `scaffold role template`
- `generate template first, I will edit it`

Procedure:

1. Resolve `roleName`.
2. Create `<templateCacheRoot>/<RoleName>/`.
3. Copy `.agents/skills/character-config-codex/assets/base.character-template.json` into `<RoleName>.character-template.json`.
4. Replace the base template placeholders with the resolved role name.
5. Stop after scaffolding unless the user also asked to generate actual assets.

Do **not** allocate IDs or write actual assets during scaffold-only flows.

### B. Create actual assets from a completed template

Use this after the user has customized the scaffolded template and explicitly asks to apply it.

Procedure:

1. Validate the template.
2. Preview the IDs.
3. Create the actual role folder if needed.
4. Create character/hit-reaction/action assets from the template.
5. Write the `resolved` section back into the cache template.
6. Update the registry.

### C. Modify an existing role

Use this when the user edits the existing role's cache template and asks the agent to apply the changes.

Procedure:

1. Read the cache template first.
2. Compare the template's desired state with the current actual folder.
3. Preserve existing IDs when keys still match.
4. Allocate IDs only for truly new actions.
5. Apply updates.
6. Rewrite `resolved`, including `routeSetAssetPaths`.
7. Update the registry if maxima moved forward.

There is no folder-creation step in the normal edit flow.

### D. Rebuild a missing or stale cache template

Use this when the actual role folder exists but the cache template is missing or clearly stale.

Procedure:

1. Scan only the target role folder.
2. Build or repair `<templateCacheRoot>/<RoleName>/<RoleName>.character-template.json`.
3. Populate user-editable sections from the best recoverable structure.
4. Populate `resolved` from actual files.
5. Preserve file order as discovered unless the user requests reordering.

If reconstruction would require guessing too much semantic data, tell the user what was recovered and what still needs manual completion.

## ID Rules

Default rules:

- `RoleID` range: `1000-1999`
- `FirstActionMappedRoleId`: `1001`
- `ActionID` range: `10000-99999`
- per-role action block size: `200`

Action range formula:

- `roleOffset = roleId - firstActionMappedRoleId`
- `actionStart = actionIdRange.start + roleOffset * actionBlockSize`
- `actionEnd = actionStart + actionBlockSize - 1`

Default allocation behavior:

- next role id = `roleIdMax + 1`
- next action id = `roleActionMax + 1`
- if no role action max exists, use that role's block start
- assign new action ids in template text order

Do not renumber stable existing actions just because the template order changed.

## Prompt And Command Conventions

Interpret common user requests like this:

- `create new role template`: scaffold the cache folder and base template only
- `create role config from template`: scaffold if needed, then validate, allocate, write actual assets, reconcile, update registry
- `modify role config`: read the existing cache template first, then sync to actual files
- `edit command-set files`: inspect or edit one role's `ActionRouteSetAsset` files and the `RouteSets` references on related action assets
- `rebuild role template cache`: derive or repair the per-role cache template from actual files
- `edit template only`: edit the cache template only and stop before asset writes
- `reconcile template with actual files`: run reconciliation only, then rewrite `resolved` if needed

When the intent is ambiguous, ask only the smallest clarifying question needed to choose between:

- template-only work
- actual asset sync
- cache rebuild

## Safety Rules

Follow these guardrails to avoid destructive mistakes:

- Never write actual assets for a role until the cache template path and actual role folder path are both explicit.
- Never create, edit, delete, or rename files outside the target role folder in a one-role flow.
- Never retarget `NextAction` in a command-set asset to another role's action asset.
- Never overwrite an existing cache template folder during a "new role" request without confirming whether it should be reused or rebuilt.
- Never trust `resolved` blindly; verify it against actual files before allocation or deletion.
- Never delete actual files solely because the template omitted them until the candidate deletions are confirmed to belong to the same role folder.
- Never leave an action asset pointing at another role's command-set guid after a copy-based create flow.
- Never decrease registry maxima during delete flows.
- Never silently convert a role rename into "create a new role" or vice versa; if `character.roleName` changes against an existing folder, confirm whether this is a rename.
- If the planned sync includes deletes, folder rename, or more than a handful of file writes, summarize the create/update/delete set before applying it.
- If the cache template and actual files disagree after the write, stop, report the mismatch, and repair the cache template before claiming success.

## Notes For The Agent

- Prefer the helper script over manual registry math.
- Prefer `rg` for targeted inspection.
- Prefer per-role scans over project-wide scans.
- Treat the cache template file as the routine working document.
- Treat actual files as the final authority when the template snapshot is stale.
- Read [template-schema.md](./references/template-schema.md) before changing the cache template structure.
