using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Designer-authored catalog of named input-lock tags. Drives the inspector dropdowns on the
    /// lockable components and the editor debugger, and lets <see cref="IInputLockService.LockAll"/>
    /// cover a known, curated set of tags. Purely authoring data — the runtime interns these names
    /// through <see cref="InputLockTagRegistry"/> like any other tag.
    /// </summary>
    [CreateAssetMenu(
        fileName = "InputLockTagCatalog",
        menuName = "Kobapps/Input Lock/Tag Catalog",
        order = 0)]
    public sealed class InputLockTagCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("Unique tag name interned at runtime.")]
            public string Name;

            [Tooltip("Optional designer note shown in tooling.")]
            public string Description;

            [Tooltip("Colour used to draw this tag in the debugger.")]
            public Color Color;
        }

        [SerializeField]
        private List<Entry> _entries = new List<Entry>
        {
            new Entry { Name = "Default", Description = "Catch-all tag.", Color = new Color(0.6f, 0.6f, 0.6f) },
        };

        /// <summary>The authored entries.</summary>
        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>Interns every catalog entry into the registry. Safe to call multiple times.</summary>
        public void RegisterAll()
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                var name = _entries[i].Name;
                if (!string.IsNullOrEmpty(name))
                {
                    InputLockTagRegistry.Get(name);
                }
            }
        }

        /// <summary>Interns and returns the tags for every non-empty entry, in order.</summary>
        public void CollectTags(List<InputLockTag> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.Clear();
            for (var i = 0; i < _entries.Count; i++)
            {
                var name = _entries[i].Name;
                if (!string.IsNullOrEmpty(name))
                {
                    buffer.Add(InputLockTagRegistry.Get(name));
                }
            }
        }
    }
}
