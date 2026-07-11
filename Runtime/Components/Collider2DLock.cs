using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Disables 2D <see cref="Collider2D"/>s while locked so 2D world objects stop receiving input.
    /// Leave the target list empty to auto-collect colliders on this GameObject and its children.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Collider 2D Lock")]
    public sealed class Collider2DLock : InputLockableBehaviour
    {
        [SerializeField]
        [Tooltip("2D colliders to toggle. If empty, colliders in children are collected on Awake.")]
        private Collider2D[] _colliders = new Collider2D[0];

        private void Awake()
        {
            if (_colliders == null || _colliders.Length == 0)
            {
                _colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
            }
        }

        protected override void OnLock() => SetEnabled(false);

        protected override void OnUnlock() => SetEnabled(true);

        private void SetEnabled(bool enabled)
        {
            for (var i = 0; i < _colliders.Length; i++)
            {
                var collider = _colliders[i];
                if (collider != null)
                {
                    collider.enabled = enabled;
                }
            }
        }
    }
}
