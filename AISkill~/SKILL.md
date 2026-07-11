---
name: inputlockkit
description: Install and use InputLockKit — the Kobapps Unity package for tag/group-based input locking. Use when the user wants to add InputLockKit to a Unity project, or to block/lock player input (UI, buttons, world objects, grid cells) during tutorials, cutscenes, popups, loading, or reward flows; when they mention "input lock", "lock the UI", "disable input", "gate a tutorial", "lock selected cells", or ask to lock/unlock by tag or group. Includes an automatic installer.
---

# InputLockKit

AAA, zero-allocation, tag/group-based input locking for Unity. Lock and unlock player input from
anywhere with a one-line disposable handle, reference-counted so overlapping systems never fight.
Package id: `com.kobapps.inputlockkit` (assemblies `Kobapps.InputLockKit`, `.UI`, `.Editor`).

Source of truth for the API is the package's own `README.md` and `Documentation~/bmad/`. This skill is
the fast path for **installing** it into a project and **using** it correctly.

## Install (automatic)

Ask which install mode the user wants, then run `scripts/install.py` from this skill folder. It edits
the target project's `Packages/manifest.json` (and, for `--local`, copies the package in).

```bash
# Embedded local copy (recommended when you have the package source, e.g. this repo):
python scripts/install.py "<UnityProjectRoot>" --local "<repo>/Packages/com.kobapps.inputlockkit"

# Or via Git URL:
python scripts/install.py "<UnityProjectRoot>" --git

# Or a specific registry/git version string:
python scripts/install.py "<UnityProjectRoot>" --version "1.2.0"
```

`<UnityProjectRoot>` is the folder that contains `Packages/` and `Assets/`. After it runs, tell the
user to focus the Unity Editor (or reopen the project) so UPM resolves the package.

**If the Unity MCP is connected** (tools/skills like `package-add`, `assets-refresh`), you can instead
call `package-add` with the git URL or local path and then `assets-refresh` — no manual focus needed.

**Verify** after install: the menu **Tools ▸ Input Lock ▸ Debugger** exists and
`using Kobapps.InputLockKit;` compiles.

## Use it — API cheat sheet

```csharp
using Kobapps.InputLockKit;

// One-liner, scope-safe (auto-unlocks, even on exception):
using (InputLock.All()) { await PlayCutscene(); }

// Lock/unlock by tag with a handle:
var h = InputLock.Lock("MainUi");   // ... later ...  h.Dispose();

// Tutorials — leave one thing interactable:
using (InputLock.AllExcept(new InputLockTag[] { "TutorialButton" })) { await WaitForTap(); }

// Groups & selections (e.g. grid cells):
InputLock.Group("Grid");                 // lock every member of a group
InputLock.GroupExcept("Grid", picked);   // lock all except a selection (IReadOnlyList<InputLockableBehaviour>)
InputLock.Only(picked);                  // lock exactly a selection

// Query:
if (InputLock.IsLocked("Camera")) { }
```

Tag locks and group/selection locks share **one reference count per lockable**, so nothing unlocks
until the last source releases.

## Use it — components (no code on the receiving side)

Add a component and set its **Tags** (and optionally a **Group** + **Identifier**) in the inspector; it
locks when any of its tags — or its group/selection — is locked:

| Component | Effect while locked | Assembly |
|-----------|--------------------|----------|
| `CanvasGroupLock` | Disables interaction/raycasts on a UI subtree | `.UI` |
| `SelectableLock` | Makes a Button/Toggle/Slider non-interactable | `.UI` |
| `GraphicRaycasterLock` | Turns off a canvas raycaster (whole canvas) | `.UI` |
| `BehaviourLock` | Disables any set of `Behaviour`s | core |
| `GameObjectActiveLock` | (De)activates GameObjects (e.g. a blocker overlay) | core |
| `Collider2DLock` / `Collider3DLock` | Disables colliders so world objects stop taking clicks | core |
| `UnityEventLock` | Fires UnityEvents on lock/unlock | core |

Author a new one in a few lines:
```csharp
public sealed class AbilityLock : InputLockableBehaviour
{
    [SerializeField] private Ability _ability;
    protected override void OnLock()   => _ability.enabled = false;
    protected override void OnUnlock() => _ability.enabled = true;
}
```
Dynamic content (spawned grid cells) can set tags/group at runtime: `cell.SetGroup("Grid", id)`,
`cell.AddTag($"cell_{r}_{c}")`.

## Wiring & persistence

- **No DI:** drop an **Input Lock Service Installer** on a bootstrap object (optionally assign a tag
  catalog + a state adapter). Otherwise the static `InputLock` facade uses a runtime default service.
- **DI:** `new InputLockService(adapter)` and bind it; components resolve via `InputLock.Service` or a
  scene `IInputLockServiceProvider`.
- **Persist locks across a reload/relogin:** implement `IInputLockStateAdapter` (ships a PlayerPrefs
  sample) and assign it to the installer.

## Debugger

**Tools ▸ Input Lock ▸ Debugger** — live status header, every tag with locked state / ref-count /
owners / affected components, groups with their members, a search filter, and an event log. Live only
in Play Mode.

## Common recipes

- **Block everything during an async flow:** `using (InputLock.All()) { await X(); }`.
- **Tutorial step:** `handle = InputLock.AllExcept(new InputLockTag[] { stepTag });` and dispose to advance.
- **Lock a grid selection:** cells are `InputLockableBehaviour` in a group; `InputLock.Only(selected)`,
  `InputLock.GroupExcept("Grid", selected)`, or `InputLock.Group("Grid")`.
- **Popup opens:** `var h = InputLock.Lock("World");` on open, `h.Dispose();` on close.

## Examples

Runnable example scenes live in the package's `Samples~` (import via Package Manager) and, in this
repo, under `Assets/InputLockKitExamples/Scenes/` (01 Basic UI Lock, 02 Tutorial Gating, 03 Custom
Extensions, 04 Grid Lock).
