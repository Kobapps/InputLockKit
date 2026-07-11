using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// The drop-in, one-line facade over the active <see cref="IInputLockService"/>. Resolves the
    /// service through the registered <see cref="IInputLockServiceProvider"/>, falling back to
    /// <see cref="InputLockRuntime.Default"/>. Use it when you don't want to thread a service
    /// reference through your code:
    /// <code>
    /// using (InputLock.All()) { await DoCutscene(); }
    /// var h = InputLock.Lock("MainUi");
    /// ...
    /// h.Dispose();
    /// </code>
    /// </summary>
    public static class InputLock
    {
        private static IInputLockServiceProvider _provider;

        /// <summary>The service the facade currently forwards to.</summary>
        public static IInputLockService Service => _provider?.Service ?? InputLockRuntime.Default;

        /// <summary>Overrides service resolution (scene-scoped instance, DI binding, tests, …).</summary>
        public static void SetProvider(IInputLockServiceProvider provider)
        {
            _provider = provider;
        }

        /// <summary>Clears any custom provider, reverting to the runtime default.</summary>
        public static void ClearProvider()
        {
            _provider = null;
        }

        public static InputLockHandle Lock(InputLockTag tag, [CallerFilePath] string owner = null)
        {
            return Service.Lock(tag, owner);
        }

        public static InputLockHandle Lock(InputLockTag[] tags, string owner = null)
        {
            return Service.Lock(tags, owner);
        }

        public static InputLockHandle All([CallerFilePath] string owner = null)
        {
            return Service.LockAll(owner);
        }

        public static InputLockHandle AllExcept(InputLockTag[] except, string owner = null)
        {
            return Service.LockAllExcept(except, owner);
        }

        /// <summary>Locks an explicit selection of lockables (e.g. picked grid cells).</summary>
        public static InputLockHandle Only(IReadOnlyList<InputLockableBehaviour> lockables, string owner = null)
        {
            return Service.LockOnly(lockables, owner);
        }

        /// <summary>Locks every lockable currently in <paramref name="group"/>.</summary>
        public static InputLockHandle Group(InputLockGroup group, string owner = null)
        {
            return Service.LockGroup(group, owner);
        }

        /// <summary>Locks every member of <paramref name="group"/> except the given selection.</summary>
        public static InputLockHandle GroupExcept(
            InputLockGroup group, IReadOnlyList<InputLockableBehaviour> except, string owner = null)
        {
            return Service.LockGroupExcept(group, except, owner);
        }

        /// <summary>The lockables currently registered in <paramref name="group"/>.</summary>
        public static IReadOnlyList<InputLockableBehaviour> GroupMembers(InputLockGroup group)
        {
            return Service.GetGroupMembers(group);
        }

        public static void Release(InputLockHandle handle)
        {
            handle.Dispose();
        }

        public static bool IsLocked(InputLockTag tag)
        {
            return Service.IsLocked(tag);
        }

        public static bool IsAnyLocked => Service.IsAnyLocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            _provider = null;
        }
    }
}
