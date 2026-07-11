using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Drop-in scene wiring for InputLockKit when you are not using a DI container. Creates a service
    /// instance, registers a tag catalog, plugs in a per-game state adapter, and (optionally) publishes
    /// itself as the global provider so the <see cref="InputLock"/> facade and all lockable components
    /// resolve to this instance. Put one on a bootstrap object in your first scene.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Input Lock Service Installer")]
    [DefaultExecutionOrder(-1000)]
    public sealed class InputLockServiceInstaller : MonoBehaviour, IInputLockServiceProvider
    {
        [SerializeField]
        [Tooltip("Optional catalog whose tags are pre-registered so LockAll covers them.")]
        private InputLockTagCatalog _catalog;

        [SerializeField]
        [Tooltip("Optional component implementing IInputLockStateAdapter for save/restore.")]
        private MonoBehaviour _stateAdapter;

        [SerializeField]
        [Tooltip("Publish this service to the global InputLock facade and all components.")]
        private bool _setAsGlobalProvider = true;

        [SerializeField]
        [Tooltip("Ask the state adapter to restore persisted locks on Start.")]
        private bool _restoreOnStart = true;

        private InputLockService _service;

        /// <inheritdoc />
        public IInputLockService Service => _service;

        private void Awake()
        {
            var adapter = _stateAdapter as IInputLockStateAdapter;
            _service = new InputLockService(adapter);

            if (_catalog != null)
            {
                _catalog.RegisterAll();
                _service.RegisterCatalog(_catalog);
            }

            if (_setAsGlobalProvider)
            {
                InputLock.SetProvider(this);
            }
        }

        private void Start()
        {
            if (_restoreOnStart)
            {
                _service.RestoreFromAdapter();
            }
        }

        private void OnDestroy()
        {
            if (_setAsGlobalProvider && ReferenceEquals(InputLock.Service, _service))
            {
                InputLock.ClearProvider();
            }

            _service?.Reset();
        }
    }
}
