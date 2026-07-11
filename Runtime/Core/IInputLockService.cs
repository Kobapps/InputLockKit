using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// The core tag-based input locking service. Any number of independent systems may lock the same
    /// tag; input for that tag stays locked until every requester releases its handle (reference
    /// counted). The service is a plain object — construct it directly, bind it in a DI container, or
    /// use the drop-in <see cref="InputLock"/> facade.
    /// </summary>
    public interface IInputLockService
    {
        /// <summary>Locks a single tag and returns a handle that releases only this request.</summary>
        InputLockHandle Lock(InputLockTag tag, [CallerFilePath] string owner = null);

        /// <summary>Locks several tags at once with one handle.</summary>
        InputLockHandle Lock(InputLockTag[] tags, string owner = null);

        /// <summary>Locks every currently-known tag (catalog + anything referenced so far).</summary>
        InputLockHandle LockAll(string owner = null);

        /// <summary>Locks every known tag except those in <paramref name="except"/>.</summary>
        InputLockHandle LockAllExcept(InputLockTag[] except, string owner = null);

        /// <summary>Locks an explicit selection of lockables (e.g. picked grid cells).</summary>
        InputLockHandle LockOnly(IReadOnlyList<InputLockableBehaviour> lockables, string owner = null);

        /// <summary>Locks every lockable currently registered in <paramref name="group"/>.</summary>
        InputLockHandle LockGroup(InputLockGroup group, string owner = null);

        /// <summary>Locks every member of a group except those in <paramref name="except"/>.</summary>
        InputLockHandle LockGroupExcept(
            InputLockGroup group, IReadOnlyList<InputLockableBehaviour> except, string owner = null);

        /// <summary>The lockables currently registered in <paramref name="group"/> (live view).</summary>
        IReadOnlyList<InputLockableBehaviour> GetGroupMembers(InputLockGroup group);

        /// <summary>Releases a handle's contribution. Stale / double release is a safe no-op.</summary>
        void Release(InputLockHandle handle);

        /// <summary>True if <paramref name="tag"/> has at least one active lock.</summary>
        bool IsLocked(InputLockTag tag);

        /// <summary>Current reference count (number of live handles) holding <paramref name="tag"/>.</summary>
        int GetLockCount(InputLockTag tag);

        /// <summary>True if any tag is currently locked.</summary>
        bool IsAnyLocked { get; }

        /// <summary>Ensures a tag is part of the known set (so it participates in <see cref="LockAll"/>).</summary>
        void RegisterTag(InputLockTag tag);

        /// <summary>Interns and registers every tag in a catalog as known.</summary>
        void RegisterCatalog(InputLockTagCatalog catalog);

        /// <summary>Registers a reactive component so it is driven by the tags it declares.</summary>
        void SubscribeLockable(InputLockableBehaviour lockable);

        /// <summary>Stops driving a previously subscribed component.</summary>
        void UnsubscribeLockable(InputLockableBehaviour lockable);

        /// <summary>Asks the state adapter to re-apply any persisted locks.</summary>
        void RestoreFromAdapter();

        /// <summary>Releases every lock and clears all state. Outstanding handles become inert.</summary>
        void Reset();

        /// <summary>Raised whenever a tag transitions between locked and unlocked.</summary>
        event Action<InputLockTag, bool> TagStateChanged;

        /// <summary>Raised after any change to the set of locks. Used by tooling and adapters.</summary>
        event Action LocksChanged;

        /// <summary>All tag ids the service currently knows about. For tooling.</summary>
        IReadOnlyList<int> KnownTagIds { get; }

        /// <summary>The tags currently locked, as a live read-only view. For tooling / adapters.</summary>
        InputLockSnapshot Snapshot { get; }
    }
}
