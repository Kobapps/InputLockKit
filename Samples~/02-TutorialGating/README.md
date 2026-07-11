# Sample 02 — Tutorial Gating

Uses `InputLock.AllExcept(...)` to lock the whole menu except the one button the current tutorial
step wants pressed. Each button carries a `SelectableLock` bound to its own tag, so gating is just
moving which tag is *excluded*.

## Run it
1. Add **TutorialGatingDemo** to an empty GameObject in a new scene.
2. Press Play. Only the highlighted step's button is interactable; press it to advance.
3. Watch the locks shift in **Kobapps ▸ Input Lock ▸ Debugger**.

## Key idea
```csharp
// Lock everything except this step's button:
_gateHandle = InputLock.AllExcept(new InputLockTag[] { ButtonTags[step] });
// Advancing releases the old gate and creates the next one.
```
