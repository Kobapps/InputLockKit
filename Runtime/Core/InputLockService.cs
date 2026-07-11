using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Default <see cref="IInputLockService"/> implementation. Reference-counts locks per tag, pools
    /// lock records and their buffers, and pushes changes only to the components affected by a change —
    /// so steady-state Lock / Release / IsLocked are allocation-free. Beyond tag locks it also supports
    /// locking a whole <b>group</b> of lockables, a group-except-a-selection, or an explicit selection.
    /// </summary>
    /// <remarks>Main-thread affine. Not safe for concurrent access from multiple threads.</remarks>
    public sealed class InputLockService : IInputLockService, IInputLockRestoreContext
    {
        private sealed class LockRecord
        {
            public int Generation = 1; // starts at 1 so a default handle (gen 0) never matches
            public bool Active;
            public int[] Tags = new int[4];
            public int TagCount;
            public InputLockableBehaviour[] Direct = new InputLockableBehaviour[4];
            public int DirectCount;
            public string Owner;
        }

        private readonly List<LockRecord> _records = new List<LockRecord>(32);
        private readonly Stack<int> _freeSlots = new Stack<int>(32);

        private readonly Dictionary<int, int> _tagRefCount = new Dictionary<int, int>(64);
        private readonly Dictionary<int, List<InputLockableBehaviour>> _tagSubscribers =
            new Dictionary<int, List<InputLockableBehaviour>>(64);
        private readonly Dictionary<int, List<InputLockableBehaviour>> _groupMembers =
            new Dictionary<int, List<InputLockableBehaviour>>(16);
        private readonly List<InputLockableBehaviour> _subscribers = new List<InputLockableBehaviour>(64);

        private readonly List<int> _knownTagList = new List<int>(64);
        private readonly HashSet<int> _knownTagSet = new HashSet<int>();
        private readonly List<int> _lockedTagIds = new List<int>(16);

        // Reusable scratch for group operations (never returned to callers).
        private readonly List<InputLockableBehaviour> _directBuffer = new List<InputLockableBehaviour>(32);

        private readonly IInputLockStateAdapter _adapter;
        private readonly bool _hasStateAdapter;
        private bool _restoring;

        private int _activeLockCount;

        public InputLockService() : this(null) { }

        public InputLockService(IInputLockStateAdapter adapter)
        {
            _adapter = adapter ?? NullInputLockStateAdapter.Instance;
            _hasStateAdapter = adapter != null && !(adapter is NullInputLockStateAdapter);
            RegisterTag(InputLockTag.Default);
        }

        public event Action<InputLockTag, bool> TagStateChanged;
        public event Action LocksChanged;

        public bool IsAnyLocked => _activeLockCount > 0;
        public IReadOnlyList<int> KnownTagIds => _knownTagList;
        public InputLockSnapshot Snapshot => new InputLockSnapshot(_lockedTagIds);

        // ------------------------------------------------------------------ Known tags

        public void RegisterTag(InputLockTag tag)
        {
            if (_knownTagSet.Add(tag.Id))
            {
                _knownTagList.Add(tag.Id);
                if (!_tagRefCount.ContainsKey(tag.Id))
                {
                    _tagRefCount.Add(tag.Id, 0);
                }
            }
        }

        public void RegisterCatalog(InputLockTagCatalog catalog)
        {
            if (catalog == null)
            {
                return;
            }

            var entries = catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var name = entries[i].Name;
                if (!string.IsNullOrEmpty(name))
                {
                    RegisterTag(InputLockTagRegistry.Get(name));
                }
            }
        }

        // ------------------------------------------------------------------ Tag locking

        public InputLockHandle Lock(InputLockTag tag, [CallerFilePath] string owner = null)
        {
            RegisterTag(tag); // any tag you lock becomes visible in tooling + covered by LockAll
            var slot = AcquireSlot(owner);
            var record = _records[slot];
            record.Tags[0] = tag.Id;
            record.TagCount = 1;

            ApplyLock(tag.Id);
            AfterMutation(togglesOccurred: true);
            return new InputLockHandle(this, slot, record.Generation);
        }

        public InputLockHandle Lock(InputLockTag[] tags, string owner = null)
        {
            if (tags == null || tags.Length == 0)
            {
                return InputLockHandle.None;
            }

            var slot = AcquireSlot(owner);
            var record = _records[slot];
            EnsureTagCapacity(record, tags.Length);
            record.TagCount = tags.Length;

            var togglesOccurred = false;
            for (var i = 0; i < tags.Length; i++)
            {
                var id = tags[i].Id;
                RegisterTag(tags[i]);
                record.Tags[i] = id;
                togglesOccurred |= ApplyLock(id);
            }

            AfterMutation(togglesOccurred);
            return new InputLockHandle(this, slot, record.Generation);
        }

        public InputLockHandle LockAll(string owner = null)
        {
            var slot = AcquireSlot(owner);
            var record = _records[slot];
            var count = _knownTagList.Count;
            EnsureTagCapacity(record, count);
            record.TagCount = count;

            var togglesOccurred = false;
            for (var i = 0; i < count; i++)
            {
                var id = _knownTagList[i];
                record.Tags[i] = id;
                togglesOccurred |= ApplyLock(id);
            }

            AfterMutation(togglesOccurred);
            return new InputLockHandle(this, slot, record.Generation);
        }

        public InputLockHandle LockAllExcept(InputLockTag[] except, string owner = null)
        {
            var slot = AcquireSlot(owner);
            var record = _records[slot];
            EnsureTagCapacity(record, _knownTagList.Count);

            var written = 0;
            var togglesOccurred = false;
            for (var i = 0; i < _knownTagList.Count; i++)
            {
                var id = _knownTagList[i];
                if (ContainsId(except, id))
                {
                    continue;
                }

                record.Tags[written++] = id;
                togglesOccurred |= ApplyLock(id);
            }

            record.TagCount = written;
            AfterMutation(togglesOccurred);
            return new InputLockHandle(this, slot, record.Generation);
        }

        // ------------------------------------------------------------------ Group / selection locking

        public InputLockHandle LockOnly(IReadOnlyList<InputLockableBehaviour> lockables, string owner = null)
        {
            return LockLockablesInternal(lockables, owner);
        }

        public InputLockHandle LockGroup(InputLockGroup group, string owner = null)
        {
            if (!group.IsValid || !_groupMembers.TryGetValue(group.Id, out var members))
            {
                return InputLockHandle.None;
            }

            _directBuffer.Clear();
            _directBuffer.AddRange(members);
            return LockLockablesInternal(_directBuffer, owner);
        }

        public InputLockHandle LockGroupExcept(
            InputLockGroup group, IReadOnlyList<InputLockableBehaviour> except, string owner = null)
        {
            if (!group.IsValid || !_groupMembers.TryGetValue(group.Id, out var members))
            {
                return InputLockHandle.None;
            }

            _directBuffer.Clear();
            for (var i = 0; i < members.Count; i++)
            {
                var member = members[i];
                if (!ContainsReference(except, member))
                {
                    _directBuffer.Add(member);
                }
            }

            return LockLockablesInternal(_directBuffer, owner);
        }

        public IReadOnlyList<InputLockableBehaviour> GetGroupMembers(InputLockGroup group)
        {
            return group.IsValid && _groupMembers.TryGetValue(group.Id, out var members)
                ? members
                : Array.Empty<InputLockableBehaviour>();
        }

        private InputLockHandle LockLockablesInternal(IReadOnlyList<InputLockableBehaviour> targets, string owner)
        {
            if (targets == null || targets.Count == 0)
            {
                return InputLockHandle.None;
            }

            var slot = AcquireSlot(owner);
            var record = _records[slot];
            EnsureDirectCapacity(record, targets.Count);

            var written = 0;
            for (var i = 0; i < targets.Count; i++)
            {
                var lockable = targets[i];
                if (lockable == null)
                {
                    continue;
                }

                record.Direct[written++] = lockable;
                lockable.AddLock();
            }

            if (written == 0)
            {
                // Nothing to lock — recycle the slot we optimistically acquired.
                record.Active = false;
                record.Owner = null;
                record.Generation++;
                _freeSlots.Push(slot);
                _activeLockCount--;
                return InputLockHandle.None;
            }

            record.DirectCount = written;
            LocksChanged?.Invoke();
            return new InputLockHandle(this, slot, record.Generation);
        }

        // ------------------------------------------------------------------ Release

        public void Release(InputLockHandle handle)
        {
            if (!ReferenceEquals(handle.Service, this))
            {
                return;
            }

            if ((uint)handle.Slot >= (uint)_records.Count)
            {
                return;
            }

            var record = _records[handle.Slot];
            if (!record.Active || record.Generation != handle.Generation)
            {
                return; // stale or double release — safe no-op
            }

            var togglesOccurred = false;
            for (var i = 0; i < record.TagCount; i++)
            {
                togglesOccurred |= ApplyUnlock(record.Tags[i]);
            }

            for (var i = 0; i < record.DirectCount; i++)
            {
                var lockable = record.Direct[i];
                record.Direct[i] = null;
                if (lockable != null)
                {
                    lockable.RemoveLock();
                }
            }

            record.Active = false;
            record.TagCount = 0;
            record.DirectCount = 0;
            record.Owner = null;
            record.Generation++;
            _freeSlots.Push(handle.Slot);
            _activeLockCount--;

            AfterMutation(togglesOccurred);
        }

        // ------------------------------------------------------------------ Queries

        public bool IsLocked(InputLockTag tag)
        {
            return _tagRefCount.TryGetValue(tag.Id, out var count) && count > 0;
        }

        public int GetLockCount(InputLockTag tag)
        {
            return _tagRefCount.TryGetValue(tag.Id, out var count) ? count : 0;
        }

        // ------------------------------------------------------------------ Subscribers

        public void SubscribeLockable(InputLockableBehaviour lockable)
        {
            if (lockable == null)
            {
                return;
            }

            if (!_subscribers.Contains(lockable))
            {
                _subscribers.Add(lockable);
            }

            var tagIds = lockable.ResolvedTagIds;
            var lockedContributions = 0;
            for (var i = 0; i < tagIds.Count; i++)
            {
                var id = tagIds[i];
                RegisterTag(new InputLockTag(id));

                if (!_tagSubscribers.TryGetValue(id, out var list))
                {
                    list = new List<InputLockableBehaviour>(4);
                    _tagSubscribers.Add(id, list);
                }

                if (!list.Contains(lockable))
                {
                    list.Add(lockable);
                }

                if (_tagRefCount.TryGetValue(id, out var count) && count > 0)
                {
                    lockedContributions++;
                }
            }

            var groupId = lockable.ResolvedGroupId;
            if (groupId > 0)
            {
                if (!_groupMembers.TryGetValue(groupId, out var members))
                {
                    members = new List<InputLockableBehaviour>(8);
                    _groupMembers.Add(groupId, members);
                }

                if (!members.Contains(lockable))
                {
                    members.Add(lockable);
                }
            }

            lockable.InitializeLockedCount(lockedContributions);
        }

        public void UnsubscribeLockable(InputLockableBehaviour lockable)
        {
            if (lockable == null)
            {
                return;
            }

            _subscribers.Remove(lockable);

            var tagIds = lockable.ResolvedTagIds;
            for (var i = 0; i < tagIds.Count; i++)
            {
                if (_tagSubscribers.TryGetValue(tagIds[i], out var list))
                {
                    list.Remove(lockable);
                }
            }

            var groupId = lockable.ResolvedGroupId;
            if (groupId > 0 && _groupMembers.TryGetValue(groupId, out var members))
            {
                members.Remove(lockable);
            }
        }

        // ------------------------------------------------------------------ State adapter

        public void RestoreFromAdapter()
        {
            if (_restoring)
            {
                return;
            }

            _restoring = true;
            try
            {
                _adapter.Restore(this);
            }
            finally
            {
                _restoring = false;
            }
        }

        InputLockHandle IInputLockRestoreContext.Lock(InputLockTag tag, string owner)
        {
            RegisterTag(tag);
            var slot = AcquireSlot(owner);
            var record = _records[slot];
            record.Tags[0] = tag.Id;
            record.TagCount = 1;
            ApplyLock(tag.Id);
            AfterMutation(togglesOccurred: true);
            return new InputLockHandle(this, slot, record.Generation);
        }

        // ------------------------------------------------------------------ Lifecycle

        public void Reset()
        {
            for (var i = 0; i < _subscribers.Count; i++)
            {
                if (_subscribers[i] != null)
                {
                    _subscribers[i].ForceUnlock();
                }
            }

            for (var slot = 0; slot < _records.Count; slot++)
            {
                var record = _records[slot];
                if (!record.Active)
                {
                    continue;
                }

                for (var i = 0; i < record.DirectCount; i++)
                {
                    record.Direct[i] = null;
                }

                record.Active = false;
                record.TagCount = 0;
                record.DirectCount = 0;
                record.Owner = null;
                record.Generation++;
            }

            _freeSlots.Clear();
            for (var slot = 0; slot < _records.Count; slot++)
            {
                _freeSlots.Push(slot);
            }

            _tagRefCount.Clear();
            for (var i = 0; i < _knownTagList.Count; i++)
            {
                _tagRefCount[_knownTagList[i]] = 0;
            }

            _lockedTagIds.Clear();
            _activeLockCount = 0;

            LocksChanged?.Invoke();
        }

        // ------------------------------------------------------------------ Internals

        private int AcquireSlot(string owner)
        {
            int slot;
            LockRecord record;
            if (_freeSlots.Count > 0)
            {
                slot = _freeSlots.Pop();
                record = _records[slot];
            }
            else
            {
                record = new LockRecord();
                slot = _records.Count;
                _records.Add(record);
            }

            record.Active = true;
            record.TagCount = 0;
            record.DirectCount = 0;
            record.Owner = string.IsNullOrEmpty(owner) ? "Unknown" : owner;
            _activeLockCount++;
            return slot;
        }

        private static void EnsureTagCapacity(LockRecord record, int needed)
        {
            if (record.Tags.Length < needed)
            {
                var newSize = record.Tags.Length;
                while (newSize < needed)
                {
                    newSize <<= 1;
                }

                record.Tags = new int[newSize];
            }
        }

        private static void EnsureDirectCapacity(LockRecord record, int needed)
        {
            if (record.Direct.Length < needed)
            {
                var newSize = record.Direct.Length;
                while (newSize < needed)
                {
                    newSize <<= 1;
                }

                record.Direct = new InputLockableBehaviour[newSize];
            }
        }

        private bool ApplyLock(int id)
        {
            _tagRefCount.TryGetValue(id, out var count);
            count++;
            _tagRefCount[id] = count;

            if (count != 1)
            {
                return false;
            }

            _lockedTagIds.Add(id);
            NotifySubscribers(id, true);
            TagStateChanged?.Invoke(new InputLockTag(id), true);
            return true;
        }

        private bool ApplyUnlock(int id)
        {
            if (!_tagRefCount.TryGetValue(id, out var count) || count == 0)
            {
                return false;
            }

            count--;
            _tagRefCount[id] = count;

            if (count != 0)
            {
                return false;
            }

            RemoveLockedTag(id);
            NotifySubscribers(id, false);
            TagStateChanged?.Invoke(new InputLockTag(id), false);
            return true;
        }

        private void NotifySubscribers(int id, bool locked)
        {
            if (!_tagSubscribers.TryGetValue(id, out var list))
            {
                return;
            }

            var count = list.Count;
            for (var i = 0; i < count; i++)
            {
                list[i].OnServiceTagChanged(locked);
            }
        }

        private void RemoveLockedTag(int id)
        {
            var last = _lockedTagIds.Count - 1;
            for (var i = 0; i <= last; i++)
            {
                if (_lockedTagIds[i] == id)
                {
                    _lockedTagIds[i] = _lockedTagIds[last];
                    _lockedTagIds.RemoveAt(last);
                    return;
                }
            }
        }

        private void AfterMutation(bool togglesOccurred)
        {
            LocksChanged?.Invoke();

            if (_hasStateAdapter && togglesOccurred && !_restoring)
            {
                _adapter.OnLocksChanged(new InputLockSnapshot(_lockedTagIds));
            }
        }

        private static bool ContainsId(InputLockTag[] tags, int id)
        {
            if (tags == null)
            {
                return false;
            }

            for (var i = 0; i < tags.Length; i++)
            {
                if (tags[i].Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsReference(IReadOnlyList<InputLockableBehaviour> list, InputLockableBehaviour item)
        {
            if (list == null)
            {
                return false;
            }

            for (var i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool IsHandleActive(InputLockHandle handle)
        {
            if ((uint)handle.Slot >= (uint)_records.Count)
            {
                return false;
            }

            var record = _records[handle.Slot];
            return record.Active && record.Generation == handle.Generation;
        }

        internal string GetHandleOwner(InputLockHandle handle)
        {
            if ((uint)handle.Slot >= (uint)_records.Count)
            {
                return null;
            }

            var record = _records[handle.Slot];
            return record.Generation == handle.Generation ? record.Owner : null;
        }

        // ------------------------------------------------------------------ Tooling (editor/tests)

        internal int ActiveLockCount => _activeLockCount;

        internal IReadOnlyList<InputLockableBehaviour> GetSubscribers(int tagId)
        {
            return _tagSubscribers.TryGetValue(tagId, out var list) ? list : null;
        }

        internal IReadOnlyCollection<int> GroupIds => _groupMembers.Keys;

        internal void CollectOwners(int tagId, List<string> buffer)
        {
            buffer.Clear();
            for (var slot = 0; slot < _records.Count; slot++)
            {
                var record = _records[slot];
                if (!record.Active)
                {
                    continue;
                }

                for (var i = 0; i < record.TagCount; i++)
                {
                    if (record.Tags[i] == tagId)
                    {
                        buffer.Add(record.Owner);
                        break;
                    }
                }
            }
        }

        internal void DebugForceUnlockTag(int tagId)
        {
            for (var slot = 0; slot < _records.Count; slot++)
            {
                var record = _records[slot];
                if (!record.Active)
                {
                    continue;
                }

                var holdsTag = false;
                for (var i = 0; i < record.TagCount; i++)
                {
                    if (record.Tags[i] == tagId)
                    {
                        holdsTag = true;
                        break;
                    }
                }

                if (!holdsTag)
                {
                    continue;
                }

                var togglesOccurred = false;
                for (var i = 0; i < record.TagCount; i++)
                {
                    togglesOccurred |= ApplyUnlock(record.Tags[i]);
                }

                for (var i = 0; i < record.DirectCount; i++)
                {
                    var lockable = record.Direct[i];
                    record.Direct[i] = null;
                    if (lockable != null)
                    {
                        lockable.RemoveLock();
                    }
                }

                record.Active = false;
                record.TagCount = 0;
                record.DirectCount = 0;
                record.Owner = null;
                record.Generation++;
                _freeSlots.Push(slot);
                _activeLockCount--;
                AfterMutation(togglesOccurred);
            }
        }
    }
}
