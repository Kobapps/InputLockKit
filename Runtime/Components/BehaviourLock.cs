using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Enables/disables one or more <see cref="Behaviour"/>s (scripts, cameras, audio listeners, …)
    /// when its tags lock. The most general gameplay lockable — point it at whatever should stop
    /// responding while input is locked.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Behaviour Lock")]
    public sealed class BehaviourLock : InputLockableBehaviour
    {
        [SerializeField]
        [Tooltip("Behaviours disabled while locked, re-enabled while unlocked.")]
        private Behaviour[] _targets = new Behaviour[0];

        protected override void OnLock() => SetEnabled(false);

        protected override void OnUnlock() => SetEnabled(true);

        private void SetEnabled(bool enabled)
        {
            for (var i = 0; i < _targets.Length; i++)
            {
                var target = _targets[i];
                if (target != null)
                {
                    target.enabled = enabled;
                }
            }
        }
    }
}
