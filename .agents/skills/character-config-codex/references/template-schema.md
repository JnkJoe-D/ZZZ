# Character Template Cache Schema

## Purpose

Use one cache template file per role folder.

The file serves two jobs:

- describe what should exist
- remember what currently exists

Keep user-editable desired data separate from agent-written resolved data.

## Folder Layout

Recommended layout:

```text
<templateCacheRoot>/
  <RoleName>/
    <RoleName>.character-template.json
```

Example:

```text
Configs/CharacterTemplates/
  安比/
    安比.character-template.json
```

## Top-Level Sections

- `version`: schema version
- `character`: user-editable role-level desired fields
- `hitReaction`: user-editable desired hit-reaction fields
- `params`: optional user-editable helper params
- `actions`: user-editable desired action list, and its text order is meaningful
- `resolved`: agent-maintained snapshot of actual files, ids, and paths

## User-Editable Rules

The user-editable sections are:

- `character`
- `hitReaction`
- `params`
- `actions`

Rules:

- `character.roleName` is required
- `character.actionRootKey` is required
- `actions` must be a non-empty array
- `actions[].key` must be unique within one file
- `actions[].key` order is the default allocation order for newly created actions
- do not put concrete `roleId`, `actionId`, or `id` fields in `character` or `actions`

## Resolved Rules

`resolved` is written by the agent after reconcile/sync.

It may contain:

- concrete `roleId`
- actual role folder path
- actual character asset path
- actual hit-reaction asset path
- resolved action records with concrete `actionId`
- role-local command-set asset paths in `routeSetAssetPaths`
- last sync time
- diff or health markers

The user may inspect `resolved`, but normal edits should happen in the user-editable sections.

## Example

```json
{
  "version": 2,
  "character": {
    "roleName": "新角色",
    "prefabPath": "Assets/Resources/Prefab/Role/新角色/新角色.prefab",
    "actionRootKey": "基础_待机",
    "autoAllocateRoleId": true
  },
  "hitReaction": {
    "enabled": true,
    "lightKey": "受击_轻",
    "heavyKey": "受击_重",
    "knockAwayKey": null
  },
  "params": {
    "normalAttackCount": 4,
    "hasDashAttack": true,
    "hasEvadeForward": true,
    "hasEvadeBackward": true
  },
  "actions": [
    {
      "key": "基础_待机",
      "displayName": "基础_待机",
      "assetType": "LocomotionConfigAsset",
      "group": "base",
      "preload": true
    },
    {
      "key": "普通攻击1",
      "displayName": "普通攻击1",
      "assetType": "SkillConfigAsset",
      "group": "normal_attack",
      "preload": false
    }
  ],
  "resolved": {
    "roleName": "新角色",
    "roleId": 1004,
    "templatePath": "Configs/CharacterTemplates/新角色/新角色.character-template.json",
    "roleFolder": "Assets/Resources/Serializations/ScriptableObjects/Action/新角色",
    "characterAssetPath": "Assets/Resources/Serializations/ScriptableObjects/Action/新角色/新角色.asset",
    "hitReactionAssetPath": "Assets/Resources/Serializations/ScriptableObjects/Action/新角色/新角色受击.asset",
    "routeSetAssetPaths": [
      "Assets/Resources/Serializations/ScriptableObjects/Action/新角色/新角色_指令集_0_0_慢跑起步.asset",
      "Assets/Resources/Serializations/ScriptableObjects/Action/新角色/新角色_指令集_1_0_一段攻击指令.asset"
    ],
    "actions": [
      {
        "key": "基础_待机",
        "actionId": 10600,
        "assetPath": "Assets/Resources/Serializations/ScriptableObjects/Action/新角色/新角色_基础_待机.asset",
        "assetType": "LocomotionConfigAsset"
      },
      {
        "key": "普通攻击1",
        "actionId": 10601,
        "assetPath": "Assets/Resources/Serializations/ScriptableObjects/Action/新角色/新角色_普通攻击1.asset",
        "assetType": "SkillConfigAsset"
      }
    ],
    "lastSyncedUtc": "2026-05-10T09:30:00Z"
  }
}
```

## Validation Notes

Current script validation still targets the desired sections:

- `character.roleName`
- `character.actionRootKey`
- `actions`
- hit-reaction keys
- no concrete ids in `character` and `actions`

`resolved` is intentionally outside the allocation input contract except when the agent uses it to preserve existing ids during edit flows.
