# Sample 01 — Basic UI Lock

Shows the everyday workflow: lock a tag, let a `CanvasGroupLock` react to it with zero glue code,
and use a disposable `using (InputLock.All())` scope for a temporary full lock.

## Run it
1. Create an empty scene.
2. Add an empty GameObject and put **BasicUILockDemo** on it (or menu
   *Kobapps ▸ Input Lock ▸ Samples ▸ Basic UI Lock Demo*).
3. Press Play. The demo builds its own UI.
4. Open **Tools ▸ Input Lock ▸ Debugger** to watch the `Panel` tag lock/unlock live.

## What to look at
- `InputLock.Lock("Panel")` returns a handle; disposing it unlocks.
- The panel has a `CanvasGroupLock` set to the `Panel` tag — it locks itself automatically.
- "Lock ALL for 2s" uses `using (InputLock.All()) { … }` so input is restored even if the
  routine is interrupted.
