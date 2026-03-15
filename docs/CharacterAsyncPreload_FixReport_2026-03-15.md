# Character Async Preload Fix Report

Date: 2026-03-15

## Summary

This update changed the character spawn and action preload path to an awaitable flow and fixed a VFX pool warning caused by duplicate returns during process cleanup.

## Main Changes

- Added async timeline loading APIs in `SerializationUtility`.
- Added async preload orchestration and in-flight task deduplication in `ActionManager`.
- Moved action preload out of `CharacterEntity.Init` and into `CharacterManager/PlayerManager.PossessNewCharacterAsync` before instantiation.
- Updated `Test_Character` to wait for spawn completion and expose `IsSpawnCompleted` and `IsSpawnSucceeded`.
- Changed runtime playback to use cache-only synchronous reads and emit warnings on cache miss.

## Files Updated

- `Assets/GameClient/SkillEditor/Runtime/Serialization/SerializationUtility.cs`
- `Assets/GameClient/Logic/Skill/ActionManager.cs`
- `Assets/GameClient/Logic/Character/CharacterManager.cs`
- `Assets/GameClient/Logic/Player/PlayerManager.cs`
- `Assets/GameClient/Logic/Character/CharacterEntity.cs`
- `Assets/GameClient/Logic/Character/Test_Character.cs`
- `Assets/GameClient/Logic/Character/ActionPlayer.cs`
- `Assets/GameClient/SkillEditor/Runtime/Playback/Processes/RuntimeVFXProcess.cs`

## VFX Pool Warning Analysis

Warning:

`[GlobalPoolManager] 归还的对象不属于任何已注册的池: FX_slash_04(Clone)`

Root cause:

- `RuntimeVFXProcess.OnExit()` returned the VFX instance.
- `SkillRunner.FullCleanup()` later called `OnDisable()` for the same process instance.
- `RuntimeVFXProcess.OnDisable()` returned the same VFX instance again.
- `GlobalPoolManager` had already removed that instance from `_activeInstances`, so the second return path logged the warning and destroyed it as an unknown object.

Fix:

- Reworked `RuntimeVFXProcess` so release is idempotent.
- Added `_returnQueued` guard to prevent double scheduling.
- Centralized release logic in one method used by both `OnExit()` and `OnDisable()`.
- Cleared local references after scheduling or performing the return.

## Expected Result

- Character spawn can now be awaited end-to-end from the test bootstrap.
- Timelines should finish loading before the character enters FSM playback states.
- The VFX pool warning should stop appearing for normal skill effect cleanup.

## Verification Notes

Not validated in Unity Play Mode from this environment. Recommended checks:

- Spawn a test character through `Test_Character` and confirm `IsSpawnSucceeded == true`.
- Enter Idle, Jog, Dash, and skill states and confirm there are no timeline cache miss warnings.
- Cast a skill that spawns `FX_slash_04` and confirm the pool warning no longer appears.
