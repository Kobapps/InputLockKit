using UnityEngine;
using UnityEngine.EventSystems;

namespace Kobapps.InputLockKit.UI
{
    /// <summary>
    /// Disables a canvas <see cref="BaseRaycaster"/> (typically a GraphicRaycaster) while locked, so an
    /// entire canvas stops receiving pointer events with a single cheap toggle — the broadest UI lock.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Graphic Raycaster Lock")]
    [RequireComponent(typeof(BaseRaycaster))]
    public sealed class GraphicRaycasterLock : InputLockableBehaviour
    {
        private BaseRaycaster _raycaster;
        private bool _baseEnabled;

        private void Awake()
        {
            _raycaster = GetComponent<BaseRaycaster>();
            _baseEnabled = _raycaster.enabled;
        }

        protected override void OnLock() => _raycaster.enabled = false;

        protected override void OnUnlock() => _raycaster.enabled = _baseEnabled;
    }
}
