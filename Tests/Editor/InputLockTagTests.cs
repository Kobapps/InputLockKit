using NUnit.Framework;

namespace Kobapps.InputLockKit.Tests
{
    /// <summary>Tag interning: stable ids, value equality, Default reservation.</summary>
    [TestFixture]
    public sealed class InputLockTagTests
    {
        [SetUp]
        public void SetUp() => InputLockTagRegistry.ResetForTests();

        [Test]
        public void SameName_ReturnsEqualTags()
        {
            InputLockTag a = "MainUi";
            InputLockTag b = InputLockTag.Get("MainUi");

            Assert.AreEqual(a, b);
            Assert.AreEqual(a.Id, b.Id);
            Assert.IsTrue(a == b);
        }

        [Test]
        public void DifferentNames_ReturnDifferentTags()
        {
            InputLockTag a = "A";
            InputLockTag b = "B";

            Assert.AreNotEqual(a, b);
            Assert.IsTrue(a != b);
        }

        [Test]
        public void DefaultTag_HasIdZeroAndMatchesDefaultStruct()
        {
            Assert.AreEqual(0, InputLockTag.Default.Id);
            Assert.AreEqual(default(InputLockTag), InputLockTag.Default);
            Assert.AreEqual("Default", InputLockTag.Default.Name);
        }

        [Test]
        public void NullOrEmptyName_CollapsesToDefault()
        {
            Assert.AreEqual(InputLockTag.Default, InputLockTag.Get(null));
            Assert.AreEqual(InputLockTag.Default, InputLockTag.Get(string.Empty));
        }

        [Test]
        public void Name_RoundTripsThroughRegistry()
        {
            InputLockTag tag = "Camera";
            Assert.AreEqual("Camera", tag.Name);
            Assert.IsTrue(InputLockTagRegistry.IsRegistered("Camera"));
        }
    }
}
