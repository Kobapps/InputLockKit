using System;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// A value-type identifier for a group of lockables (e.g. all cells of one grid). Backed by an
    /// interned integer id (see <see cref="InputLockGroupRegistry"/>). Members are tracked by the
    /// service so you can lock a whole group, a group-except-a-selection, or just a selection. Create
    /// one from a string (implicitly or via <see cref="Get"/>); <c>default</c> is <see cref="None"/>.
    /// </summary>
    [Serializable]
    public readonly struct InputLockGroup : IEquatable<InputLockGroup>
    {
        /// <summary>The interned id. <c>0</c> means <see cref="None"/>.</summary>
        public readonly int Id;

        internal InputLockGroup(int id)
        {
            Id = id;
        }

        /// <summary>The empty group (no grouping). Equal to <c>default(InputLockGroup)</c>.</summary>
        public static InputLockGroup None => default;

        /// <summary>True for any real (non-empty) group.</summary>
        public bool IsValid => Id > 0;

        /// <summary>The group's name, or empty for <see cref="None"/>.</summary>
        public string Name => InputLockGroupRegistry.NameOf(Id);

        /// <summary>Interns <paramref name="name"/> and returns its group.</summary>
        public static InputLockGroup Get(string name) => InputLockGroupRegistry.Get(name);

        public static implicit operator InputLockGroup(string name) => InputLockGroupRegistry.Get(name);

        public bool Equals(InputLockGroup other) => Id == other.Id;

        public override bool Equals(object obj) => obj is InputLockGroup other && other.Id == Id;

        public override int GetHashCode() => Id;

        public override string ToString() => IsValid ? Name : "None";

        public static bool operator ==(InputLockGroup a, InputLockGroup b) => a.Id == b.Id;

        public static bool operator !=(InputLockGroup a, InputLockGroup b) => a.Id != b.Id;
    }
}
