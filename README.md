# InputLockKit

**AAA-grade, zero-allocation, tag-based input locking for Unity.**
Lock and unlock player input from anywhere with one line, ref-counted so overlapping systems never
fight, scope-safe via disposable handles, with reactive UI/gameplay components, a pluggable per-game
state adapter, and a live editor debugger. Standalone — **no third-party dependencies**.

> Part of the **Kobapps** package library · `com.kobapps.inputlockkit`

---

## Why

Every game needs to temporarily block input — tutorials, cutscenes, popups, network waits, reward
animations. InputLockKit turns that into a first-class, reusable service instead of a scatter of
`bool _locked` flags:

- **One-liner, scope-safe** — `using (InputLock.All()) { await Reward(); }` can't leak a lock.
- **Reference counted** — many systems can lock the same tag; it unlocks only when the last one
  releases. A handle releases *its own* contribution and nothing else.
- **Zero steady-state GC** — interned integer tags, pooled handles, per-tag subscriber push. No LINQ,
  no per-lock `Guid`, no re-scan of every component.
- **DI-agnostic** — plain C# service. Use the static facade, a scene installer, or bind it in
  Zenject / VContainer.
- **Batteries included** — UI + gameplay lockables, a tag catalog, a state adapter, and a pro debugger.

## Install

Package Manager ▸ **Add package from git URL** (or add to `Packages/manifest.json`):

```
"com.kobapps.inputlockkit": "https://github.com/Kobapps/InputLockKit.git"
```

Or copy the folder into your project's `Packages/`.

## Quick start

```csharp
using Kobapps.InputLockKit;

// Lock a category of input. Store the handle; dispose it to unlock.
var handle = InputLock.Lock("MainUi");
...
handle.Dispose();               // or handle.Release()

// Scope-based: guaranteed unlock, even on exception.
using (InputLock.All())
{
    await PlayCutscene();
}

// Leave exactly one thing interactable (tutorials):
using (InputLock.AllExcept(new InputLockTag[] { "TutorialButton" }))
{
    await WaitForTutorialTap();
}

// Query:
if (InputLock.IsLocked("Camera")) { /* ... */ }
```

### Groups & selections (e.g. grid cells)

Give lockables a **group** (and optional identifier) — in the inspector or at runtime via
`SetGroup(group, id)` — then lock by group, by group-except-a-selection, or by an explicit selection.
Handy for dynamic content like grid cells:

```csharp
// Each cell is a lockable in the "Grid" group (tags/group can be set on dynamically spawned cells):
cell.SetGroup("Grid", $"Cell_{r}_{c}");
cell.AddTag($"cell_{r}_{c}");          // dynamic per-cell tag, added at runtime

InputLock.Group("Grid");               // lock every cell in the group
InputLock.GroupExcept("Grid", picked); // lock all cells except the picked ones
InputLock.Only(picked);                // lock exactly the picked cells
```

Tag locks and group/selection locks share one reference count per lockable, so overlapping sources
never fight and a lockable unlocks only when the last one releases.

No code on the receiving side — drop a component:

| Component | Effect while locked |
|-----------|--------------------|
| `CanvasGroupLock` | Disables interaction/raycasts on a UI subtree (restores authored state) |
| `SelectableLock` | Makes a Button/Toggle/Slider non-interactable |
| `GraphicRaycasterLock` | Turns off a canvas raycaster (whole-canvas lock) |
| `BehaviourLock` | Disables any set of `Behaviour`s |
| `GameObjectActiveLock` | (De)activates GameObjects — e.g. show a blocker overlay |
| `Collider2DLock` / `Collider3DLock` | Disables colliders so world objects stop taking clicks |
| `UnityEventLock` | Fires UnityEvents on lock/unlock (designer escape hatch) |

Set each component's **Tags** in the inspector (with a catalog-backed dropdown). A component locks
when *any* of its tags is locked.

### Author your own lockable
```csharp
public sealed class AbilityLock : InputLockableBehaviour
{
    [SerializeField] private Ability _ability;
    protected override void OnLock()   => _ability.enabled = false;
    protected override void OnUnlock() => _ability.enabled = true;
}
```

### Persist locks (per-game state adapter)
Implement `IInputLockStateAdapter` (or use the PlayerPrefs sample) so locks survive a save/reload —
handy when a tutorial must stay gated after a relogin. Wire it via the **Input Lock Service
Installer** component or `new InputLockService(adapter)`.

## Editor tooling
**Tools ▸ Input Lock ▸ Debugger** — a live window showing every tag, its locked state and
reference count, who owns each lock, which components it drives, plus toggles, an ad-hoc tag injector,
and a rolling event log.

## Architecture at a glance
- `InputLockTag` — interned value-type tag (int id + name), created from strings or a catalog.
- `IInputLockService` / `InputLockService` — ref-counted, pooled, push-based core (POCO).
- `InputLockHandle` — disposable, generation-stamped receipt (stale/double release is a no-op).
- `InputLock` — static facade over the active service (overridable provider for DI/tests).
- `InputLockableBehaviour` — base for reactive components; O(1) per tag toggle.
- `IInputLockStateAdapter` — your save system's plug-in point.

Assemblies: `Kobapps.InputLockKit` (core, no UGUI) · `Kobapps.InputLockKit.UI` (UGUI components) ·
`Kobapps.InputLockKit.Editor` (tooling) · `Kobapps.InputLockKit.Tests`.

Full design docs live in `Documentation~/bmad/`. Import the runnable examples from the
**Samples** tab in Package Manager.

## Requirements
Unity **6000.0+**. IL2CPP-safe. Main-thread affine.

## License
MIT — see `LICENSE.md`.
