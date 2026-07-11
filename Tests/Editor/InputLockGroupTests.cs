using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Kobapps.InputLockKit.Tests
{
    /// <summary>Groups, selection locking and dynamic tags.</summary>
    [TestFixture]
    public sealed class InputLockGroupTests
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
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            InputLockTagRegistry.ResetForTests();
            InputLockGroupRegistry.ResetForTests();
            _service = new InputLockService();
            InputLock.SetProvider(new TestProvider { Service = _service });
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

        private CountingLockable NewCell(string group, string id, params string[] tags)
        {
            var go = new GameObject("cell");
            _spawned.Add(go);
            var cell = go.AddComponent<CountingLockable>();
            if (tags != null && tags.Length > 0)
            {
                cell.SetTags(tags);
            }

            if (group != null)
            {
                cell.SetGroup(group, id);
            }

            cell.Subscribe(); // explicit: edit mode doesn't call OnEnable on AddComponent
            return cell;
        }

        [Test]
        public void LockGroup_LocksEveryMember()
        {
            var a = NewCell("Grid", "a");
            var b = NewCell("Grid", "b");

            var handle = _service.LockGroup("Grid");

            Assert.IsTrue(a.IsLocked);
            Assert.IsTrue(b.IsLocked);

            _service.Release(handle);
            Assert.IsFalse(a.IsLocked);
            Assert.IsFalse(b.IsLocked);
        }

        [Test]
        public void LockGroupExcept_LeavesSelectionUnlocked()
        {
            var a = NewCell("Grid", "a");
            var b = NewCell("Grid", "b");
            var c = NewCell("Grid", "c");

            _service.LockGroupExcept("Grid", new InputLockableBehaviour[] { b });

            Assert.IsTrue(a.IsLocked);
            Assert.IsFalse(b.IsLocked, "Excepted member stays unlocked.");
            Assert.IsTrue(c.IsLocked);
        }

        [Test]
        public void LockOnly_LocksExactlyTheSelection()
        {
            var a = NewCell("Grid", "a");
            var b = NewCell("Grid", "b");
            var c = NewCell("Grid", "c");

            var handle = _service.LockOnly(new InputLockableBehaviour[] { a, c });

            Assert.IsTrue(a.IsLocked);
            Assert.IsFalse(b.IsLocked);
            Assert.IsTrue(c.IsLocked);

            _service.Release(handle);
            Assert.IsFalse(a.IsLocked);
            Assert.IsFalse(c.IsLocked);
        }

        [Test]
        public void GetGroupMembers_ReturnsCurrentMembers()
        {
            NewCell("Grid", "a");
            NewCell("Grid", "b");
            NewCell("Other", "x");

            Assert.AreEqual(2, _service.GetGroupMembers("Grid").Count);
            Assert.AreEqual(1, _service.GetGroupMembers("Other").Count);
        }

        [Test]
        public void TagLockAndGroupLock_ShareOneRefCount()
        {
            var a = NewCell("Grid", "a", "cell_a");

            var tagHandle = _service.Lock("cell_a"); // lock via its tag
            var groupHandle = _service.LockGroup("Grid"); // and via group
            Assert.IsTrue(a.IsLocked);
            Assert.AreEqual(1, a.LockCalls, "Only one OnLock across both lock sources.");

            _service.Release(tagHandle);
            Assert.IsTrue(a.IsLocked, "Still locked via the group.");

            _service.Release(groupHandle);
            Assert.IsFalse(a.IsLocked);
            Assert.AreEqual(1, a.UnlockCalls);
        }

        [Test]
        public void AddTag_AtRuntime_MakesLockableReactToNewTag()
        {
            var a = NewCell(null, null, "start");

            _service.Lock("dynamic");
            Assert.IsFalse(a.IsLocked, "Not listening to 'dynamic' yet.");

            a.AddTag("dynamic"); // dynamic tag added at runtime → re-subscribes, syncs to current state
            Assert.IsTrue(a.IsLocked, "Now listening to the already-locked 'dynamic' tag.");
        }

        [Test]
        public void SetGroup_AtRuntime_RegistersMembership()
        {
            var a = NewCell(null, null, "t");
            Assert.AreEqual(0, _service.GetGroupMembers("Grid").Count);

            a.SetGroup("Grid", "a");
            Assert.AreEqual(1, _service.GetGroupMembers("Grid").Count);

            _service.LockGroup("Grid");
            Assert.IsTrue(a.IsLocked);
        }
    }
}
