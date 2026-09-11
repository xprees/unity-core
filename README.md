# Unity Core classes - cz.xprees.core

[![NPM Version](https://img.shields.io/npm/v/cz.xprees.core)](https://www.npmjs.com/package/cz.xprees.core)

Base package containing core systems, state lifecycle orchestration, and ScriptableObject utilities for Unity projects.

## Installation

Install the package using npm scoped registry in `Project Settings > Package Manager > Scoped Registries`:

```json
{
    "name": "NPM - xprees",
    "url": "https://registry.npmjs.org",
    "scopes": [
        "cz.xprees"
    ]
}
```

Then install `cz.xprees.core` via the Unity Package Manager.

---

## State Management & Lifecycle Architecture

The package provides automated, zero-boilerplate state snapshotting and restoration for ScriptableObjects across scenario boundaries and Editor play
mode transitions.

### 1. State Lifetime (`StateLifetime`)

Every stateful ScriptableObject inherits from `DescriptionBaseSO` or declares its lifecycle scope via `[StatefulLifetime]`:

- **`StateLifetime.Scenario` (Default)**:
  Scoped strictly to the active scenario. Captured before mutations and automatically restored to its baseline on scenario start/restart.
- **`StateLifetime.Session`**:
  Persists across scenarios throughout a play session. Restored only on game/session boundaries or play mode exit.
- **`StateLifetime.Persistent`**:
  Excluded from state snapshots. Ideal for immutable databases, user settings, or content saved externally to disk.

```csharp
// Per-instance: Set the "Lifetime" dropdown in the Inspector.
// Per-class: Override via attribute:
[StatefulLifetime(StateLifetime.Persistent)]
public class GlobalPlayerConfigSO : DescriptionBaseSO { }
```

### 2. State Snapshot Service (`StateSnapshotService`)

A centralized, pure runtime service that handles:

- **`EnsureCaptured(ScriptableObject target)`**: Serializes the baseline JSON state prior to the first mutation.
- **`Restore(ScriptableObject target)`**: Reverts serialized fields in-place from baseline JSON and triggers
  `IRuntimeStateOwner.ClearTransientState()`.
- **`RestoreAll(IEnumerable<ScriptableObject> targets, StateLifetime maxLifetime)`**: Multi-pass restoration that ensures all targets are
  baseline-captured before restoring, preventing inter-object dependency races.
- **`[SnapshotIgnore]`**: Decorate fields to preserve their values across snapshot restorations.

### 3. Editor PlayMode Lifecycle (`StatePlayModeLifecycleHook`)

Located in `cz.xprees.core.editor`, this centralized runner hooks into `EditorApplication.playModeStateChanged`:

- **`EnteredPlayMode`**: Resets stateful objects decorated with `[ResetOnPlayMode(PlayModeResetTiming.EnterPlayMode)]` (skips `Persistent` objects).
- **`ExitingPlayMode`**: Restores all mutated ScriptableObjects in-place back to their baseline via `StateSnapshotService.RestoreAll()` and clears
  editor dirty flags before returning to edit mode.
- **`EnteredEditMode`**: Purges snapshot dictionaries.

### 4. Play Mode Reset Attribute (`ResetOnPlayModeAttribute`)

Control Play Mode transition resets declaratively on classes:

```csharp
[Flags]
public enum PlayModeResetTiming
{
    None = 0,
    EnterPlayMode = 1 << 0,
    ExitPlayMode = 1 << 1,
    Both = EnterPlayMode | ExitPlayMode
}

// Example: opt-out of play mode reset
[ResetOnPlayMode(PlayModeResetTiming.None)]
public class PersistentSettingsSO : ScriptableObject { }
```

### 5. Runtime State Ownership (`IRuntimeStateOwner`)

Implement `IRuntimeStateOwner` on objects with non-serialized runtime state (active handles, listeners, temporary collections):

```csharp
public interface IRuntimeStateOwner
{
    void ClearTransientState();
}
```

Invoked automatically after serialized fields are restored by `StateSnapshotService`.
