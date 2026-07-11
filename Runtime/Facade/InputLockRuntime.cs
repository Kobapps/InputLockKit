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

        /// <summary>The lazily-created default service.</summary>
        public static IInputLockService Default => _default ??= new InputLockService();

        /// <summary>True if a default service has been created this session.</summary>
        public static bool HasDefault => _default != null;

        /// <summary>Replaces the default service (e.g. to inject a state adapter at boot).</summary>
        public static void SetDefault(IInputLockService service)
        {
            _default = service;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            _default = null;
        }
    }
}
