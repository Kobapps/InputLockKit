using Kobapps.InputLockKit.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kobapps.InputLockKit.Samples
{
    /// <summary>
    /// Demonstrates tutorial gating with <see cref="InputLock.AllExcept"/>: every menu button is locked
    /// except the one the current tutorial step wants you to press. Pressing the highlighted button
    /// advances the step; the lock moves with it. Drop on an empty GameObject and press Play.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Samples/Tutorial Gating Demo")]
    public sealed class TutorialGatingDemo : MonoBehaviour
    {
        private static readonly string[] ButtonTags = { "Play", "Shop", "Settings", "Quit" };

        private readonly Button[] _buttons = new Button[ButtonTags.Length];
        private InputLockHandle _gateHandle;
        private int _step = -1;
        private Text _status;

        private void Start()
        {
            var canvas = CreateCanvas();

            for (var i = 0; i < ButtonTags.Length; i++)
            {
                var button = CreateButton(canvas.transform, ButtonTags[i],
                    new Vector2(0f, 300f - i * 130f), new Vector2(500f, 110f));

                // Each button locks on its own tag.
                var selectableLock = button.gameObject.AddComponent<SelectableLock>();
                selectableLock.SetTags(new[] { ButtonTags[i] });

                var captured = i;
                button.onClick.AddListener(() => OnButtonPressed(captured));
                _buttons[i] = button;
            }

            _status = CreateButton(canvas.transform, "", new Vector2(0f, -520f),
                new Vector2(960f, 80f)).GetComponentInChildren<Text>();

            AdvanceTo(0);
        }

        private void OnButtonPressed(int index)
        {
            if (index != _step)
            {
                return; // gated out — shouldn't happen while locked, but guard anyway
            }

            AdvanceTo(_step + 1);
        }

        private void AdvanceTo(int step)
        {
            _gateHandle.Dispose(); // release the previous gate

            if (step >= ButtonTags.Length)
            {
                SetStatus("Tutorial complete — all buttons unlocked.");
                _step = -1;
                return;
            }

            _step = step;

            // Lock everything EXCEPT the button this step wants the player to press.
            _gateHandle = InputLock.AllExcept(new InputLockTag[] { ButtonTags[step] });
            SetStatus($"Step {step + 1}/{ButtonTags.Length}: press \"{ButtonTags[step]}\".");
        }

        private void SetStatus(string message)
        {
            if (_status != null)
            {
                _status.text = message;
            }
        }

        // ---- minimal self-contained UGUI helpers ----

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Tutorial Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        private static Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(string.IsNullOrEmpty(label) ? "Status" : label, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.22f, 0.45f, 0.85f);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
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
