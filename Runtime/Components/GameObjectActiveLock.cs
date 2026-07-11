using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Toggles the active state of one or more GameObjects when its tags lock. Handy for swapping in a
    /// "blocker" overlay or hiding interactive props. By default targets are deactivated while locked;
    /// tick <see cref="_activeWhenLocked"/> to invert (e.g. show a blocker only while locked).
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/GameObject Active Lock")]
    public sealed class GameObjectActiveLock : InputLockableBehaviour
    {
        [SerializeField]
        [Tooltip("GameObjects whose active state is driven by the lock.")]
        private GameObject[] _targets = new GameObject[0];

        [SerializeField]
        [Tooltip("If true, targets are ACTIVE while locked (e.g. a blocker overlay). " +
                 "If false, targets are INACTIVE while locked.")]
        private bool _activeWhenLocked;

        protected override void OnLock() => SetActive(_activeWhenLocked);

        protected override void OnUnlock() => SetActive(!_activeWhenLocked);

        private void SetActive(bool active)
        {
            for (var i = 0; i < _targets.Length; i++)
            {
                var target = _targets[i];
                if (target != null)
                {
                    target.SetActive(active);
                }
            }
        }
    }
}
