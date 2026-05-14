---
description: Guide for extending and developing features for the SkillEditor system
---

# SkillEditor Development Guide

This guide outlines the architecture and workflows for extending the SkillEditor, reflecting the latest refactoring efforts including attribute-driven tracks, service-based context, and decoupled animation handling.

## 1. Architecture Overview

The SkillEditor is built on a modular architecture designed for extensibility and loose coupling.

*   **Core Logic**: `SkillRunner`, `ProcessContext`, `ProcessBase<T>`
*   **Data Model**: `SkillTimeline`, `TrackBase`, `ClipBase`
*   **Editor UI**: `ATEditorWindow`, `TrackListView`, `TimelineView`
*   **Service Layer**: `IServiceFactory`, `SkillServiceFactory`, `ISkillAnimationHandler`
*   **Registry**: `TrackRegistry` (Attribute-based discovery)
*   **Inspectors**: `TrackDrawer`, `ClipDrawer`, `CustomDrawerAttribute`

## 2. Adding a New Track Type

To add a new track type (e.g., `SoundTrack`, `CameraShakeTrack`), you no longer need to modify core editor code.

### Step 1: Define the Clip Data
Create a class inheriting from `ClipBase` (or a specific subclass like `CurveClip`).

```csharp
[Serializable]
public class MyCustomClip : ClipBase
{
    public float myParameter;
    // ...
}
```

### Step 2: Define the Track Data
Create a class inheriting from `TrackBase`. Decorate it with `[TrackDefinition]` to register it.

```csharp
[TrackDefinition(
    displayName: "My Custom Track",
    menuPath: "Add Track/Custom/My Track",
    icon: "MyIcon", // Icon name in Gizmos or Editor default resources
    order: 100,
    clipType: typeof(MyCustomClip), // The clip type this track manages
    colorHex: "#FF5500" // Track display color
)]
public class MyCustomTrack : TrackBase
{
    // ...
}
```
*The `TrackRegistry` will automatically discover this track and add it to the "Add Track" menu.*

### Step 3: Implement the Runtime Process
Create a class inheriting from `ProcessBase<T>` where `T` is your clip type. Bind it using `[ProcessBinding]`.

```csharp
[ProcessBinding(typeof(MyCustomClip), PlayMode.Runtime)]
public class RuntimeMyCustomProcess : ProcessBase<MyCustomClip>
{
    public override void OnEnter()
    {
        // Access services via context
        var runner = context.GetService<MonoBehaviour>(); // Universal Coroutine Runner
        // ...
    }

    public override void OnUpdate(float currentTime, float deltaTime) { ... }
    public override void OnExit() { ... }
}
```

### Step 4: Implement the Editor Preview Process (Optional)
If you need preview functionality in the editor, create a process bound to `PlayMode.EditorPreview`.

```csharp
[ProcessBinding(typeof(MyCustomClip), PlayMode.EditorPreview)]
public class EditorMyCustomProcess : ProcessBase<MyCustomClip>
{
    // ...
}
```

## 3. Customizing Inspector (Drawers)

To customize how your Track or Clip is displayed in the Inspector, use the `[CustomDrawer]` attribute. **Do not modify `DrawerFactory`**.

### Custom Track Drawer
Inherit from `TrackDrawer` and decorate with `[CustomDrawer]`.

```csharp
using UnityEditor;
using ATEditor.Editor;

[CustomDrawer(typeof(MyCustomTrack))]
public class MyCustomTrackDrawer : TrackDrawer
{
    public override void DrawInspector(TrackBase track)
    {
        var myTrack = track as MyCustomTrack;
        if (myTrack == null) return;

        EditorGUILayout.LabelField("Base Settings", EditorStyles.boldLabel);
        base.DrawInspector(track); // Draw default properties

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Custom Settings", EditorStyles.boldLabel);
        // Custom GUI code
        if (GUILayout.Button("Action")) 
        {
             // ...
        }
    }
}
```

### Custom Clip Drawer
Inherit from `ClipDrawer` and decorate with `[CustomDrawer]`.

```csharp
using UnityEditor;
using ATEditor.Editor;

[CustomDrawer(typeof(MyCustomClip))]
public class MyCustomClipDrawer : ClipDrawer
{
    public override void DrawInspector(ClipBase clip)
    {
        var myClip = clip as MyCustomClip;
        if (myClip == null) return;

        // Custom Header
        EditorGUILayout.LabelField("My Custom Clip", EditorStyles.boldLabel);
        
        // Use standard IMGUI or base.DrawInspector
        EditorGUI.BeginChangeCheck();
        myClip.myParameter = EditorGUILayout.FloatField("Parameter", myClip.myParameter);
        if (EditorGUI.EndChangeCheck())
        {
             // Handle changes if needed
        }
    }
}
```
*The `DrawerFactory` will automatically find and use these drawers when inspecting the corresponding Track or Clip.*

## 4. Using Services in ProcessContext

The `ProcessContext` now uses a Service Locator pattern with Lazy Loading.

### Available Services

*   **`ISkillAnimationHandler`**: Abstracts animation system operations (Play, CrossFade, SetLayerWeight).
    *   *Usage*: `context.GetService<ISkillAnimationHandler>()`
*   **`ISkillActor`**: access to the actor's transform, bones, etc.
    *   *Usage*: `context.GetService<ISkillActor>()`
*   **`MonoBehaviour` (CoroutineRunner)**: A universal runner for starting coroutines (e.g., for delayed VFX destruction).
    *   *Usage*: `context.GetService<MonoBehaviour>()`

### Registering New Services
Services are created by `SkillServiceFactory` in `GameClient/Adapters`. To add a new service:

1.  Define the interface (e.g., `IAudioManager`).
2.  Implement the interface (e.g., `AudioServiceAdapter`).
3.  Update `SkillServiceFactory.ProvideService` to return the implementation when requested.

## 5. Animation System Integration

The SkillEditor is decoupled from specific animation solutions (like `AnimComponent` or Unity's `Animator`).

*   **Runtime**: The `RuntimeAnimationProcess` talks to `ISkillAnimationHandler`.
*   **Adapter**: `AnimComponentAdapter` implements this interface and forwards calls to the actual `AnimComponent`.
*   **Extending**: If you switch to a different animation system, implement a new `ISkillAnimationHandler` adapter and update `SkillServiceFactory`.

## 6. VFX Lifecycle Management

VFX clips should not manage their own cleanup via `context.Owner`. Instead, utilize the universal `CoroutineRunner`.

```csharp
public override void OnExit()
{
    if (needsDelayedDestroy)
    {
        var runner = context.GetService<MonoBehaviour>();
        if (runner != null)
        {
            runner.StartCoroutine(DelayedDestroy(instance, duration));
        }
        else
        {
             // Fallback: Immediate destroy
             Object.Destroy(instance);
        }
    }
}
```

## 7. Key Rules & Best Practices

1.  **OCP (Open/Closed Principle)**: Never modify `TrackListView.cs`, `TrackRegistry.cs`, or `DrawerFactory.cs` to add new features. Use attributes (`[TrackDefinition]`, `[CustomDrawer]`).
2.  **DIP (Dependency Inversion)**: Processes should not depend on concrete GameClient classes (like `AnimComponent` or specific MonoBehaviours). Use `ProcessContext.GetService<T>`.
3.  **Lazy Loading**: Services are loaded on demand. Do not assume a service exists; always check for null after `GetService`.
