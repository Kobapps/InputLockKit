using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Owns the process-wide default <see cref="IInputLockService"/> used by the drop-in
    /// <see cref="InputLock"/> facade when no custom provider is registered. The instance is created
    /// lazily and rebuilt on every play session so state never leaks across Enter-Play-Mode (even with
    /// domain reload disabled).
    /// </summary>
    public static class InputLockRuntime
    {
        private static IInputLockService _default;
        private static InputLockTagCatalog _catalog;

        /// <summary>The lazily-created default service.</summary>
        public static IInputLockService Default => _default ??= new InputLockService();

        /// <summary>True if a default service has been created this session.</summary>
        public static bool HasDefault => _default != null;

        /// <summary>
        /// A process-wide default tag catalog that every newly-created <see cref="InputLockService"/>
        /// registers, so catalog tags are globally known (LockAll, debugger). Set by the settings
        /// bootstrap; cleared on entering play mode.
        /// </summary>
        public static InputLockTagCatalog Catalog
        {
            get => _catalog;
            set
            {
                _catalog = value;
                if (_default != null && value != null)
                {
                    _default.RegisterCatalog(value);
                }
            }
        }

        /// <summary>Replaces the default service (e.g. to inject a state adapter at boot).</summary>
        public static void SetDefault(IInputLockService service)
        {
            _default = service;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            _default = null;
            _catalog = null;
        }
    }
}
