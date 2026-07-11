using System.Collections.Generic;
using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Base class for any component that should react to input locks. A lockable can be driven three
    /// ways, all reference-counted into a single locked state:
    /// <list type="bullet">
    /// <item>by <b>tag</b> — it locks when any of its declared tags is locked;</item>
    /// <item>by <b>group</b> — the service can lock a whole group of lockables at once;</item>
    /// <item>by <b>selection</b> — the service can lock an explicit set of lockables (e.g. picked cells).</item>
    /// </list>
    /// Tags and group can be set in the inspector or changed at runtime (dynamic cells). Authoring a
    /// new lockable is just subclassing this and overriding <see cref="OnLock"/> / <see cref="OnUnlock"/>.
    /// </summary>
    /// <remarks>
    /// Subscription happens once (on first enable) and is torn down on destroy. Do not call the
    /// service's lock/release from inside <see cref="OnLock"/> / <see cref="OnUnlock"/> — the service
    /// is main-thread, single-pass.
    /// </remarks>
    public abstract class InputLockableBehaviour : MonoBehaviour
    {
        [SerializeField]
        [InputLockTag]
        [Tooltip("Tags this component listens to. It locks when any of these is locked.")]
        private List<string> _tags = new List<string> { "Default" };

        [SerializeField]
        [InputLockGroup]
        [Tooltip("Optional group this lockable belongs to. Lets the service lock the whole group, the " +
                 "group-except-a-selection, or just a selection (e.g. grid cells).")]
        private string _group;

        [SerializeField]
        [Tooltip("Optional identifier for this lockable within its group (e.g. \"Cell_3_4\"). Shown in tooling.")]
        private string _id;

        [SerializeField]
        [Tooltip("If set, resolve the service through this provider instead of the global default.")]
        private MonoBehaviour _providerOverride; // expected to implement IInputLockServiceProvider

        [SerializeField]
        [Tooltip("When true, this component does NOT auto-subscribe; call Subscribe() yourself.")]
        private bool _manualSubscribe;

        private readonly List<int> _resolvedTagIds = new List<int>(4);
        private bool _tagsResolved;
        private int _groupId;
        private bool _groupResolved;

        private bool _subscribed;
        private int _lockCount;
        private IInputLockService _service;

        /// <summary>The tag names this component listens to.</summary>
        public IReadOnlyList<string> Tags => _tags;

        /// <summary>The group this lockable belongs to (<see cref="InputLockGroup.None"/> if none).</summary>
        public InputLockGroup Group => new InputLockGroup(ResolvedGroupId);

        /// <summary>Optional per-lockable identifier within its group.</summary>
        public string Identifier => _id;

        /// <summary>True while the component is in its locked state.</summary>
        public bool IsLocked { get; private set; }

        /// <summary>Resolved interned tag ids. Rebuilt lazily when the tag list changes.</summary>
        internal IReadOnlyList<int> ResolvedTagIds
        {
            get
            {
                if (!_tagsResolved)
                {
                    ResolveTags();
                }

                return _resolvedTagIds;
            }
        }

        /// <summary>Resolved interned group id (0 = none). Rebuilt lazily when the group changes.</summary>
        internal int ResolvedGroupId
        {
            get
            {
                if (!_groupResolved)
                {
                    _groupId = InputLockGroupRegistry.Get(_group).Id;
                    _groupResolved = true;
                }

                return _groupId;
            }
        }

        protected virtual void OnEnable()
        {
            if (!_manualSubscribe)
            {
                Subscribe();
            }
        }

        protected virtual void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>Subscribes to the resolved service. Safe to call more than once.</summary>
        public void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            ResolveTags();
            _ = ResolvedGroupId;
            _service = ResolveService();
            if (_service == null)
            {
                return;
            }

            _subscribed = true;
            _service.SubscribeLockable(this);
        }

        /// <summary>Unsubscribes from the service and returns to the unlocked state.</summary>
        public void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _subscribed = false;
            _service?.UnsubscribeLockable(this);
            ForceUnlock();
        }

        /// <summary>Replaces the tag list at runtime. Re-subscribes if already subscribed.</summary>
        public void SetTags(IReadOnlyList<string> tags)
        {
            _tags.Clear();
            if (tags != null)
            {
                for (var i = 0; i < tags.Count; i++)
                {
                    _tags.Add(tags[i]);
                }
            }

            _tagsResolved = false;
            Resubscribe();
        }

        /// <summary>Adds one tag at runtime (dynamic tags). Re-subscribes if already subscribed.</summary>
        public void AddTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || _tags.Contains(tag))
            {
                return;
            }

            _tags.Add(tag);
            _tagsResolved = false;
            Resubscribe();
        }

        /// <summary>Removes one tag at runtime. Re-subscribes if already subscribed.</summary>
        public void RemoveTag(string tag)
        {
            if (_tags.Remove(tag))
            {
                _tagsResolved = false;
                Resubscribe();
            }
        }

        /// <summary>
        /// Sets this lockable's group (and optional identifier) at runtime. Handy for dynamically
        /// spawned members such as grid cells. Re-subscribes if already subscribed so group membership
        /// updates immediately.
        /// </summary>
        public void SetGroup(string group, string identifier = null)
        {
            _group = group;
            if (identifier != null)
            {
                _id = identifier;
            }

            _groupResolved = false;
            Resubscribe();
        }

        private void Resubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            var service = _service;
            Unsubscribe();
            _service = service;
            Subscribe();
        }

        private IInputLockService ResolveService()
        {
            if (_providerOverride is IInputLockServiceProvider provider)
            {
                return provider.Service;
            }

            return InputLock.Service;
        }

        private void ResolveTags()
        {
            _resolvedTagIds.Clear();
            for (var i = 0; i < _tags.Count; i++)
            {
                var name = _tags[i];
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var id = InputLockTagRegistry.Get(name).Id;
                if (!_resolvedTagIds.Contains(id))
                {
                    _resolvedTagIds.Add(id);
                }
            }

            _tagsResolved = true;
        }

        // ---------------------------------------------------- Service-driven transitions

        /// <summary>Sets the initial locked contribution count when (re)subscribing.</summary>
        internal void InitializeLockedCount(int lockedContributions)
        {
            _lockCount = lockedContributions;
            var shouldLock = lockedContributions > 0;
            if (shouldLock == IsLocked)
            {
                return;
            }

            IsLocked = shouldLock;
            if (shouldLock)
            {
                OnLock();
            }
            else
            {
                OnUnlock();
            }
        }

        /// <summary>Adds one lock contribution (from a tag toggle, a group lock, or a selection lock).</summary>
        internal void AddLock()
        {
            if (++_lockCount == 1)
            {
                IsLocked = true;
                OnLock();
            }
        }

        /// <summary>Removes one lock contribution. Clamped so it never goes negative.</summary>
        internal void RemoveLock()
        {
            if (_lockCount > 0 && --_lockCount == 0)
            {
                IsLocked = false;
                OnUnlock();
            }
        }

        internal void OnServiceTagChanged(bool locked)
        {
            if (locked)
            {
                AddLock();
            }
            else
            {
                RemoveLock();
            }
        }

        internal void ForceUnlock()
        {
            _lockCount = 0;
            if (!IsLocked)
            {
                return;
            }

            IsLocked = false;
            OnUnlock();
        }

        /// <summary>Called when the component transitions into the locked state.</summary>
        protected abstract void OnLock();

        /// <summary>Called when the component transitions back to the unlocked state.</summary>
        protected abstract void OnUnlock();
    }
}
