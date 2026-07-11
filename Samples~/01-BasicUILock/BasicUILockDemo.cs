using System.Collections;
using Kobapps.InputLockKit.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kobapps.InputLockKit.Samples
{
    /// <summary>
    /// Drop this on an empty GameObject and press Play. It builds a small UI at runtime and shows the
    /// three most common patterns:
    ///   • Lock / unlock a specific tag ("Panel").
    ///   • A <see cref="CanvasGroupLock"/> reacting to that tag with no glue code.
    ///   • A scoped "lock everything" using a disposable handle in a coroutine.
    /// Open <b>Kobapps ▸ Input Lock ▸ Debugger</b> while playing to watch it live.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Samples/Basic UI Lock Demo")]
    public sealed class BasicUILockDemo : MonoBehaviour
    {
        private const string PanelTag = "Panel";

        private Text _status;

        private void Start()
        {
            var canvas = SampleUiFactory.CreateCanvas("InputLockKit Sample Canvas");

            // A panel that locks itself whenever the "Panel" tag is locked.
            var panel = SampleUiFactory.CreatePanel(canvas.transform, "Locked Panel",
                new Vector2(0f, 250f), new Vector2(600f, 300f));
            var panelLock = panel.gameObject.AddComponent<CanvasGroupLock>();
            panelLock.SetTags(new[] { PanelTag });
            var innerButton = SampleUiFactory.CreateButton(panel, "I'm inside the panel",
                Vector2.zero, new Vector2(460f, 90f));
            innerButton.onClick.AddListener(() => SetStatus("Inner button clicked!"));

            // Controls.
            SampleUiFactory.CreateButton(canvas.transform, "Lock Panel",
                new Vector2(-330f, -250f), new Vector2(300f, 90f))
                .onClick.AddListener(LockPanel);
            SampleUiFactory.CreateButton(canvas.transform, "Unlock Panel",
                new Vector2(0f, -250f), new Vector2(300f, 90f))
                .onClick.AddListener(UnlockPanel);
            SampleUiFactory.CreateButton(canvas.transform, "Lock ALL for 2s",
                new Vector2(330f, -250f), new Vector2(300f, 90f))
                .onClick.AddListener(() => StartCoroutine(LockAllRoutine()));

            _status = SampleUiFactory.CreateButton(canvas.transform, "Status: ready",
                new Vector2(0f, -420f), new Vector2(960f, 70f)).GetComponentInChildren<Text>();

            SetStatus("Ready. Try the buttons.");
        }

        private InputLockHandle _panelHandle;

        private void LockPanel()
        {
            if (!_panelHandle.IsActive)
            {
                _panelHandle = InputLock.Lock(PanelTag);
                SetStatus("Panel locked — the inner button is now inert.");
            }
        }

        private void UnlockPanel()
        {
            _panelHandle.Dispose();
            SetStatus("Panel unlocked.");
        }

        private IEnumerator LockAllRoutine()
        {
            SetStatus("Everything locked for 2 seconds…");
            using (InputLock.All()) // auto-releases at end of scope
            {
                yield return new WaitForSeconds(2f);
            }

            SetStatus("All input restored.");
        }

        private void SetStatus(string message)
        {
            if (_status != null)
            {
                _status.text = "Status: " + message;
            }
        }
    }
}
