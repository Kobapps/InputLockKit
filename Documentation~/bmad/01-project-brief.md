# Project Brief — InputLockKit

> BMAD artifact · Phase: Analysis · Owner: Analyst → PM
> Status: Approved · Target: Kobapps package library

## 1. Problem statement

Games constantly need to **temporarily block player input** — during tutorials, cutscenes,
popups, network requests, reward animations, scene transitions. Teams keep re-inventing this
per project, usually as an ad-hoc `bool _inputLocked` sprinkled across managers, or a
monolithic service welded to that game's DI container, UI stack and save system.

The reference implementation reviewed (`IcyGhost/MergeMiner-1`) is a mature but
**non-portable** example:

- Coupled to **Zenject** (`[Inject]`, `SignalBus`) and **Odin** (editor window).
- Tags are a **hardcoded game enum** (`InputLockTag` with ~80 project-specific values).
- Heavy **LINQ + per-call allocations** (`Guid.NewGuid().ToString()`, `Select`/`Where`/`ToList`,
  a fresh `HashSet<string>` per lock) on a hot, frequently-toggled path.
- `UnlockInput(string[] tags)` **clears every owner's lock** on a tag, not just the caller's —
  an ownership bug that makes overlapping systems fight.
- Persistence is **hardwired** to the game's `GameState` + `Tutorials` services.
- The service re-evaluates **every** lockable on **every** change — O(locks × lockables).

## 2. Vision

A **standalone, dependency-free, AAA-quality** Unity package under the **Kobapps** namespace that
any project can drop in and use in minutes:

> *Lock and unlock input by tag, from anywhere, with a one-line disposable handle, zero steady-state
> allocations, a pluggable per-game state adapter, ready-made UI/gameplay components, and a
> professional live debugger.*

## 3. Goals & success criteria

| # | Goal | Measure of success |
|---|------|--------------------|
| G1 | Standalone package | Compiles in an empty Unity 6 project with **no third-party deps** (no Zenject/Odin/DOTween). |
| G2 | Minimal allocations | **0 B GC** per Lock/Release after warmup (verified by `Assert.That(() => …, Is.Not.AllocatingGCMemory)` / GC probe test). |
| G3 | Simple API | Lock input in **one line**; scope-safe via `using`. |
| G4 | Better & more modular | Split assemblies (Core / UI / Editor / Tests); Core has no UGUI dependency. |
| G5 | Pro editor tools | Live debugger window: tags, ref-counts, owners, subscribers, toggles, event log. |
| G6 | State support | `IInputLockStateAdapter` — each game plugs its own save/restore; ships PlayerPrefs sample. |
| G7 | Extensible components | Base `InputLockableBehaviour`; ship UI + gameplay lockables; new ones in <20 LOC. |
| G8 | Examples | Runnable sample scenes importable from Package Manager. |

## 4. Non-goals

- Not an input *mapping/rebinding* system (that's the Input System's job).
- Not a global pause/time-scale manager (locks input, not simulation).
- No networking/replication of lock state (adapter can add it).

## 5. Target users

- **Gameplay/UI engineers** who want a one-liner to gate input.
- **Technical designers** who define lock tags and wire components without code.
- **QA / tools engineers** who need to see and toggle live locks while playing.

## 6. Constraints

- Unity **6000.x** (Unity 6), .NET Standard 2.1, UGUI available but optional for Core.
- IL2CPP-safe (no reflection on the hot path, no `dynamic`).
- DI-agnostic: usable from plain `new`, a static facade, or bound in Zenject/VContainer/etc.

## 7. Reference deltas (what we keep / fix)

| Reference behaviour | InputLockKit decision |
|---------------------|-----------------------|
| Tag = fixed enum | Interned `InputLockTag` struct (int id + name), defined in code or a `ScriptableObject` catalog. |
| GUID string per lock | Pooled integer handle with generation stamp (struct, disposable). |
| Clear-all on unlock | Ref-counted; a handle releases **only its own** contribution. |
| Re-evaluate all lockables | Per-tag subscriber lists; only affected lockables update, O(subscribers-of-toggled-tag). |
| Zenject/Odin | POCO service + IMGUI editor; DI optional. |
| Hardwired save | `IInputLockStateAdapter`. |
