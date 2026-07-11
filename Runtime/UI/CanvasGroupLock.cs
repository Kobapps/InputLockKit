using UnityEngine;

namespace Kobapps.InputLockKit.UI
{
    /// <summary>
    /// Blocks interaction with a whole UI subtree by driving its <see cref="CanvasGroup"/> while locked.
    /// Captures the group's authored interactable / raycast state on Awake and restores it on unlock, so
    /// a panel that was intentionally non-interactive stays that way.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Canvas Group Lock")]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class CanvasGroupLock : InputLockableBehaviour
    {
        private CanvasGroup _canvasGroup;
        private bool _baseInteractable;
        private bool _baseBlocksRaycasts;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _baseInteractable = _canvasGroup.interactable;
            _baseBlocksRaycasts = _canvasGroup.blocksRaycasts;
        }

        protected override void OnLock()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        protected override void OnUnlock()
        {
            _canvasGroup.interactable = _baseInteractable;
            _canvasGroup.blocksRaycasts = _baseBlocksRaycasts;
        }
    }
}
