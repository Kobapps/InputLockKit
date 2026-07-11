namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Context handed to <see cref="IInputLockStateAdapter.Restore"/> so an adapter can re-apply
    /// previously saved locks. Locking through the context is flagged as a restore, so it will not
    /// re-trigger <see cref="IInputLockStateAdapter.OnLocksChanged"/> and cause a save loop.
    /// </summary>
    public interface IInputLockRestoreContext
    {
        /// <summary>Re-applies a lock for <paramref name="tag"/> during restore.</summary>
        InputLockHandle Lock(InputLockTag tag, string owner = "StateRestore");
    }

    /// <summary>
    /// The per-game bridge between InputLockKit and a project's own persistence layer. Implement this
    /// to save the set of active locks (e.g. so a tutorial that locked input stays locked after a
    /// relogin) and to restore them on load. The kit ships <see cref="NullInputLockStateAdapter"/>
    /// (no-op default) and a PlayerPrefs sample adapter.
    /// </summary>
    public interface IInputLockStateAdapter
    {
        /// <summary>
        /// Invoked after the locked-tag set changes (never during a restore). Persist the tags in
        /// <paramref name="snapshot"/> however the game sees fit. Keep it fast; it can fire often.
        /// </summary>
        void OnLocksChanged(InputLockSnapshot snapshot);

        /// <summary>
        /// Invoked when the service asks the adapter to re-apply saved locks. Read your saved tags and
        /// call <see cref="IInputLockRestoreContext.Lock"/> for each. Store the returned handles if you
        /// intend to release them later.
        /// </summary>
        void Restore(IInputLockRestoreContext context);
    }
}
