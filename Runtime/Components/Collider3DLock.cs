using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Disables 3D <see cref="Collider"/>s while locked so world objects stop receiving clicks / raycasts.
    /// Leave the target list empty to auto-collect colliders on this GameObject and its children.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Collider 3D Lock")]
    public sealed class Collider3DLock : InputLockableBehaviour
    {
        [SerializeField]
        [Tooltip("Colliders to toggle. If empty, colliders in children are collected on Awake.")]
        private Collider[] _colliders = new Collider[0];

        private void Awake()
        {
            if (_colliders == null || _colliders.Length == 0)
            {
                _colliders = GetComponentsInChildren<Collider>(includeInactive: true);
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
