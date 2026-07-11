using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kobapps.InputLockKit.Editor
{
    /// <summary>
    /// Shared editor helpers for the InputLockKit project assets: locating / creating the settings and
    /// tag catalog, and keeping the catalog as the authoritative list of tags (auto-adding any tag that
    /// is used). Used by the settings page, the tag drawer and the lockable inspector.
    /// </summary>
    public static class InputLockEditorAssets
    {
        private const string RootDir = "Assets/InputLockKit";
        private const string ResourcesDir = RootDir + "/Resources";
        private const string SettingsAssetPath = ResourcesDir + "/InputLockSettings.asset";
        private const string CatalogAssetPath = RootDir + "/InputLockTagCatalog.asset";

        public static InputLockSettings FindSettings()
        {
            var settings = InputLockSettings.LoadOrNull();
            if (settings != null)
            {
                return settings;
            }

            var guids = AssetDatabase.FindAssets("t:" + nameof(InputLockSettings));
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<InputLockSettings>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        public static InputLockSettings GetOrCreateSettings()
        {
            var settings = FindSettings();
            if (settings != null)
            {
                return settings;
            }

            EnsureFolder(ResourcesDir);
            settings = ScriptableObject.CreateInstance<InputLockSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        public static InputLockTagCatalog GetActiveCatalog()
        {
            var settings = FindSettings();
            if (settings != null && settings.Catalog != null)
            {
                return settings.Catalog;
            }

            var guids = AssetDatabase.FindAssets("t:" + nameof(InputLockTagCatalog));
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<InputLockTagCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        /// <summary>Returns the active catalog, creating settings + catalog (and wiring them) if needed.</summary>
        public static InputLockTagCatalog GetOrCreateCatalog()
        {
            var catalog = GetActiveCatalog();
            if (catalog != null)
            {
                EnsureAssignedToSettings(catalog);
                return catalog;
            }

            EnsureFolder(RootDir);
            catalog = ScriptableObject.CreateInstance<InputLockTagCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetDatabase.GenerateUniqueAssetPath(CatalogAssetPath));
            AssetDatabase.SaveAssets();
            catalog.RegisterAll();
            EnsureAssignedToSettings(catalog);
            return catalog;
        }

        private static void EnsureAssignedToSettings(InputLockTagCatalog catalog)
        {
            var settings = GetOrCreateSettings();
            if (settings.Catalog == catalog)
            {
                return;
            }

            var so = new SerializedObject(settings);
            var prop = so.FindProperty("_catalog");
            if (prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = catalog;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>All tag names in the active catalog (empty list if none). The authoritative list.</summary>
        public static List<string> AllTagNames()
        {
            var result = new List<string>();
            var catalog = GetActiveCatalog();
            if (catalog == null)
            {
                return result;
            }

            var entries = catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                if (!string.IsNullOrEmpty(entries[i].Name) && !result.Contains(entries[i].Name))
                {
                    result.Add(entries[i].Name);
                }
            }

            return result;
        }

        /// <summary>Adds a tag to the (auto-created) active catalog and persists it. No-op if present.</summary>
        public static void AddTag(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var catalog = GetOrCreateCatalog();
            if (catalog.AddTag(name))
            {
                InputLockTagRegistry.Get(name);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>Ensures every tag in <paramref name="names"/> exists in the catalog. Persists once.</summary>
        public static void EnsureTags(IEnumerable<string> names)
        {
            if (names == null)
            {
                return;
            }

            InputLockTagCatalog catalog = null;
            var changed = false;
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                catalog = catalog ?? GetOrCreateCatalog();
                if (catalog.AddTag(name))
                {
                    InputLockTagRegistry.Get(name);
                    changed = true;
                }
            }

            if (changed && catalog != null)
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>All group names in the active catalog (empty list if none).</summary>
        public static List<string> AllGroupNames()
        {
            var result = new List<string>();
            var catalog = GetActiveCatalog();
            if (catalog == null)
            {
                return result;
            }

            var groups = catalog.Groups;
            for (var i = 0; i < groups.Count; i++)
            {
                if (!string.IsNullOrEmpty(groups[i].Name) && !result.Contains(groups[i].Name))
                {
                    result.Add(groups[i].Name);
                }
            }

            return result;
        }

        /// <summary>Adds a group to the (auto-created) active catalog and persists it. No-op if present.</summary>
        public static void AddGroup(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var catalog = GetOrCreateCatalog();
            if (catalog.AddGroup(name))
            {
                InputLockGroupRegistry.Get(name);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>Ensures every group in <paramref name="names"/> exists in the catalog. Persists once.</summary>
        public static void EnsureGroups(IEnumerable<string> names)
        {
            if (names == null)
            {
                return;
            }

            InputLockTagCatalog catalog = null;
            var changed = false;
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                catalog = catalog ?? GetOrCreateCatalog();
                if (catalog.AddGroup(name))
                {
                    InputLockGroupRegistry.Get(name);
                    changed = true;
                }
            }

            if (changed && catalog != null)
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Scans every loaded <see cref="InputLockableBehaviour"/> (open scenes + loaded prefab assets)
        /// and adds their tags and groups to the catalog. Returns how many new entries were added.
        /// </summary>
        public static int CollectFromProject()
        {
            var beforeTags = AllTagNames().Count;
            var beforeGroups = AllGroupNames().Count;

            var lockables = Resources.FindObjectsOfTypeAll<InputLockableBehaviour>();
            var tags = new List<string>();
            var groups = new List<string>();
            foreach (var lockable in lockables)
            {
                if (lockable == null)
                {
                    continue;
                }

                var lockableTags = lockable.Tags;
                for (var i = 0; i < lockableTags.Count; i++)
                {
                    tags.Add(lockableTags[i]);
                }

                var group = lockable.Group;
                if (group.IsValid)
                {
                    groups.Add(group.Name);
                }
            }

            EnsureTags(tags);
            EnsureGroups(groups);
            return (AllTagNames().Count - beforeTags) + (AllGroupNames().Count - beforeGroups);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
