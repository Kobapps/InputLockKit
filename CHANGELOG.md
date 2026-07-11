# Changelog

All notable changes to InputLockKit are documented here. This project adheres to
[Semantic Versioning](https://semver.org/).

## [1.1.0] — 2026-07-11

### Added
- **Groups & selection locking.** `InputLockGroup` + `InputLockGroupRegistry`; lockables can declare a
  group and identifier (inspector or runtime `SetGroup`). New service/facade API: `LockGroup`,
  `LockGroupExcept`, `LockOnly`, and `GetGroupMembers` — lock a whole group, a group-except-a-selection,
  or an explicit selection (e.g. picked grid cells).
- **Dynamic tags at runtime.** `InputLockableBehaviour.AddTag` / `RemoveTag` (plus existing `SetTags`),
  re-subscribing so newly spawned content (dynamic grid cells) can gain/lose tags on the fly.
- Unified per-lockable reference count so tag locks and group/selection locks compose without fighting.
- Debugger **Groups** panel: lists groups, member counts, and a live lock/unlock toggle per group.
- Example 04 **Grid Lock**: dynamically spawned button cells in a group; Lock Selected / Lock All
  Except Selected / Lock Whole Group, driven by a custom `GridCellLock` lockable.
- **Redesigned debugger window**: status/metrics header, colored LOCKED/OPEN pills, section headers,
  a search + "Locked only" filter, groups expandable to their member lockables (pingable, per-member
  lock dots), and a cleaner event log.
- AI skill (`.claude/skills/inputlockkit`) documenting usage with an automatic installer script
  (git URL, embedded local copy, or version string).

### Changed
- Locking a tag now registers it as known, so ad-hoc locked tags are visible in the debugger and
  covered by `LockAll`.

### Fixed
- Debugger no longer shows stale data from the previous Play session after exiting Play Mode; it
  clears live state and shows a clean idle view.

## [1.0.0] — 2026-07-11

### Added
- Core `InputLockService` with reference-counted, tag-based locking and pooled, generation-stamped
  `InputLockHandle`s (disposable; stale/double release is a safe no-op).
- Interned `InputLockTag` value type + `InputLockTagRegistry` and designer-facing
  `InputLockTagCatalog` ScriptableObject.
- Static `InputLock` facade, `IInputLockServiceProvider`, `InputLockRuntime` default service, and the
  `InputLockServiceInstaller` scene component (no DI required).
- Reactive components: `CanvasGroupLock`, `SelectableLock`, `GraphicRaycasterLock` (UI);
  `BehaviourLock`, `GameObjectActiveLock`, `Collider2DLock`, `Collider3DLock`, `UnityEventLock`.
- `InputLockableBehaviour` base with per-tag subscription and O(1) push updates.
- Pluggable persistence via `IInputLockStateAdapter` (+ `NullInputLockStateAdapter` default and a
  PlayerPrefs sample adapter).
- Editor **Input Lock Debugger** window (live tags, ref-counts, owners, affected components, toggles,
  ad-hoc tag injection, event log) and catalog / lockable inspectors.
- Edit-mode test suite covering ref-counting, handles, LockAll/Except, subscriber push, late
  subscription, Reset, state adapter, and a zero-allocation probe.
- Three importable samples: Basic UI Lock, Tutorial Gating, Custom Extensions.
- BMAD planning docs (brief, PRD, architecture, epics/stories) under `Documentation~/bmad/`.

[1.0.0]: https://github.com/Kobapps/InputLockKit/releases/tag/v1.0.0
