using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Project-level InputLockKit configuration. Lives in a <c>Resources</c> folder so it can be loaded
    /// at runtime (including in builds). When <see cref="AutoRegisterOnLoad"/> is on, the assigned
    /// <see cref="Catalog"/> is registered before the first scene loads, so its tags are known to
    /// <see cref="IInputLockService.LockAll"/> and appear in the debugger without any manual wiring.
    /// Create and edit it from <b>Edit ▸ Project Settings ▸ Input Lock</b>.
    /// </summary>
    public sealed class InputLockSettings : ScriptableObject
    {
        /// <summary>Resource name (must live at the root of some <c>Resources</c> folder).</summary>
        public const string ResourceName = "InputLockSettings";

        [SerializeField]
        [Tooltip("Tag catalog registered automatically at game start (if Auto Register On Load is on).")]
        private InputLockTagCatalog _catalog;

        [SerializeField]
        [Tooltip("Register the catalog before the first scene loads so its tags are globally known.")]
        private bool _autoRegisterOnLoad = true;

        /// <summary>The catalog registered on load.</summary>
        public InputLockTagCatalog Catalog => _catalog;

        /// <summary>Whether the catalog is registered automatically before the first scene loads.</summary>
        public bool AutoRegisterOnLoad => _autoRegisterOnLoad;

        /// <summary>Loads the project settings from Resources, or null if none has been created.</summary>
        public static InputLockSettings LoadOrNull()
        {
            return Resources.Load<InputLockSettings>(ResourceName);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterConfiguredCatalog()
        {
            var settings = LoadOrNull();
            if (settings == null || !settings._autoRegisterOnLoad || settings._catalog == null)
            {
                return;
            }

            settings._catalog.RegisterAll();
            settings._catalog.RegisterAllGroups();
            InputLockRuntime.Catalog = settings._catalog;
        }
    }
}
