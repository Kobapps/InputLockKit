using System.Collections.Generic;
using NUnit.Framework;

namespace Kobapps.InputLockKit.Tests
{
    /// <summary>State adapter save/restore wiring and the re-entrancy guard.</summary>
    [TestFixture]
    public sealed class InputLockStateAdapterTests
    {
        private sealed class FakeAdapter : IInputLockStateAdapter
        {
            public readonly List<string> Saved = new List<string>();
            public readonly List<string> ToRestore = new List<string>();
            public int SaveCalls;

            public void OnLocksChanged(InputLockSnapshot snapshot)
            {
                SaveCalls++;
                Saved.Clear();
                for (var i = 0; i < snapshot.Count; i++)
                {
                    Saved.Add(snapshot[i].Name);
                }
            }

            public void Restore(IInputLockRestoreContext context)
            {
                for (var i = 0; i < ToRestore.Count; i++)
                {
                    context.Lock(ToRestore[i]);
                }
            }
        }

        [SetUp]
        public void SetUp() => InputLockTagRegistry.ResetForTests();

        [Test]
        public void OnLocksChanged_ReceivesLockedTags()
        {
            var adapter = new FakeAdapter();
            var service = new InputLockService(adapter);

            service.Lock("A");
            service.Lock("B");

            CollectionAssert.Contains(adapter.Saved, "A");
            CollectionAssert.Contains(adapter.Saved, "B");
        }

        [Test]
        public void Restore_ReappliesSavedLocks_WithoutTriggeringSave()
        {
            var adapter = new FakeAdapter();
            adapter.ToRestore.Add("A");
            adapter.ToRestore.Add("B");

            var service = new InputLockService(adapter);
            service.RestoreFromAdapter();

            Assert.IsTrue(service.IsLocked("A"));
            Assert.IsTrue(service.IsLocked("B"));
            Assert.AreEqual(0, adapter.SaveCalls, "Restore must not re-trigger a save (no loop).");
        }

        [Test]
        public void SaveResumesAfterRestore()
        {
            var adapter = new FakeAdapter();
            adapter.ToRestore.Add("A");
            var service = new InputLockService(adapter);
            service.RestoreFromAdapter();

            service.Lock("C"); // normal lock after restore should save

            Assert.AreEqual(1, adapter.SaveCalls);
            CollectionAssert.Contains(adapter.Saved, "A");
            CollectionAssert.Contains(adapter.Saved, "C");
        }
    }
}
