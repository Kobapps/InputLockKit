using System;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// A value-type handle to an input-lock category. Backed by an interned integer id
    /// (see <see cref="InputLockTagRegistry"/>) so equality, hashing and dictionary use are
    /// allocation-free and IL2CPP-friendly. Create one from a string (implicitly or via
    /// <see cref="Get"/>), from a <see cref="InputLockTagCatalog"/>, or use <see cref="Default"/>.
    /// </summary>
    [Serializable]
    public readonly struct InputLockTag : IEquatable<InputLockTag>
    {
        /// <summary>The interned id. <c>0</c> is the reserved <see cref="Default"/> tag.</summary>
        public readonly int Id;

        internal InputLockTag(int id)
        {
            Id = id;
        }

        /// <summary>The reserved catch-all tag (id 0). Equal to <c>default(InputLockTag)</c>.</summary>
        public static InputLockTag Default => new InputLockTag(0);

        /// <summary>The human-readable name this tag was interned under.</summary>
        public string Name => InputLockTagRegistry.NameOf(Id);

        /// <summary>Interns <paramref name="name"/> and returns its tag.</summary>
        public static InputLockTag Get(string name) => InputLockTagRegistry.Get(name);

        public static implicit operator InputLockTag(string name) => InputLockTagRegistry.Get(name);

        public bool Equals(InputLockTag other) => Id == other.Id;

        public override bool Equals(object obj) => obj is InputLockTag other && other.Id == Id;

        public override int GetHashCode() => Id;

        public override string ToString() => Name;

        public static bool operator ==(InputLockTag a, InputLockTag b) => a.Id == b.Id;

        public static bool operator !=(InputLockTag a, InputLockTag b) => a.Id != b.Id;
    }
}
