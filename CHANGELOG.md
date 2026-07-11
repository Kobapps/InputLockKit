# Changelog

All notable changes to InputLockKit are documented here. This project adheres to
[Semantic Versioning](https://semver.org/).

## [1.2.0] — 2026-07-12

### Added
- **Project Settings page** — *Edit ▸ Project Settings ▸ Input Lock* (also *Tools ▸ Input Lock ▸
  Settings*, and a Settings button in the debugger): create/assign and inline-edit a **tag catalog**,
  toggle auto-register, and install the AI skill.
- `InputLockSettings` (a `Resources` asset) + a `BeforeSceneLoad` bootstrap that registers the
  configured catalog into `InputLockRuntime.Catalog`, so **catalog tags are known to every service,
  `LockAll` and the debugger automatically** — no manual wiring. Every new `InputLockService` picks it up.
- One-click **"Add AI Skill to .claude/skills"** (the Claude skill is bundled in the package under
  `AISkill~/`).
- **`[InputLockTag]` / `[InputLockGroup]` attributes** — decorate a serialized `string` /
  `List<string>` to draw it as a catalog-backed **dropdown** with *Add New…* (adds to the catalog) and
  *Open Tag Catalog…*. Applied to the lockable Tags and Group fields.
- The **catalog is now the authoritative, self-populating list of tags *and* reusable groups**: any
  tag or group used on a lockable is added to the catalog automatically (on edit), plus a *Collect Tags
  & Groups Used In Project* action in settings to backfill existing content. Catalog groups are interned
  at load alongside tags.

### Changed
- Debugger menu moved from `Kobapps ▸ Input Lock ▸ Debugger` to **`Tools ▸ Input Lock ▸ Debugger`**
  (removes the top-level "Kobapps" menu).
- Example controllers now demonstrate the new fields: `[InputLockTag]` on the basic/tutorial/custom
  examples' tag fields and `[InputLockGroup]` on the grid example's group; the example catalog is
  seeded with those tags (`Panel`, `Play`, `Shop`, `Settings`, `Quit`, `Gameplay`) and the `Grid` group.

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

[1.2.0]: https://github.com/Kobapps/InputLockKit/releases/tag/v1.2.0
[1.1.0]: https://github.com/Kobapps/InputLockKit/releases/tag/v1.1.0
[1.0.0]: https://github.com/Kobapps/InputLockKit/releases/tag/v1.0.0
