# Product Requirements Document — InputLockKit

> BMAD artifact · Phase: Planning · Owner: PM
> Depends on: `01-project-brief.md`

## 1. Overview

InputLockKit provides a tag-based input locking service, a set of reactive lockable components,
a pluggable persistence adapter, and professional editor tooling. Multiple independent systems
can request a lock on the same tag; input for that tag stays locked until **all** requesters
release. Every lock returns a disposable handle scoped to its owner.

## 2. Personas & top user stories

- **As a gameplay engineer**, I lock all input for the duration of an async reward flow with
  `using (InputLock.All()) { await Reward(); }` and never worry about forgetting to unlock.
- **As a tutorial designer**, I mark the only interactable button with an *allow* tag and
  `InputLock.AllExcept(tutorialTag)` so the rest of the UI is inert.
- **As a UI engineer**, I drop a `CanvasGroupLock` on a panel, pick its tags in the inspector,
  and it disables itself whenever any of those tags is locked — no code.
- **As a tools engineer**, I open *Kobapps ▸ Input Lock ▸ Debugger*, see which tags are locked,
  who owns each lock, which components are affected, and toggle locks live.
- **As a systems engineer**, I implement `IInputLockStateAdapter` so active locks survive a
  save/reload (e.g., a tutorial re-locks input after relogin).

## 3. Functional requirements

### FR-1 Core locking service
- FR-1.1 Lock by one tag, many tags, all tags, or all-except a set.
- FR-1.2 Each lock returns an `InputLockHandle` (struct, `IDisposable`).
- FR-1.3 Releasing a handle removes only that handle's contribution (ref-counted per tag).
- FR-1.4 `IsLocked(tag)` returns true iff ref-count > 0.
- FR-1.5 Double-release and stale-handle release are safe no-ops (generation check).
- FR-1.6 `LockAll` covers all *known* tags and any tag registered later stays consistent.
- FR-1.7 Events: `TagStateChanged(tag, isLocked)` and `LocksChanged` for tooling/adapters.
- FR-1.8 `Reset()` releases everything and clears state (for domain reload / test teardown).

### FR-2 Tags
- FR-2.1 `InputLockTag` is a value type comparable/equatable without allocation.
- FR-2.2 Tags are created from strings (interned) or from a `ScriptableObject` catalog.
- FR-2.3 A catalog asset lets designers define named tags with descriptions/colors.

### FR-3 Lockable components
- FR-3.1 Base `InputLockableBehaviour` subscribes to its tags and receives `OnLock`/`OnUnlock`.
- FR-3.2 Ships: `CanvasGroupLock`, `SelectableLock`, `GraphicRaycasterLock` (UI);
  `BehaviourLock`, `GameObjectActiveLock`, `Collider2DLock`, `Collider3DLock`,
  `UnityEventLock` (gameplay/general).
- FR-3.3 A component with multiple tags locks if **any** of its tags is locked.
- FR-3.4 Late subscription option (subscribe after a game-load signal) for objects that persist.
- FR-3.5 Authoring a new lockable requires overriding only `OnLock`/`OnUnlock`.

### FR-4 State adapter
- FR-4.1 `IInputLockStateAdapter` with `Save(activeLocks)` / `Restore()` hooks.
- FR-4.2 Service invokes the adapter on lock/unlock (guarded so replays don't recurse).
- FR-4.3 Ships a `PlayerPrefsInputLockStateAdapter` sample and a `NullInputLockStateAdapter` default.

### FR-5 Editor tooling
- FR-5.1 Debugger window listing every known tag with locked state and live ref-count.
- FR-5.2 Expand a tag to see owners (handles) and affected components (ping-able).
- FR-5.3 Toggle a lock on/off and inject an ad-hoc custom tag lock at runtime.
- FR-5.4 Rolling event log (lock/unlock with owner + timestamp).
- FR-5.5 Tag catalog editor; lockable inspectors with a tag dropdown sourced from the catalog.

### FR-6 Packaging & samples
- FR-6.1 UPM package `com.kobapps.inputlockkit`, semver, installable via Git URL or local.
- FR-6.2 Assemblies: `Kobapps.InputLockKit`, `Kobapps.InputLockKit.UI`,
  `Kobapps.InputLockKit.Editor`, `Kobapps.InputLockKit.Tests`.
- FR-6.3 Importable samples covering basic lock, tutorial gating, custom lockable, custom adapter.

## 4. Non-functional requirements

| ID | Requirement |
|----|-------------|
| NFR-1 | **0 B** managed allocation per Lock/Release/IsLocked after warmup. |
| NFR-2 | No third-party runtime dependencies; Core has no UGUI dependency. |
| NFR-3 | IL2CPP + no-reflection hot path; thread-affine to main thread (documented). |
| NFR-4 | Public API XML-documented; README quick-start ≤ 10 lines to first lock. |
| NFR-5 | Edit-mode tests cover ref-counting, handles, subscribers, adapter, and alloc budget. |
| NFR-6 | Deterministic teardown; no static leaks between play sessions / domain reloads. |

## 5. Acceptance / release checklist

- [ ] Compiles clean in Unity 6 with only the package present.
- [ ] All edit-mode tests green, including the zero-alloc probe.
- [ ] Debugger window opens and reflects live locks.
- [ ] Each sample scene runs and demonstrates its feature.
- [ ] README quick-start reproduces from scratch.
