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

        [SerializeField]
        [Tooltip("Reusable group names (e.g. \"Grid\", \"HUD\"). Pick these on lockables to share a group.")]
        private List<Entry> _groups = new List<Entry>();

        /// <summary>The authored tag entries.</summary>
        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>The authored group entries (reusable group names).</summary>
        public IReadOnlyList<Entry> Groups => _groups;

        /// <summary>True if the catalog already contains an entry with this name.</summary>
        public bool Contains(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds a new named tag to the catalog if not already present. Returns true if it was added.
        /// Editor authoring helper — callers persist the change (SetDirty / SaveAssets).
        /// </summary>
        internal bool AddTag(string name)
        {
            if (string.IsNullOrEmpty(name) || Contains(name))
            {
                return false;
            }

            _entries.Add(new Entry
            {
                Name = name,
                Description = string.Empty,
                Color = new Color(0.55f, 0.6f, 0.7f),
            });
            return true;
        }

        /// <summary>True if the catalog already contains a group with this name.</summary>
        public bool ContainsGroup(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            for (var i = 0; i < _groups.Count; i++)
            {
                if (_groups[i].Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Adds a new named group if not already present. Returns true if it was added.</summary>
        internal bool AddGroup(string name)
        {
            if (string.IsNullOrEmpty(name) || ContainsGroup(name))
            {
                return false;
            }

            _groups.Add(new Entry
            {
                Name = name,
                Description = string.Empty,
                Color = new Color(0.7f, 0.55f, 0.4f),
            });
            return true;
        }

        /// <summary>Interns every catalog tag into the registry. Safe to call multiple times.</summary>
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

        /// <summary>Interns every catalog group name into the group registry.</summary>
        public void RegisterAllGroups()
        {
            for (var i = 0; i < _groups.Count; i++)
            {
                var name = _groups[i].Name;
                if (!string.IsNullOrEmpty(name))
                {
                    InputLockGroupRegistry.Get(name);
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
