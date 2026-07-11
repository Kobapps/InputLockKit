using UnityEngine;

namespace Kobapps.InputLockKit.Samples
{
    /// <summary>
    /// Example of authoring a brand-new lockable in ~15 lines: subclass
    /// <see cref="InputLockableBehaviour"/> and override <see cref="OnLock"/> / <see cref="OnUnlock"/>.
    /// This one shrinks and greys the object while locked as a visual "disabled" cue — but you could
    /// just as easily disable an ability, mute audio, or swap an animation.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Samples/Pulse Scale Lock")]
    public sealed class PulseScaleLock : InputLockableBehaviour
    {
        [SerializeField] private Vector3 _lockedScale = new Vector3(0.85f, 0.85f, 0.85f);

        private Vector3 _unlockedScale;

        private void Awake()
        {
            _unlockedScale = transform.localScale;
        }

        protected override void OnLock() => transform.localScale = _lockedScale;

        protected override void OnUnlock() => transform.localScale = _unlockedScale;
    }
}
