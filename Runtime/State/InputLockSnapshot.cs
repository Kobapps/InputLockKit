using System.Collections.Generic;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// A cheap, allocation-free read-only view over the set of tags that are currently locked.
    /// Passed to <see cref="IInputLockStateAdapter.OnLocksChanged"/> so games can persist lock state
    /// without the service having to build a list per change. The view is only valid for the duration
    /// of the callback; copy out the names you need to keep.
    /// </summary>
    public readonly struct InputLockSnapshot
    {
        private readonly IReadOnlyList<int> _lockedTagIds;

        internal InputLockSnapshot(IReadOnlyList<int> lockedTagIds)
        {
            _lockedTagIds = lockedTagIds;
        }

        /// <summary>Number of tags currently locked.</summary>
        public int Count => _lockedTagIds?.Count ?? 0;

        /// <summary>The locked tag at <paramref name="index"/>.</summary>
        public InputLockTag this[int index] => new InputLockTag(_lockedTagIds[index]);
    }
}
