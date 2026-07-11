using System;
using System.Collections.Generic;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Interns input-lock <b>group</b> names into stable integer ids, mirroring
    /// <see cref="InputLockTagRegistry"/> but in an independent id space. Id <c>0</c> is the reserved
    /// empty group (<see cref="InputLockGroup.None"/>), so real groups start at 1 and
    /// <c>default(InputLockGroup)</c> is "no group".
    /// </summary>
    /// <remarks>Main-thread affine; registration happens at authoring / spawn time, not per-frame.</remarks>
    public static class InputLockGroupRegistry
    {
        private static readonly Dictionary<string, int> IdByName =
            new Dictionary<string, int>(32, StringComparer.Ordinal);
        private static readonly List<string> NameById = new List<string>(32);

        static InputLockGroupRegistry()
        {
            NameById.Add(string.Empty); // id 0 = None
            IdByName.Add(string.Empty, 0);
        }

        /// <summary>Number of interned groups including the reserved empty group.</summary>
        public static int Count => NameById.Count;

        /// <summary>Interns <paramref name="name"/>; null / empty maps to <see cref="InputLockGroup.None"/>.</summary>
        public static InputLockGroup Get(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return default;
            }

            if (!IdByName.TryGetValue(name, out var id))
            {
                id = NameById.Count;
                NameById.Add(name);
                IdByName.Add(name, id);
            }

            return new InputLockGroup(id);
        }

        /// <summary>Resolves the display name for an interned group id (empty for id 0 / invalid).</summary>
        public static string NameOf(int id)
        {
            return (uint)id < (uint)NameById.Count ? NameById[id] : string.Empty;
        }

        /// <summary>All interned group names (index 0 is the empty group). For tooling.</summary>
        public static IReadOnlyList<string> AllNames => NameById;

        /// <summary>Test-only reset back to just the reserved empty group.</summary>
        internal static void ResetForTests()
        {
            IdByName.Clear();
            NameById.Clear();
            NameById.Add(string.Empty);
            IdByName.Add(string.Empty, 0);
        }
    }
}
