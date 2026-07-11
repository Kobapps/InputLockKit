using System;
using NUnit.Framework;

namespace Kobapps.InputLockKit.Tests
{
    /// <summary>
    /// Guards NFR-1: Lock / Release / IsLocked must not allocate managed memory per call in steady
    /// state. Measured with <see cref="GC.GetAllocatedBytesForCurrentThread"/> amortized over many
    /// iterations — a real per-call allocation (LINQ, a new List, boxing) scales into the kilobytes
    /// and fails loudly, while one-off JIT / measurement noise amortizes to ~0 and passes.
    /// </summary>
    [TestFixture]
    public sealed class InputLockAllocationTests
    {
        private const int Iterations = 4096;
        private const long SlackBytes = 1024; // fixed slack for JIT / measurement noise

        private InputLockService _service;
        private InputLockTag _tag;

        [SetUp]
        public void SetUp()
        {
            InputLockTagRegistry.ResetForTests();
            _service = new InputLockService();
            _tag = InputLockTag.Get("Hot");
            _service.RegisterTag(_tag);

            // Warm up: force the record pool, tag buffer and dictionary buckets to exist and JIT paths.
            for (var i = 0; i < 64; i++)
            {
                var h = _service.Lock(_tag, "warmup");
                _service.Release(h);
            }
        }

        [Test]
        public void LockAndRelease_DoesNotAllocatePerCall()
        {
            var before = GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < Iterations; i++)
            {
                var handle = _service.Lock(_tag, "probe");
                _service.Release(handle);
            }

            var total = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(total, Is.LessThan(SlackBytes),
                $"Lock+Release allocated {total} bytes over {Iterations} iterations " +
                $"({(double)total / Iterations:F3} B/call) — expected ~0.");
        }

        [Test]
        public void IsLocked_DoesNotAllocatePerCall()
        {
            var handle = _service.Lock(_tag, "probe");

            var before = GC.GetAllocatedBytesForCurrentThread();
            var sink = false;
            for (var i = 0; i < Iterations; i++)
            {
                sink ^= _service.IsLocked(_tag);
            }

            var total = GC.GetAllocatedBytesForCurrentThread() - before;
            _service.Release(handle);

            Assert.That(total, Is.LessThan(SlackBytes),
                $"IsLocked allocated {total} bytes over {Iterations} iterations (sink={sink}).");
        }
    }
}
