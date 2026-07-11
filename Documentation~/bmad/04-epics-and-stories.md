# Epics & Stories — InputLockKit

> BMAD artifact · Phase: Sharding (SM) · Owner: Scrum Master → Dev
> Depends on: `03-architecture.md`

Story status legend: ⬜ todo · 🟦 in progress · ✅ done

## Epic A — Package foundation
- ✅ **A1** Embedded UPM package `com.kobapps.inputlockkit` (package.json, README, CHANGELOG, LICENSE).
- ✅ **A2** Assembly definitions: Core, UI, Editor, Tests with correct references & platform scoping.

## Epic B — Tag system
- ✅ **B1** `InputLockTag` interned struct + `InputLockTagRegistry` (string↔int, name cache).
- ✅ **B2** `InputLockTagCatalog` ScriptableObject (designer tags: name, description, color).
  *AC:* creating a tag via string twice returns equal, allocation-free values.

## Epic C — Locking core
- ✅ **C1** `InputLockHandle` disposable struct with generation validity.
- ✅ **C2** `IInputLockService` + `InputLockService`: Lock/Release, ref-counting, pooled records.
  *AC:* two handles on one tag; releasing one keeps it locked; releasing both unlocks (FR-1.3).
- ✅ **C3** `LockAll`, `LockAllExcept`, `IsLocked`, events, `Reset`.
  *AC:* stale/double release is a safe no-op (FR-1.5).

## Epic D — Facade & providers
- ✅ **D1** `InputLockRuntime` default service + `SubsystemRegistration` reset.
- ✅ **D2** `IInputLockServiceProvider` + `InputLock` static facade + `InputLockScope` helper.

## Epic E — State adapter
- ✅ **E1** `IInputLockStateAdapter`, `InputLockSnapshot`, `NullInputLockStateAdapter`.
- ✅ **E2** Service ↔ adapter wiring with re-entrancy guard; `Restore` path.

## Epic F — Lockable components
- ✅ **F1** `InputLockableBehaviour` base: per-tag subscription, `_lockedTagCount`, late-subscribe.
- ✅ **F2** Gameplay lockables: `BehaviourLock`, `GameObjectActiveLock`, `Collider2DLock`,
  `Collider3DLock`, `UnityEventLock`.
- ✅ **F3** UI lockables (UI asmdef): `CanvasGroupLock`, `SelectableLock`, `GraphicRaycasterLock`.

## Epic G — Editor tooling
- ✅ **G1** `InputLockDebuggerWindow` (IMGUI): tags, ref-counts, owners, subscribers, toggles.
- ✅ **G2** Runtime custom-tag injection + rolling event log.
- ✅ **G3** Catalog editor + lockable inspector with tag dropdown.

## Epic H — Tests
- ✅ **H1** Ref-count / handle / LockAll(Except) / IsLocked tests (ported + extended from reference).
- ✅ **H2** Subscriber push-update tests; late-subscribe.
- ✅ **H3** Zero-alloc probe; adapter save/restore; Reset leak test.

## Epic I — Samples & docs
- ✅ **I1** Sample: basic UI lock (button/panel) + async “All” scope.
- ✅ **I2** Sample: tutorial gating via `AllExcept`.
- ✅ **I3** Sample: custom lockable + custom `PlayerPrefs` state adapter.
- ✅ **I4** README quick-start + API overview; samples registered in package.json.

## Traceability (PRD → stories)

| PRD | Stories |
|-----|---------|
| FR-1 | C1, C2, C3 |
| FR-2 | B1, B2 |
| FR-3 | F1, F2, F3 |
| FR-4 | E1, E2, I3 |
| FR-5 | G1, G2, G3 |
| FR-6 | A1, A2, I1–I4 |
| NFR-1 | H3 |
