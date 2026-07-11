# Architecture — InputLockKit

> BMAD artifact · Phase: Architecture · Owner: Architect
> Depends on: `02-prd.md`

## 1. Module / assembly map

```
com.kobapps.inputlockkit/
├── Runtime/
│   ├── Kobapps.InputLockKit.asmdef          (Core — no UGUI, no third-party)
│   │   ├── Tags/         InputLockTag, InputLockTagRegistry, InputLockTagCatalog
│   │   ├── Core/         IInputLockService, InputLockService, InputLockHandle, LockRecord
│   │   ├── Facade/       InputLock (static), IInputLockServiceProvider,
│   │   │                 InputLockRuntime, InputLockServiceInstaller
│   │   ├── State/        IInputLockStateAdapter, NullInputLockStateAdapter, InputLockSnapshot
│   │   └── Components/   InputLockableBehaviour (+ non-UGUI lockables)
│   └── UI/
│       └── Kobapps.InputLockKit.UI.asmdef    (refs com.unity.ugui)
│           └── CanvasGroupLock, SelectableLock, GraphicRaycasterLock
├── Editor/
│   └── Kobapps.InputLockKit.Editor.asmdef    (Editor-only)
│       ├── InputLockDebuggerWindow (IMGUI)
│       ├── InputLockTagCatalogEditor, InputLockableBehaviourEditor
│       └── InputLockMenu
├── Tests/
│   └── Kobapps.InputLockKit.Tests.asmdef     (Editor test-only)
├── Samples~/                                  (imported via Package Manager)
└── Documentation~/
```

Rationale: **Core stays UGUI-free** so headless/server or non-UGUI projects can use the service.
UI lockables live behind their own asmdef that references `com.unity.ugui`. Editor and Tests are
isolated so nothing editor-only ships in a build.

## 2. Tag model

`InputLockTag` is a `readonly struct` wrapping an interned integer id plus its display name.

- `InputLockTagRegistry` (static) maps `string → int` once and caches `int → name`.
  Interning means dictionary lookups on the hot path use **int** keys (fast, no string hashing),
  while creation from a string happens rarely (usually at authoring/registration time).
- Equality/hashing operate on the int id → **no allocation**, struct-friendly for `Dictionary`.
- Implicit `string → InputLockTag` conversion keeps call sites terse; a `ScriptableObject`
  `InputLockTagCatalog` provides designer-defined tags and feeds inspector dropdowns.

```csharp
public readonly struct InputLockTag : IEquatable<InputLockTag>
{
    public readonly int Id;                 // interned
    public string Name => InputLockTagRegistry.NameOf(Id);
    public static implicit operator InputLockTag(string name) => InputLockTagRegistry.Get(name);
}
```

## 3. Locking core — data structures & algorithms

State held by `InputLockService`:

| Field | Type | Purpose |
|-------|------|---------|
| `_records` | `LockRecord[]` (pooled, slot-indexed) | one per active handle |
| `_freeSlots` | `Stack<int>` | recycled slots (no alloc on reuse) |
| `_generation` | `int[]` per slot | detects stale/double release |
| `_tagRefCount` | `Dictionary<int,int>` | how many handles hold each tag |
| `_tagSubscribers` | `Dictionary<int,List<InputLockableBehaviour>>` | lockables per tag |
| `_knownTags` | `List<int>` | for `LockAll` / debugger enumeration |

**Handle** — `readonly struct InputLockHandle : IDisposable { service; slot; generation; }`.
`Dispose()` → `service.Release(this)`. Validity = `generation[slot] == handle.generation`.

### Lock(tags)
1. Pop a free slot (or grow). Stamp `_generation[slot]`.
2. Store the tag ids into the record's pooled `int[]` buffer (grown only if needed).
3. For each tag: `count = ++_tagRefCount[tag]`. If `count == 1` → tag transitioned to **locked** →
   notify its subscriber list inline (see §4) and fire `TagStateChanged` (transitions are independent
   per tag, so notification is inline rather than batched).
4. Fire `LocksChanged`; notify the state adapter only when a real one is bound and not restoring.
5. Return handle. **No allocation** in steady state.

### Release(handle)
1. Validate generation (stale → no-op). 
2. For each tag in the record: `count = --_tagRefCount[tag]`. If `count == 0` → **unlocked** → dirty.
3. Flush dirty tags (unlock notifications). Return buffer, push slot to free list, bump generation.

Ref-counting means overlapping owners never clobber each other (fixes the reference's clear-all bug),
and steady-state operations touch only dictionaries + pooled arrays.

## 4. Reactive lockables — push, not poll

Each `InputLockableBehaviour` keeps `_lockedTagCount` = how many of *its* tags are currently locked.

- On a tag toggling **locked**, the service walks that tag's subscriber list; each subscriber does
  `if (++_lockedTagCount == 1) OnLock();`
- On a tag toggling **unlocked**: `if (--_lockedTagCount == 0) OnUnlock();`

This is **O(subscribers of the toggled tag)**, not O(all lockables), and each subscriber update is
O(1) with no re-scan of its tag list. Subscription registers the lockable under each of its tag ids.

## 5. Facade & DI strategy

- `InputLockService` is a plain class — `new InputLockService(adapter)`. Bind it in any container.
- `InputLockRuntime` holds a lazily-created **default** service (for the drop-in, no-DI path) and is
  reset on domain reload via `[RuntimeInitializeOnLoadMethod]`.
- `InputLock` static facade forwards to the current service resolved through
  `IInputLockServiceProvider` (default = runtime singleton; a scene component can override to a
  scoped instance). Keeps call sites to one line while remaining override-able for tests/DI.

## 6. State adapter contract

```csharp
public interface IInputLockStateAdapter
{
    void OnLocksChanged(InputLockSnapshot snapshot);     // persist active tags
    void Restore(IInputLockRestoreContext context);      // re-apply saved locks on init
}
```

`InputLockSnapshot` exposes the currently-locked tags via a live, read-only view (no per-call
`List` alloc). The service calls `OnLocksChanged` after mutations (guarded by a re-entrancy flag so a
restore doesn't re-trigger a save, and skipped entirely when no real adapter is bound — keeping the
lock path allocation-free). Restore hands the adapter an `IInputLockRestoreContext` to re-apply saved
locks. Games map this onto their own save system; the package ships a PlayerPrefs sample and a no-op
default.

## 7. Threading & lifetime

- Main-thread affine (Unity components). Documented; no locks/atomics on the hot path.
- All statics reset through `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` so
  Enter-Play-Mode-without-domain-reload is clean.
- `Reset()` on the service unwinds every record and clears dictionaries → no cross-session leak.

## 8. Editor tooling architecture

- `InputLockDebuggerWindow` (IMGUI) binds to the active service via the provider, subscribes to
  `LocksChanged`, and renders tags → ref-count → owners/subscribers. Play-mode only for live data;
  edit-mode shows the catalog. Repaints are event-driven (no per-frame polling).
- Editor references Core + UI asmdefs; never referenced by runtime.

## 9. Key risks & mitigations

| Risk | Mitigation |
|------|-----------|
| Hidden allocation on hot path | Dedicated alloc-probe test; pooled buffers; no LINQ in Runtime. |
| Stale handle after Reset | Generation stamps invalidate all outstanding handles. |
| UGUI hard-dependency creep | UI lockables isolated in their own asmdef. |
| Static state across play sessions | `SubsystemRegistration` reset + `Reset()` API. |
