using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kobapps.InputLockKit.Samples
{
    /// <summary>
    /// Ties together the two extension points: a custom lockable (<see cref="PulseScaleLock"/>) and a
    /// custom state adapter (<see cref="PlayerPrefsInputLockStateAdapter"/>). Lock the "Gameplay" tag,
    /// reload the scene, and the lock is restored from PlayerPrefs — the target is still shrunk.
    ///
    /// This wires the service in code so the sample is self-contained; in a real project you would drop
    /// an <see cref="InputLockServiceInstaller"/> in the scene and assign the adapter in the inspector.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Samples/Custom Extensions Demo")]
    public sealed class CustomExtensionsDemo : MonoBehaviour
    {
        private const string GameplayTag = "Gameplay";

        private InputLockService _service;
        private PlayerPrefsInputLockStateAdapter _adapter;
        private InputLockHandle _handle;
        private Text _status;

        private void Start()
        {
            // 1. Build the service with a persistence adapter and publish it to the facade.
            _adapter = gameObject.AddComponent<PlayerPrefsInputLockStateAdapter>();
            _service = new InputLockService(_adapter);
            _service.RegisterTag(GameplayTag);
            InputLock.SetProvider(new Provider(_service));

            // 2. Build UI: a target that reacts to the Gameplay tag via our custom lockable.
            var canvas = CreateCanvas();
            var target = CreateImage(canvas.transform, "Gameplay Target",
                new Vector2(0f, 250f), new Vector2(360f, 360f), new Color(0.9f, 0.6f, 0.2f));
            var lockable = target.gameObject.AddComponent<PulseScaleLock>();
            lockable.SetTags(new[] { GameplayTag });

            CreateButton(canvas.transform, "Lock Gameplay", new Vector2(-330f, -250f)).onClick
                .AddListener(LockGameplay);
            CreateButton(canvas.transform, "Unlock Gameplay", new Vector2(0f, -250f)).onClick
                .AddListener(UnlockGameplay);
            CreateButton(canvas.transform, "Reload Scene", new Vector2(330f, -250f)).onClick
                .AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            CreateButton(canvas.transform, "Clear Saved", new Vector2(0f, -400f)).onClick
                .AddListener(() => { _adapter.ClearSaved(); SetStatus("Cleared saved lock state."); });

            _status = CreateButton(canvas.transform, "", new Vector2(0f, -520f)).GetComponentInChildren<Text>();

            // 3. Restore any previously-saved locks (after the lockable is subscribed so it reacts).
            _service.RestoreFromAdapter();
            SetStatus(_service.IsLocked(GameplayTag)
                ? "Restored: Gameplay was locked from a previous run."
                : "Ready. Lock Gameplay, then Reload Scene to see it persist.");
        }

        private void OnDestroy()
        {
            if (InputLock.Service == _service)
            {
                InputLock.ClearProvider();
            }
        }

        private void LockGameplay()
        {
            if (!_handle.IsActive)
            {
                _handle = _service.Lock(GameplayTag, "GameplayDemo");
                SetStatus("Gameplay locked — target shrinks. Reload to persist.");
            }
        }

        private void UnlockGameplay()
        {
            _handle.Dispose();
            SetStatus("Gameplay unlocked.");
        }

        private void SetStatus(string message)
        {
            if (_status != null)
            {
                _status.text = message;
            }
        }

        private sealed class Provider : IInputLockServiceProvider
        {
            public Provider(IInputLockService service) => Service = service;
            public IInputLockService Service { get; }
        }

        // ---- minimal self-contained UGUI helpers ----

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Custom Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080f, 1920f);
            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            return canvas;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            return go.GetComponent<Image>();
        }

        private static Button CreateButton(Transform parent, string label, Vector2 pos)
        {
            var go = new GameObject(string.IsNullOrEmpty(label) ? "Status" : label, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.22f, 0.45f, 0.85f);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(300f, 90f);
            rect.anchoredPosition = pos;

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return go.GetComponent<Button>();
        }
    }
}
