using UnityEngine;
using UnityEngine.Events;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Fires UnityEvents on lock / unlock so designers can wire arbitrary reactions (animators, VFX,
    /// SFX, tween triggers) without writing code. The escape hatch for anything the typed lockables
    /// don't cover.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Unity Event Lock")]
    public sealed class UnityEventLock : InputLockableBehaviour
    {
        [SerializeField]
        [Tooltip("Invoked when the component becomes locked.")]
        private UnityEvent _onLocked = new UnityEvent();

        [SerializeField]
        [Tooltip("Invoked when the component becomes unlocked.")]
        private UnityEvent _onUnlocked = new UnityEvent();

        /// <summary>Invoked when the component becomes locked.</summary>
        public UnityEvent OnLocked => _onLocked;

        /// <summary>Invoked when the component becomes unlocked.</summary>
        public UnityEvent OnUnlocked => _onUnlocked;

        protected override void OnLock() => _onLocked?.Invoke();

        protected override void OnUnlock() => _onUnlocked?.Invoke();
    }
}
