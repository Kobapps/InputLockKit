using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Kobapps.InputLockKit.Tests
{
    /// <summary>Reactive component behaviour driven through the global provider (integration).</summary>
    [TestFixture]
    public sealed class InputLockableBehaviourTests
    {
        private sealed class TestProvider : IInputLockServiceProvider
        {
            public IInputLockService Service { get; set; }
        }

        private sealed class CountingLockable : InputLockableBehaviour
        {
            public int LockCalls;
            public int UnlockCalls;
            protected override void OnLock() => LockCalls++;
            protected override void OnUnlock() => UnlockCalls++;
        }

        private InputLockService _service;
        private TestProvider _provider;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            InputLockTagRegistry.ResetForTests();
            _service = new InputLockService();
            _provider = new TestProvider { Service = _service };
            InputLock.SetProvider(_provider);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    Object.DestroyImmediate(_spawned[i]);
                }
            }

            _spawned.Clear();
            InputLock.ClearProvider();
        }

        private CountingLockable NewLockable(params string[] tags)
        {
            var go = new GameObject("lockable");
            _spawned.Add(go);
            var lockable = go.AddComponent<CountingLockable>();
            lockable.SetTags(tags);
            // In edit mode Unity does not invoke OnEnable on AddComponent, so subscribe explicitly.
            // (In play mode the component auto-subscribes via OnEnable; this call is then a no-op.)
            lockable.Subscribe();
            return lockable;
        }

        [Test]
        public void Lockable_LocksWhenItsTagLocks()
        {
            var lockable = NewLockable("UI");

            _service.Lock("UI");

            Assert.IsTrue(lockable.IsLocked);
            Assert.AreEqual(1, lockable.LockCalls);
        }

        [Test]
        public void Lockable_WithMultipleTags_LocksIfAnyLocked_UnlocksWhenAllClear()
        {
            var lockable = NewLockable("A", "B");

            var a = _service.Lock("A");
            var b = _service.Lock("B");
            Assert.IsTrue(lockable.IsLocked);
            Assert.AreEqual(1, lockable.LockCalls, "Only one OnLock across overlapping tags.");

            _service.Release(a);
            Assert.IsTrue(lockable.IsLocked, "Still locked via B.");

            _service.Release(b);
            Assert.IsFalse(lockable.IsLocked);
            Assert.AreEqual(1, lockable.UnlockCalls);
        }

        [Test]
        public void Lockable_SubscribingWhileAlreadyLocked_SyncsToLockedState()
        {
            _service.Lock("UI");           // lock first
            var lockable = NewLockable("UI"); // then subscribe

            Assert.IsTrue(lockable.IsLocked, "Late subscriber must adopt the current locked state.");
            Assert.AreEqual(1, lockable.LockCalls);
        }

        [Test]
        public void Unsubscribe_StopsReceivingUpdatesAndUnlocks()
        {
            var lockable = NewLockable("UI");
            _service.Lock("UI");
            Assert.IsTrue(lockable.IsLocked);

            lockable.Unsubscribe();
            Assert.IsFalse(lockable.IsLocked, "Unsubscribe returns to unlocked state.");
        }
    }
}
