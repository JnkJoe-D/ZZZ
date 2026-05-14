#!/usr/bin/env python3
from __future__ import annotations

import argparse
import copy
import json
import re
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ACTION_ASSET_ROOT = "Assets/Resources/Serializations/ScriptableObjects/Action"
DEFAULT_TEMPLATE_CACHE_ROOT = "Configs/CharacterTemplates"
TEMPLATE_FILE_SUFFIX = ".character-template.json"

SCRIPT_GUID_CHARACTER = "fc06e0520cbb132459d8e63f748b257e"
SCRIPT_GUID_HIT_REACTION = "cc907aef54d1b934298be1107be47939"
SCRIPT_GUID_SKILL = "429a74382b9739749b6c5893828d2a4b"
SCRIPT_GUID_LOCOMOTION = "f30efc83f55cddf4e95a3a907679c0b3"
SCRIPT_GUID_ROUTE_SET = "7e2dbc65d716c54479ad3f5268d89ffc"

ACTION_SCRIPT_GUIDS = {
    SCRIPT_GUID_SKILL: "SkillConfigAsset",
    SCRIPT_GUID_LOCOMOTION: "LocomotionConfigAsset",
}

SCRIPT_GUID_TO_KIND = {
    SCRIPT_GUID_CHARACTER: "CharacterConfigAsset",
    SCRIPT_GUID_HIT_REACTION: "HitReactionConfig",
    SCRIPT_GUID_SKILL: "SkillConfigAsset",
    SCRIPT_GUID_LOCOMOTION: "LocomotionConfigAsset",
    SCRIPT_GUID_ROUTE_SET: "ActionRouteSetAsset",
}

DEFAULT_RULES = {
    "roleIdRange": {"start": 1000, "end": 1999},
    "firstActionMappedRoleId": 1001,
    "actionIdRange": {"start": 10000, "end": 99999},
    "actionBlockSize": 200,
}

DEFAULT_REGISTRY = {
    "version": 1,
    "rules": copy.deepcopy(DEFAULT_RULES),
    "paths": {
        "templateCacheRoot": DEFAULT_TEMPLATE_CACHE_ROOT,
    },
    "allocators": {
        "roleId": {
            "hasAllocated": False,
            "maxAllocated": 0,
        }
    },
    "roleActionAllocators": [],
    "stats": {
        "characterCount": 0,
        "actionCount": 0,
        "hitReactionCount": 0,
        "lastRebuildUtc": None,
    },
}

ROLE_ID_RE = re.compile(r"^\s*RoleID:\s*(\d+)\s*$", re.MULTILINE)
ACTION_ID_RE = re.compile(r"^\s*ID:\s*(\d+)\s*$", re.MULTILINE)
SCRIPT_GUID_RE = re.compile(r"^\s*m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-f]+),\s*type:\s*3\}\s*$", re.MULTILINE)
META_GUID_RE = re.compile(r"^guid:\s*([0-9a-f]+)\s*$", re.MULTILINE)


@dataclass
class ScanIssue:
    severity: str
    message: str
    path: str | None = None


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def save_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")


def write_text(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8")


def relative_unix(path: Path, root: Path) -> str:
    return str(path.relative_to(root)).replace("\\", "/")


def natural_key(text: str) -> list[Any]:
    parts = re.split(r"(\d+)", text)
    key: list[Any] = []
    for part in parts:
        if part.isdigit():
            key.append(int(part))
        else:
            key.append(part)
    return key


def ensure_registry_shape(payload: dict[str, Any] | None) -> dict[str, Any]:
    registry = copy.deepcopy(DEFAULT_REGISTRY)
    if payload:
        registry.update(payload)
    registry["rules"] = {**copy.deepcopy(DEFAULT_RULES), **(payload.get("rules", {}) if payload else {})}

    paths = registry.get("paths") or {}
    registry["paths"] = {
        "templateCacheRoot": str(paths.get("templateCacheRoot") or DEFAULT_TEMPLATE_CACHE_ROOT).replace("\\", "/"),
    }

    allocators = registry.get("allocators") or {}
    role_alloc = allocators.get("roleId") or {}
    allocators["roleId"] = {
        "hasAllocated": bool(role_alloc.get("hasAllocated", False)),
        "maxAllocated": int(role_alloc.get("maxAllocated", 0)),
    }
    registry["allocators"] = allocators

    role_action_allocators = registry.get("roleActionAllocators") or []
    normalized_allocators: list[dict[str, Any]] = []
    for item in role_action_allocators:
        if not isinstance(item, dict):
            continue
        action_range = item.get("actionRange") or {}
        normalized_allocators.append(
            {
                "roleId": int(item.get("roleId", 0)),
                "roleName": item.get("roleName") or "",
                "actionRange": {
                    "start": int(action_range.get("start", 0)),
                    "end": int(action_range.get("end", 0)),
                },
                "hasAllocated": bool(item.get("hasAllocated", False)),
                "maxAllocated": int(item.get("maxAllocated", 0)),
            }
        )
    registry["roleActionAllocators"] = normalized_allocators

    stats = registry.get("stats") or {}
    registry["stats"] = {
        "characterCount": int(stats.get("characterCount", 0)),
        "actionCount": int(stats.get("actionCount", 0)),
        "hitReactionCount": int(stats.get("hitReactionCount", 0)),
        "lastRebuildUtc": stats.get("lastRebuildUtc"),
    }
    return registry


def fresh_registry_from_payload(payload: dict[str, Any] | None) -> dict[str, Any]:
    registry = ensure_registry_shape(payload)
    registry["allocators"] = {
        "roleId": {
            "hasAllocated": False,
            "maxAllocated": 0,
        }
    }
    registry["roleActionAllocators"] = []
    registry["stats"] = {
        "characterCount": 0,
        "actionCount": 0,
        "hitReactionCount": 0,
        "lastRebuildUtc": None,
    }
    return registry


def registry_path_from_args(args: argparse.Namespace) -> Path:
    if args.registry:
        return Path(args.registry)
    return Path(args.project_root) / "ProjectSettings" / "Codex" / "character_action_registry.json"


def character_asset_root(project_root: Path) -> Path:
    return project_root / ACTION_ASSET_ROOT


def template_root_from_registry(project_root: Path, registry: dict[str, Any], override: str | None = None) -> Path:
    raw = override or registry.get("paths", {}).get("templateCacheRoot") or DEFAULT_TEMPLATE_CACHE_ROOT
    root = Path(raw)
    if not root.is_absolute():
        root = project_root / root
    return root.resolve()


def set_registry_template_root(registry: dict[str, Any], project_root: Path, template_root: Path) -> None:
    registry.setdefault("paths", {})
    try:
        registry["paths"]["templateCacheRoot"] = relative_unix(template_root, project_root)
    except ValueError:
        registry["paths"]["templateCacheRoot"] = str(template_root).replace("\\", "/")


def role_name_from_path(path: Path) -> str:
    return path.parent.name


def role_action_range(rules: dict[str, Any], role_id: int) -> tuple[int, int]:
    role_range = rules["roleIdRange"]
    action_range = rules["actionIdRange"]
    first_mapped = int(rules["firstActionMappedRoleId"])
    block_size = int(rules["actionBlockSize"])

    if role_id < first_mapped:
        raise ValueError(f"RoleID {role_id} is smaller than firstActionMappedRoleId {first_mapped}.")

    if role_id < int(role_range["start"]) or role_id > int(role_range["end"]):
        raise ValueError(f"RoleID {role_id} is outside role range [{role_range['start']}, {role_range['end']}].")

    offset = role_id - first_mapped
    start = int(action_range["start"]) + offset * block_size
    end = start + block_size - 1

    if start < int(action_range["start"]) or end > int(action_range["end"]):
        raise ValueError(f"RoleID {role_id} action range exceeds global action range.")

    return start, end


def parse_id(text: str, pattern: re.Pattern[str]) -> int | None:
    match = pattern.search(text)
    if not match:
        return None
    return int(match.group(1))


def parse_script_guid(text: str) -> str | None:
    match = SCRIPT_GUID_RE.search(text)
    return match.group(1) if match else None


def parse_meta_guid(meta_path: Path) -> str | None:
    if not meta_path.exists():
        return None
    match = META_GUID_RE.search(read_text(meta_path))
    return match.group(1) if match else None


def parse_asset_ref_guid(text: str, field_name: str) -> str | None:
    match = re.search(
        rf"^\s*{re.escape(field_name)}:\s*\{{fileID:\s*(\d+),\s*guid:\s*([0-9a-f]+),\s*type:\s*\d+\}}\s*$",
        text,
        re.MULTILINE,
    )
    if not match:
        return None
    if match.group(1) == "0":
        return None
    return match.group(2)


def parse_asset_ref_guid_list(text: str, field_name: str) -> list[str]:
    lines = text.splitlines()
    result: list[str] = []
    capture = False
    field_prefix = f"  {field_name}:"
    for line in lines:
        if not capture:
            if line == field_prefix:
                capture = True
            continue

        if line.startswith("  - "):
            match = re.search(r"guid:\s*([0-9a-f]+)", line)
            if match:
                result.append(match.group(1))
            continue

        if line.startswith("  "):
            break

    return result


def replace_numeric_field(text: str, field_name: str, value: int) -> str:
    pattern = re.compile(rf"(^\s*{re.escape(field_name)}:\s*)(\d+)(\s*$)", re.MULTILINE)
    replaced, count = pattern.subn(rf"\g<1>{value}\g<3>", text, count=1)
    if count != 1:
        raise ValueError(f"Unable to replace numeric field '{field_name}'.")
    return replaced


def detect_hit_reaction(text: str) -> bool:
    return "hitAnimLight:" in text or "hitAnimHeavy:" in text or "hitAnimKnowAway:" in text


def derive_action_key(role_name: str, asset_path: Path) -> str:
    stem = asset_path.stem
    prefix = role_name + "_"
    if stem.startswith(prefix):
        return stem[len(prefix):]
    return stem


def build_project_guid_index(project_root: Path) -> dict[str, str]:
    index: dict[str, str] = {}
    for meta_path in (project_root / "Assets").rglob("*.meta"):
        guid = parse_meta_guid(meta_path)
        if not guid:
            continue
        asset_path = meta_path.with_suffix("")
        index[guid] = relative_unix(asset_path, project_root)
    return index


def role_folder_from_name(project_root: Path, role_name: str) -> Path:
    return character_asset_root(project_root) / role_name


def role_template_path(template_root: Path, role_name: str) -> Path:
    return template_root / role_name / f"{role_name}{TEMPLATE_FILE_SUFFIX}"


def discover_role_folders(project_root: Path) -> list[Path]:
    root = character_asset_root(project_root)
    if not root.exists():
        return []

    discovered: list[tuple[int, str, Path]] = []
    for role_dir in sorted([path for path in root.iterdir() if path.is_dir()], key=lambda item: natural_key(item.name)):
        role_id = None
        for asset_path in role_dir.glob("*.asset"):
            text = read_text(asset_path)
            if parse_script_guid(text) == SCRIPT_GUID_CHARACTER:
                role_id = parse_id(text, ROLE_ID_RE)
                break
        if role_id is None:
            continue
        discovered.append((role_id, role_dir.name, role_dir))

    discovered.sort(key=lambda item: (item[0], natural_key(item[1])))
    return [item[2] for item in discovered]


def build_role_snapshot(project_root: Path, role_dir: Path, project_guid_index: dict[str, str]) -> dict[str, Any]:
    role_name = role_dir.name
    folder_guid_to_asset: dict[str, Path] = {}
    role_asset_path: Path | None = None
    hit_asset_path: Path | None = None
    route_set_assets: list[str] = []
    action_assets: list[dict[str, Any]] = []

    for asset_path in sorted(role_dir.glob("*.asset"), key=lambda item: natural_key(item.name)):
        text = read_text(asset_path)
        script_guid = parse_script_guid(text)
        meta_guid = parse_meta_guid(asset_path.with_suffix(asset_path.suffix + ".meta"))
        if meta_guid:
            folder_guid_to_asset[meta_guid] = asset_path

        if script_guid == SCRIPT_GUID_CHARACTER:
            role_asset_path = asset_path
            continue

        if script_guid == SCRIPT_GUID_HIT_REACTION:
            hit_asset_path = asset_path
            continue

        if script_guid == SCRIPT_GUID_ROUTE_SET:
            route_set_assets.append(relative_unix(asset_path, project_root))
            continue

        action_id = parse_id(text, ACTION_ID_RE)
        if script_guid in ACTION_SCRIPT_GUIDS and action_id is not None:
            action_assets.append(
                {
                    "path": asset_path,
                    "text": text,
                    "guid": meta_guid,
                    "scriptGuid": script_guid,
                    "assetType": ACTION_SCRIPT_GUIDS[script_guid],
                    "actionId": action_id,
                    "key": derive_action_key(role_name, asset_path),
                }
            )

    if role_asset_path is None:
        raise FileNotFoundError(f"Role folder '{role_dir}' does not contain a CharacterConfigAsset.")

    role_text = read_text(role_asset_path)
    role_id = parse_id(role_text, ROLE_ID_RE)
    if role_id is None:
        raise ValueError(f"Character asset '{role_asset_path}' is missing RoleID.")

    action_assets.sort(key=lambda item: (int(item["actionId"]), natural_key(str(item["key"]))))
    action_by_guid = {item["guid"]: item for item in action_assets if item.get("guid")}

    action_root_guid = parse_asset_ref_guid(role_text, "ActionRoot")
    preload_guids = set(parse_asset_ref_guid_list(role_text, "ActionProLoadList"))
    character_prefab_guid = parse_asset_ref_guid(role_text, "CharacterPrefab")

    action_root_key = action_by_guid.get(action_root_guid, {}).get("key") if action_root_guid else None
    if not action_root_key and action_assets:
        action_root_key = action_assets[0]["key"]

    hit_payload: dict[str, Any] = {
        "enabled": hit_asset_path is not None,
        "lightKey": None,
        "heavyKey": None,
        "knockAwayKey": None,
    }
    hit_asset_rel: str | None = None
    if hit_asset_path is not None:
        hit_asset_rel = relative_unix(hit_asset_path, project_root)
        hit_text = read_text(hit_asset_path)
        for field_name, target_key in (
            ("hitAnimLight", "lightKey"),
            ("hitAnimHeavy", "heavyKey"),
            ("hitAnimKnowAway", "knockAwayKey"),
        ):
            guid = parse_asset_ref_guid(hit_text, field_name)
            if guid and guid in action_by_guid:
                hit_payload[target_key] = action_by_guid[guid]["key"]

    prefab_path = project_guid_index.get(character_prefab_guid) if character_prefab_guid else None

    action_entries: list[dict[str, Any]] = []
    resolved_actions: list[dict[str, Any]] = []
    for item in action_assets:
        action_entries.append(
            {
                "key": item["key"],
                "displayName": item["key"],
                "assetType": item["assetType"],
                "group": "locomotion" if item["assetType"] == "LocomotionConfigAsset" else "action",
                "preload": bool(item.get("guid") in preload_guids),
            }
        )
        resolved_actions.append(
            {
                "key": item["key"],
                "actionId": item["actionId"],
                "assetPath": relative_unix(item["path"], project_root),
                "assetType": item["assetType"],
            }
        )

    return {
        "roleName": role_name,
        "roleDir": role_dir,
        "roleId": role_id,
        "roleAssetPath": role_asset_path,
        "template": {
            "version": 2,
            "character": {
                "roleName": role_name,
                "prefabPath": prefab_path,
                "actionRootKey": action_root_key,
                "autoAllocateRoleId": True,
            },
            "hitReaction": hit_payload,
            "params": {},
            "actions": action_entries,
            "resolved": {
                "roleName": role_name,
                "roleId": role_id,
                "templatePath": None,
                "roleFolder": relative_unix(role_dir, project_root),
                "characterAssetPath": relative_unix(role_asset_path, project_root),
                "hitReactionAssetPath": hit_asset_rel,
                "actions": resolved_actions,
                "routeSetAssetPaths": route_set_assets,
                "lastSyncedUtc": utc_now_iso(),
            },
        },
    }


def build_role_template(project_root: Path, role_dir: Path, template_root: Path, project_guid_index: dict[str, str]) -> dict[str, Any]:
    snapshot = build_role_snapshot(project_root, role_dir, project_guid_index)
    role_name = snapshot["roleName"]
    template_path = role_template_path(template_root, role_name)
    snapshot["template"]["resolved"]["templatePath"] = relative_unix(template_path, project_root)
    template_path.parent.mkdir(parents=True, exist_ok=True)
    save_json(template_path, snapshot["template"])

    return {
        "roleName": role_name,
        "roleId": snapshot["roleId"],
        "templatePath": relative_unix(template_path, project_root),
        "actionCount": len(snapshot["template"]["actions"]),
    }


def build_all_templates(project_root: Path, template_root: Path) -> dict[str, Any]:
    project_guid_index = build_project_guid_index(project_root)
    results: list[dict[str, Any]] = []
    for role_dir in discover_role_folders(project_root):
        results.append(build_role_template(project_root, role_dir, template_root, project_guid_index))
    return {
        "templateRoot": relative_unix(template_root, project_root),
        "roles": results,
    }


def validate_template(template: dict[str, Any]) -> list[str]:
    errors: list[str] = []

    character = template.get("character") or {}
    actions = template.get("actions")
    hit_reaction = template.get("hitReaction") or {}

    if not character.get("roleName"):
        errors.append("character.roleName is required.")

    if not character.get("actionRootKey"):
        errors.append("character.actionRootKey is required.")

    if not isinstance(actions, list) or len(actions) == 0:
        errors.append("actions must be a non-empty array.")
        return errors

    key_set: set[str] = set()
    for index, action in enumerate(actions):
        if not isinstance(action, dict):
            errors.append(f"actions[{index}] must be an object.")
            continue

        key = action.get("key")
        if not key:
            errors.append(f"actions[{index}].key is required.")
            continue

        if key in key_set:
            errors.append(f"Duplicate action key: {key}")
            continue

        key_set.add(key)

        if "id" in action or "roleId" in action or "actionId" in action:
            errors.append(f"Action '{key}' must not declare concrete IDs.")

    root_key = character.get("actionRootKey")
    if root_key and root_key not in key_set:
        errors.append(f"character.actionRootKey '{root_key}' is not present in actions[].key.")

    if bool(hit_reaction.get("enabled", True)):
        for field_name in ("lightKey", "heavyKey", "knockAwayKey"):
            ref = hit_reaction.get(field_name)
            if ref and ref not in key_set:
                errors.append(f"hitReaction.{field_name} '{ref}' is not present in actions[].key.")

    if "roleId" in character:
        errors.append("character.roleId is not allowed in compact template mode.")

    return errors


def allocate_from_template(registry: dict[str, Any], template: dict[str, Any], requested_role_id: int | None, requested_role_name: str | None) -> dict[str, Any]:
    errors = validate_template(template)
    if errors:
        raise ValueError("Template validation failed: " + "; ".join(errors))

    rules = registry["rules"]
    role_alloc = registry["allocators"]["roleId"]
    actions = template["actions"]
    character = template["character"]
    resolved = template.get("resolved") or {}

    role_name = requested_role_name or character["roleName"]
    default_role_start = max(int(rules["roleIdRange"]["start"]), int(rules["firstActionMappedRoleId"]))

    if requested_role_id is not None:
        role_id = requested_role_id
    elif resolved.get("roleId"):
        role_id = int(resolved["roleId"])
    elif character.get("autoAllocateRoleId", True):
        role_id = (int(role_alloc["maxAllocated"]) + 1) if bool(role_alloc["hasAllocated"]) else default_role_start
    else:
        raise ValueError("Template requires manual roleId, but compact template mode does not declare one.")

    start, end = role_action_range(rules, role_id)

    resolved_actions_by_key = {
        str(item.get("key")): int(item["actionId"])
        for item in resolved.get("actions", [])
        if item.get("key") and item.get("actionId") is not None
    }
    preserved_ids = [action_id for _, action_id in sorted(resolved_actions_by_key.items(), key=lambda item: natural_key(item[0]))]

    next_action = start
    if preserved_ids:
        next_action = max(max(preserved_ids) + 1, start)

    action_ids: list[int] = []
    for action in actions:
        key = action["key"]
        existing_id = resolved_actions_by_key.get(key)
        if existing_id is not None:
            action_ids.append(existing_id)
            continue
        if next_action > end:
            raise ValueError(f"Action allocation exceeds role action range [{start}, {end}] for RoleID {role_id}.")
        action_ids.append(next_action)
        next_action += 1

    resulting_role_max = role_id if (not role_alloc["hasAllocated"] or role_id > role_alloc["maxAllocated"]) else int(role_alloc["maxAllocated"])
    resulting_action_max = max(action_ids) if action_ids else 0

    return {
        "roleId": role_id,
        "roleName": role_name,
        "actionRange": {"start": start, "end": end},
        "actionIds": action_ids,
        "resultingRoleIdMax": resulting_role_max,
        "resultingActionIdMax": resulting_action_max,
    }


def record_create(registry: dict[str, Any], role_id: int, role_name: str, action_ids: list[int]) -> dict[str, Any]:
    start, end = role_action_range(registry["rules"], role_id)

    role_alloc = registry["allocators"]["roleId"]
    if (not role_alloc["hasAllocated"]) or role_id > int(role_alloc["maxAllocated"]):
        role_alloc["hasAllocated"] = True
        role_alloc["maxAllocated"] = role_id

    role_bucket = None
    for item in registry["roleActionAllocators"]:
        if int(item.get("roleId", 0)) == role_id:
            role_bucket = item
            break

    if role_bucket is None:
        role_bucket = {
            "roleId": role_id,
            "roleName": role_name,
            "actionRange": {"start": start, "end": end},
            "hasAllocated": False,
            "maxAllocated": 0,
        }
        registry["roleActionAllocators"].append(role_bucket)

    role_bucket["roleName"] = role_name
    role_bucket["actionRange"] = {"start": start, "end": end}

    for action_id in action_ids:
        if action_id < start or action_id > end:
            raise ValueError(f"ActionID {action_id} is outside role action range [{start}, {end}] for RoleID {role_id}.")
        if (not role_bucket["hasAllocated"]) or action_id > int(role_bucket["maxAllocated"]):
            role_bucket["hasAllocated"] = True
            role_bucket["maxAllocated"] = action_id

    return registry


def rename_role(registry: dict[str, Any], role_id: int, role_name: str) -> dict[str, Any]:
    for item in registry["roleActionAllocators"]:
        if int(item.get("roleId", 0)) == role_id:
            item["roleName"] = role_name
            return registry
    raise ValueError(f"RoleID {role_id} does not exist in registry.")


def parse_action_ids(raw: str) -> list[int]:
    if not raw.strip():
        return []
    return [int(part.strip()) for part in raw.split(",") if part.strip()]


def reassign_ids_for_role(project_root: Path, template_path: Path, role_id: int, action_start: int) -> dict[str, Any]:
    template = load_json(template_path)
    errors = validate_template(template)
    if errors:
        raise ValueError(f"Template validation failed for {template_path}: {'; '.join(errors)}")

    role_name = str(template["character"]["roleName"])
    role_dir = role_folder_from_name(project_root, role_name)
    role_asset_path = role_dir / f"{role_name}.asset"
    if not role_asset_path.exists():
        raise FileNotFoundError(f"Role asset does not exist: {role_asset_path}")

    resolved = template.setdefault("resolved", {})
    resolved_actions = resolved.get("actions") or []
    resolved_by_key = {
        str(item.get("key")): item
        for item in resolved_actions
        if item.get("key") and item.get("assetPath")
    }

    updated_resolved_actions: list[dict[str, Any]] = []
    action_writes: list[dict[str, Any]] = []

    for offset, action in enumerate(template["actions"]):
        key = str(action["key"])
        resolved_item = resolved_by_key.get(key)
        if not resolved_item:
            raise ValueError(f"Resolved action record is missing for key '{key}' in template {template_path}.")

        asset_path = project_root / Path(str(resolved_item["assetPath"]))
        if not asset_path.exists():
            raise FileNotFoundError(f"Action asset does not exist: {asset_path}")

        new_action_id = action_start + offset
        action_text = read_text(asset_path)
        action_text = replace_numeric_field(action_text, "ID", new_action_id)
        write_text(asset_path, action_text)

        asset_type = action.get("assetType") or resolved_item.get("assetType")
        updated_resolved_actions.append(
            {
                "key": key,
                "actionId": new_action_id,
                "assetPath": relative_unix(asset_path, project_root),
                "assetType": asset_type,
            }
        )
        action_writes.append(
            {
                "key": key,
                "assetPath": relative_unix(asset_path, project_root),
                "actionId": new_action_id,
            }
        )

    role_text = read_text(role_asset_path)
    role_text = replace_numeric_field(role_text, "RoleID", role_id)
    write_text(role_asset_path, role_text)

    resolved["roleName"] = role_name
    resolved["roleId"] = role_id
    resolved["templatePath"] = relative_unix(template_path, project_root)
    resolved["roleFolder"] = relative_unix(role_dir, project_root)
    resolved["characterAssetPath"] = relative_unix(role_asset_path, project_root)
    resolved["actions"] = updated_resolved_actions
    resolved["lastSyncedUtc"] = utc_now_iso()
    save_json(template_path, template)

    return {
        "roleName": role_name,
        "roleId": role_id,
        "roleAssetPath": relative_unix(role_asset_path, project_root),
        "actionRangeStart": action_start,
        "actionRangeEnd": action_start + len(updated_resolved_actions) - 1 if updated_resolved_actions else action_start - 1,
        "actions": action_writes,
    }


def reassign_all_ids(project_root: Path, template_root: Path, registry: dict[str, Any]) -> dict[str, Any]:
    rules = registry["rules"]
    role_dirs = discover_role_folders(project_root)
    role_start = max(int(rules["roleIdRange"]["start"]), int(rules["firstActionMappedRoleId"]))

    results: list[dict[str, Any]] = []
    for index, role_dir in enumerate(role_dirs):
        role_name = role_dir.name
        template_path = role_template_path(template_root, role_name)
        if not template_path.exists():
            raise FileNotFoundError(f"Template does not exist for role '{role_name}': {template_path}")
        role_id = role_start + index
        action_start, _ = role_action_range(rules, role_id)
        results.append(reassign_ids_for_role(project_root, template_path, role_id, action_start))

    return {
        "templateRoot": relative_unix(template_root, project_root),
        "roles": results,
    }


def rebuild_registry(project_root: Path, registry_path: Path, template_root_override: str | None = None) -> dict[str, Any]:
    root = character_asset_root(project_root)
    existing_payload = load_json(registry_path) if registry_path.exists() else None
    registry = fresh_registry_from_payload(existing_payload)
    issues: list[ScanIssue] = []

    if not root.exists():
        raise FileNotFoundError(f"Character asset root does not exist: {root}")

    template_root = template_root_from_registry(project_root, registry, template_root_override)
    set_registry_template_root(registry, project_root, template_root)

    role_assets: dict[str, dict[str, Any]] = {}
    action_assets: list[dict[str, Any]] = []
    role_id_paths: dict[int, list[str]] = {}
    action_id_paths: dict[int, list[str]] = {}

    for asset_path in root.rglob("*.asset"):
        text = read_text(asset_path)
        role_id = parse_id(text, ROLE_ID_RE)
        action_id = parse_id(text, ACTION_ID_RE)
        relative_path = relative_unix(asset_path, project_root)

        if role_id is not None:
            role_name = role_name_from_path(asset_path)
            role_assets[relative_path] = {
                "path": relative_path,
                "roleId": role_id,
                "roleName": role_name,
            }
            role_id_paths.setdefault(role_id, []).append(relative_path)
            registry["stats"]["characterCount"] += 1
            role_alloc = registry["allocators"]["roleId"]
            if (not role_alloc["hasAllocated"]) or role_id > role_alloc["maxAllocated"]:
                role_alloc["hasAllocated"] = True
                role_alloc["maxAllocated"] = role_id
            continue

        if action_id is not None:
            action_assets.append(
                {
                    "path": relative_path,
                    "actionId": action_id,
                    "roleFolder": asset_path.parent.name,
                }
            )
            action_id_paths.setdefault(action_id, []).append(relative_path)
            registry["stats"]["actionCount"] += 1
            continue

        if detect_hit_reaction(text):
            registry["stats"]["hitReactionCount"] += 1

    folder_role_map: dict[str, dict[str, Any]] = {}
    for role in role_assets.values():
        folder_role_map[Path(role["path"]).parent.name] = role
        try:
            start, end = role_action_range(registry["rules"], role["roleId"])
        except ValueError as exc:
            issues.append(ScanIssue("error", str(exc), role["path"]))
            continue

        registry["roleActionAllocators"].append(
            {
                "roleId": role["roleId"],
                "roleName": role["roleName"],
                "actionRange": {"start": start, "end": end},
                "hasAllocated": False,
                "maxAllocated": 0,
            }
        )

    allocators_by_role: dict[int, dict[str, Any]] = {
        entry["roleId"]: entry for entry in registry["roleActionAllocators"]
    }

    for action in action_assets:
        role_info = folder_role_map.get(action["roleFolder"])
        if not role_info:
            issues.append(
                ScanIssue(
                    "warning",
                    f"Unable to map action asset to role folder '{action['roleFolder']}'.",
                    action["path"],
                )
            )
            continue

        role_id = role_info["roleId"]
        allocator = allocators_by_role.get(role_id)
        if not allocator:
            continue

        start = allocator["actionRange"]["start"]
        end = allocator["actionRange"]["end"]
        action_id = action["actionId"]
        if action_id < start or action_id > end:
            issues.append(
                ScanIssue(
                    "warning",
                    f"ActionID {action_id} is outside role action range [{start}, {end}].",
                    action["path"],
                )
            )
            continue

        if (not allocator["hasAllocated"]) or action_id > allocator["maxAllocated"]:
            allocator["hasAllocated"] = True
            allocator["maxAllocated"] = action_id

    for role_id, paths in sorted(role_id_paths.items()):
        if len(paths) > 1:
            issues.append(ScanIssue("error", f"Duplicate RoleID {role_id}: {', '.join(paths)}"))

    for action_id, paths in sorted(action_id_paths.items()):
        if len(paths) > 1:
            issues.append(ScanIssue("error", f"Duplicate ActionID {action_id}: {', '.join(paths)}"))

    registry["stats"]["lastRebuildUtc"] = utc_now_iso()
    save_json(registry_path, registry)

    return {
        "registryPath": str(registry_path).replace("\\", "/"),
        "registry": registry,
        "issues": [issue.__dict__ for issue in issues],
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Character config compact registry helper for Codex skill.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    rebuild = subparsers.add_parser("rebuild-registry")
    rebuild.add_argument("--project-root", default=".")
    rebuild.add_argument("--registry")
    rebuild.add_argument("--template-root")

    show = subparsers.add_parser("show-registry")
    show.add_argument("--project-root", default=".")
    show.add_argument("--registry")

    validate = subparsers.add_parser("validate-template")
    validate.add_argument("--template", required=True)

    allocate = subparsers.add_parser("allocate-from-template")
    allocate.add_argument("--project-root", default=".")
    allocate.add_argument("--registry")
    allocate.add_argument("--template", required=True)
    allocate.add_argument("--role-id", type=int)
    allocate.add_argument("--role-name")

    record = subparsers.add_parser("record-create")
    record.add_argument("--project-root", default=".")
    record.add_argument("--registry")
    record.add_argument("--role-id", required=True, type=int)
    record.add_argument("--role-name", required=True)
    record.add_argument("--action-ids", default="")

    rename = subparsers.add_parser("rename-role")
    rename.add_argument("--project-root", default=".")
    rename.add_argument("--registry")
    rename.add_argument("--role-id", required=True, type=int)
    rename.add_argument("--role-name", required=True)

    build_templates = subparsers.add_parser("build-all-templates")
    build_templates.add_argument("--project-root", default=".")
    build_templates.add_argument("--registry")
    build_templates.add_argument("--template-root")

    reassign = subparsers.add_parser("reassign-all-ids")
    reassign.add_argument("--project-root", default=".")
    reassign.add_argument("--registry")
    reassign.add_argument("--template-root")

    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    if args.command == "rebuild-registry":
        result = rebuild_registry(
            Path(args.project_root).resolve(),
            registry_path_from_args(args).resolve(),
            args.template_root,
        )
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0

    if args.command == "show-registry":
        registry_path = registry_path_from_args(args).resolve()
        registry = ensure_registry_shape(load_json(registry_path) if registry_path.exists() else None)
        print(json.dumps(registry, ensure_ascii=False, indent=2))
        return 0

    if args.command == "validate-template":
        template = load_json(Path(args.template).resolve())
        errors = validate_template(template)
        payload = {
            "templatePath": str(Path(args.template).resolve()).replace("\\", "/"),
            "valid": len(errors) == 0,
            "errors": errors,
        }
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0 if not errors else 1

    if args.command == "allocate-from-template":
        registry_path = registry_path_from_args(args).resolve()
        registry = ensure_registry_shape(load_json(registry_path) if registry_path.exists() else None)
        template = load_json(Path(args.template).resolve())
        payload = allocate_from_template(registry, template, args.role_id, args.role_name)
        payload["templatePath"] = str(Path(args.template).resolve()).replace("\\", "/")
        payload["registryPath"] = str(registry_path).replace("\\", "/")
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0

    if args.command == "record-create":
        registry_path = registry_path_from_args(args).resolve()
        registry = ensure_registry_shape(load_json(registry_path) if registry_path.exists() else None)
        action_ids = parse_action_ids(args.action_ids)
        updated = record_create(registry, args.role_id, args.role_name, action_ids)
        save_json(registry_path, updated)
        print(json.dumps(updated, ensure_ascii=False, indent=2))
        return 0

    if args.command == "rename-role":
        registry_path = registry_path_from_args(args).resolve()
        registry = ensure_registry_shape(load_json(registry_path) if registry_path.exists() else None)
        updated = rename_role(registry, args.role_id, args.role_name)
        save_json(registry_path, updated)
        print(json.dumps(updated, ensure_ascii=False, indent=2))
        return 0

    if args.command == "build-all-templates":
        project_root = Path(args.project_root).resolve()
        registry_path = registry_path_from_args(args).resolve()
        registry = ensure_registry_shape(load_json(registry_path) if registry_path.exists() else None)
        template_root = template_root_from_registry(project_root, registry, args.template_root)
        payload = build_all_templates(project_root, template_root)
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0

    if args.command == "reassign-all-ids":
        project_root = Path(args.project_root).resolve()
        registry_path = registry_path_from_args(args).resolve()
        registry = ensure_registry_shape(load_json(registry_path) if registry_path.exists() else None)
        template_root = template_root_from_registry(project_root, registry, args.template_root)
        payload = reassign_all_ids(project_root, template_root, registry)
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0

    parser.error("Unknown command.")
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
