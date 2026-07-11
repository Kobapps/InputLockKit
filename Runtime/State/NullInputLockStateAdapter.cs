namespace Kobapps.InputLockKit
{
    /// <summary>
    /// The default no-op state adapter: locks are never persisted or restored. Used when a service is
    /// created without an explicit adapter.
    /// </summary>
    public sealed class NullInputLockStateAdapter : IInputLockStateAdapter
    {
        /// <summary>Shared instance; the adapter is stateless.</summary>
        public static readonly NullInputLockStateAdapter Instance = new NullInputLockStateAdapter();

        public void OnLocksChanged(InputLockSnapshot snapshot) { }

        public void Restore(IInputLockRestoreContext context) { }
    }
}
