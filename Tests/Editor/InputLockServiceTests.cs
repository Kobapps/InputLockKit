using NUnit.Framework;

namespace Kobapps.InputLockKit.Tests
{
    /// <summary>Core service behaviour: ref-counting, handles, LockAll(Except), queries, Reset.</summary>
    [TestFixture]
    public sealed class InputLockServiceTests
    {
        private InputLockService _service;

        [SetUp]
        public void SetUp()
        {
            InputLockTagRegistry.ResetForTests();
            _service = new InputLockService();
        }

        [Test]
        public void Lock_SingleTag_TagBecomesLocked()
        {
            var handle = _service.Lock("UI");

            Assert.IsTrue(_service.IsLocked("UI"));
            Assert.IsTrue(handle.IsActive);
            Assert.AreEqual(1, _service.GetLockCount("UI"));
        }

        [Test]
        public void Release_LastHandle_TagBecomesUnlocked()
        {
            var handle = _service.Lock("UI");
            _service.Release(handle);

            Assert.IsFalse(_service.IsLocked("UI"));
            Assert.IsFalse(handle.IsActive);
        }

        [Test]
        public void RefCount_TwoHandlesOneTag_StaysLockedUntilBothReleased()
        {
            var a = _service.Lock("UI");
            var b = _service.Lock("UI");
            Assert.AreEqual(2, _service.GetLockCount("UI"));

            _service.Release(a);
            Assert.IsTrue(_service.IsLocked("UI"), "One owner still holds the lock.");

            _service.Release(b);
            Assert.IsFalse(_service.IsLocked("UI"));
        }

        [Test]
        public void Release_IsIdempotent_DoubleReleaseIsNoOp()
        {
            var a = _service.Lock("UI");
            var b = _service.Lock("UI");

            _service.Release(a);
            _service.Release(a); // double release must not decrement b's contribution

            Assert.IsTrue(_service.IsLocked("UI"));
            Assert.AreEqual(1, _service.GetLockCount("UI"));

            _service.Release(b);
            Assert.IsFalse(_service.IsLocked("UI"));
        }

        [Test]
        public void Dispose_ReleasesHandle()
        {
            using (_service.Lock("UI"))
            {
                Assert.IsTrue(_service.IsLocked("UI"));
            }

            Assert.IsFalse(_service.IsLocked("UI"));
        }

        [Test]
        public void LockMultiple_LocksEveryProvidedTag()
        {
            var handle = _service.Lock(new InputLockTag[] { "A", "B", "C" });

            Assert.IsTrue(_service.IsLocked("A"));
            Assert.IsTrue(_service.IsLocked("B"));
            Assert.IsTrue(_service.IsLocked("C"));

            _service.Release(handle);
            Assert.IsFalse(_service.IsLocked("A"));
            Assert.IsFalse(_service.IsLocked("C"));
        }

        [Test]
        public void LockAll_LocksEveryKnownTag()
        {
            _service.RegisterTag("A");
            _service.RegisterTag("B");

            var handle = _service.LockAll();

            Assert.IsTrue(_service.IsLocked("A"));
            Assert.IsTrue(_service.IsLocked("B"));
            Assert.IsTrue(_service.IsLocked(InputLockTag.Default));

            _service.Release(handle);
            Assert.IsFalse(_service.IsAnyLocked);
        }

        [Test]
        public void LockAllExcept_LeavesExcludedTagsOpen()
        {
            _service.RegisterTag("A");
            _service.RegisterTag("B");

            _service.LockAllExcept(new InputLockTag[] { "A" });

            Assert.IsFalse(_service.IsLocked("A"), "Excluded tag must stay open.");
            Assert.IsTrue(_service.IsLocked("B"));
        }

        [Test]
        public void Reset_ClearsAllLocksAndInvalidatesHandles()
        {
            var handle = _service.Lock("UI"); // "UI" locked without prior RegisterTag

            _service.Reset();

            Assert.IsFalse(_service.IsAnyLocked);
            Assert.IsFalse(_service.IsLocked("UI"), "Ephemeral tag counts must clear on Reset.");
            Assert.IsFalse(handle.IsActive, "Handles from before Reset are inert.");

            // Releasing a stale handle after reset is a safe no-op.
            Assert.DoesNotThrow(() => _service.Release(handle));
        }

        [Test]
        public void EmptyTagArray_ReturnsNoneHandle()
        {
            var handle = _service.Lock(new InputLockTag[0]);
            Assert.IsFalse(handle.IsActive);
            Assert.IsFalse(_service.IsAnyLocked);
        }

        [Test]
        public void TagStateChanged_FiresOnlyOnTransition()
        {
            var lockedEvents = 0;
            var unlockedEvents = 0;
            _service.TagStateChanged += (tag, locked) =>
            {
                if (locked) lockedEvents++;
                else unlockedEvents++;
            };

            var a = _service.Lock("UI");
            var b = _service.Lock("UI"); // second lock: no new transition
            Assert.AreEqual(1, lockedEvents);

            _service.Release(a); // still locked: no transition
            Assert.AreEqual(0, unlockedEvents);

            _service.Release(b); // now unlocked: one transition
            Assert.AreEqual(1, unlockedEvents);
        }
    }
}
