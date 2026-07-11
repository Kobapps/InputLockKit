using UnityEditor;
using UnityEngine;

namespace Kobapps.InputLockKit.Editor
{
    /// <summary>Inspector for <see cref="InputLockTagCatalog"/> with a quick "register into runtime" action.</summary>
    [CustomEditor(typeof(InputLockTagCatalog))]
    public sealed class InputLockTagCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "These tags feed the 'Add Tag From Catalog' dropdown on lockable components and are " +
                "pre-registered by the Input Lock Service Installer so LockAll covers them.",
                MessageType.Info);

            if (Application.isPlaying && GUILayout.Button("Register Into Runtime Now"))
            {
                ((InputLockTagCatalog)target).RegisterAll();
            }
        }
    }
}
