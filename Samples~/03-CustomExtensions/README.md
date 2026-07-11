# Sample 03 — Custom Lockable & State Adapter

Two extension points in one demo:

- **`PulseScaleLock`** — a custom lockable authored in ~15 lines by overriding `OnLock`/`OnUnlock`.
- **`PlayerPrefsInputLockStateAdapter`** — a reference `IInputLockStateAdapter` that persists locked
  tags to `PlayerPrefs` and restores them on load.

## Run it
1. Add **CustomExtensionsDemo** to an empty GameObject in a new scene (add the scene to Build
   Settings so *Reload Scene* works).
2. Press Play. Click **Lock Gameplay** — the target shrinks (that's the custom lockable).
3. Click **Reload Scene**. The lock is restored from PlayerPrefs; the target comes back already shrunk.
4. **Clear Saved** wipes the persisted state.

## In a real project
Skip the code wiring: drop an **Input Lock Service Installer** in your bootstrap scene, add a
**PlayerPrefs State Adapter** component, and assign it to the installer's *State Adapter* slot.
That's the whole integration.
