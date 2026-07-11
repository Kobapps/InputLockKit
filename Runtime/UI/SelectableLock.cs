using UnityEngine;
using UnityEngine.UI;

namespace Kobapps.InputLockKit.UI
{
    /// <summary>
    /// Makes a single UGUI <see cref="Selectable"/> (Button, Toggle, Slider, InputField, …)
    /// non-interactable while locked, restoring its authored interactable state on unlock.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Selectable Lock")]
    [RequireComponent(typeof(Selectable))]
    public sealed class SelectableLock : InputLockableBehaviour
    {
        private Selectable _selectable;
        private bool _baseInteractable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _baseInteractable = _selectable.interactable;
        }

        protected override void OnLock() => _selectable.interactable = false;

        protected override void OnUnlock() => _selectable.interactable = _baseInteractable;
    }
}
