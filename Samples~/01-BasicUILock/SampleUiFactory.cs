using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kobapps.InputLockKit.Samples
{
    /// <summary>Tiny helper that builds throwaway UGUI widgets so the samples are self-contained.</summary>
    internal static class SampleUiFactory
    {
        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080f, 1920f);

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            return canvas;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(label, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.22f, 0.45f, 0.85f);

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

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

        public static RectTransform CreatePanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.85f);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            return rect;
        }
    }
}
