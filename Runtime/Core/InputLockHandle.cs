using System;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// A lightweight, disposable receipt for a single lock request. Holding a handle keeps its tags
    /// locked; disposing it (or calling <see cref="IInputLockService.Release"/>) removes exactly this
    /// request's contribution and nothing else. Handles are value types stamped with a generation, so
    /// a stale or double release is a safe no-op.
    /// </summary>
    /// <example>
    /// <code>
    /// using (InputLock.All())        // locks everything for the scope
    /// {
    ///     await PlayRewardSequence();
    /// }                              // auto-unlocks, even on exception
    /// </code>
    /// </example>
    public readonly struct InputLockHandle : IEquatable<InputLockHandle>, IDisposable
    {
        internal readonly InputLockService Service;
        internal readonly int Slot;
        internal readonly int Generation;

        internal InputLockHandle(InputLockService service, int slot, int generation)
        {
            Service = service;
            Slot = slot;
            Generation = generation;
        }

        /// <summary>An empty handle that owns no lock.</summary>
        public static InputLockHandle None => default;

        /// <summary>True while this handle still holds a live lock on its service.</summary>
        public bool IsActive => Service != null && Service.IsHandleActive(this);

        /// <summary>The caller that created the lock (compile-time captured), for tooling.</summary>
        public string Owner => Service != null ? Service.GetHandleOwner(this) : null;

        /// <summary>Releases this handle's contribution. Idempotent.</summary>
        public void Dispose()
        {
            Service?.Release(this);
        }

        public bool Equals(InputLockHandle other)
        {
            return ReferenceEquals(Service, other.Service)
                   && Slot == other.Slot
                   && Generation == other.Generation;
        }

        public override bool Equals(object obj) => obj is InputLockHandle other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Service != null ? Service.GetHashCode() : 0;
                hash = (hash * 397) ^ Slot;
                hash = (hash * 397) ^ Generation;
                return hash;
            }
        }
    }
}
