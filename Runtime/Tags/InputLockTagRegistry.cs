using System;
using System.Collections.Generic;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Global, process-wide registry that interns input-lock tag names into stable integer ids.
    /// Interning keeps the locking hot path on cheap integer-keyed dictionaries while call sites
    /// stay string-friendly. Ids are assigned in first-seen order and remain stable for the
    /// lifetime of the domain; id <c>0</c> is always the reserved <see cref="InputLockTag.Default"/>.
    /// </summary>
    /// <remarks>
    /// Main-thread affine. Registration is expected to happen at authoring / initialization time,
    /// not on the per-frame hot path, so it is intentionally not synchronized.
    /// </remarks>
    public static class InputLockTagRegistry
    {
        internal const string DefaultTagName = "Default";

        private static readonly Dictionary<string, int> IdByName =
            new Dictionary<string, int>(64, StringComparer.Ordinal);
        private static readonly List<string> NameById = new List<string>(64);

        static InputLockTagRegistry()
        {
            // Reserve id 0 for the Default tag so default(InputLockTag) is always valid & meaningful.
            Get(DefaultTagName);
        }

        /// <summary>Number of distinct tags interned so far.</summary>
        public static int Count => NameById.Count;

        /// <summary>
        /// Returns the interned tag for <paramref name="name"/>, creating a new id the first time a
        /// name is seen. Null / empty names collapse to <see cref="InputLockTag.Default"/>.
        /// </summary>
        public static InputLockTag Get(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return InputLockTag.Default;
            }

            if (!IdByName.TryGetValue(name, out var id))
            {
                id = NameById.Count;
                NameById.Add(name);
                IdByName.Add(name, id);
            }

            return new InputLockTag(id);
        }

        /// <summary>Resolves the display name for an interned id.</summary>
        public static string NameOf(int id)
        {
            return (uint)id < (uint)NameById.Count ? NameById[id] : "<invalid-tag>";
        }

        /// <summary>True if <paramref name="name"/> has already been interned.</summary>
        public static bool IsRegistered(string name)
        {
            return !string.IsNullOrEmpty(name) && IdByName.ContainsKey(name);
        }

        /// <summary>Enumerates every interned tag id (0..Count-1). For tooling only.</summary>
        public static IReadOnlyList<string> AllNames => NameById;

        /// <summary>
        /// Clears every interned tag except the reserved <see cref="InputLockTag.Default"/>.
        /// Intended for test isolation only — never call from gameplay code.
        /// </summary>
        internal static void ResetForTests()
        {
            IdByName.Clear();
            NameById.Clear();
            Get(DefaultTagName);
        }
    }
}
