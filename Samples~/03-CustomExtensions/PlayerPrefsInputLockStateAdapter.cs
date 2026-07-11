using System.Text;
using UnityEngine;

namespace Kobapps.InputLockKit.Samples
{
    /// <summary>
    /// Reference <see cref="IInputLockStateAdapter"/> that persists the set of locked tags to
    /// <see cref="PlayerPrefs"/> and re-applies them on load. Assign it to an
    /// <see cref="InputLockServiceInstaller"/> (its "State Adapter" slot) to make locks survive a
    /// scene reload / relogin — e.g. so a tutorial that locked input stays locked. Swap PlayerPrefs
    /// for your own save system in a real game.
    /// </summary>
    [AddComponentMenu("Kobapps/Input Lock/Samples/PlayerPrefs State Adapter")]
    public sealed class PlayerPrefsInputLockStateAdapter : MonoBehaviour, IInputLockStateAdapter
    {
        [SerializeField]
        [Tooltip("PlayerPrefs key under which the locked tags are stored.")]
        private string _key = "inputlockkit.locks";

        private readonly StringBuilder _builder = new StringBuilder(64);

        public void OnLocksChanged(InputLockSnapshot snapshot)
        {
            if (snapshot.Count == 0)
            {
                PlayerPrefs.DeleteKey(_key);
                PlayerPrefs.Save();
                return;
            }

            _builder.Clear();
            for (var i = 0; i < snapshot.Count; i++)
            {
                if (i > 0)
                {
                    _builder.Append(',');
                }

                _builder.Append(snapshot[i].Name);
            }

            PlayerPrefs.SetString(_key, _builder.ToString());
            PlayerPrefs.Save();
        }

        public void Restore(IInputLockRestoreContext context)
        {
            if (!PlayerPrefs.HasKey(_key))
            {
                return;
            }

            var raw = PlayerPrefs.GetString(_key);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var names = raw.Split(',');
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i].Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    context.Lock(name, "PlayerPrefsRestore");
                }
            }
        }

        /// <summary>Clears any persisted lock state (handy for the demo's reset button).</summary>
        public void ClearSaved()
        {
            PlayerPrefs.DeleteKey(_key);
            PlayerPrefs.Save();
        }
    }
}
