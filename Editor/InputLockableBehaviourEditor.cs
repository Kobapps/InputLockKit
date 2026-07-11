using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kobapps.InputLockKit.Editor
{
    /// <summary>
    /// Custom inspector shared by every <see cref="InputLockableBehaviour"/>. Adds a one-click "add
    /// tag from catalog" dropdown on top of the raw tag list and a live locked/unlocked status readout
    /// while playing.
    /// </summary>
    [CustomEditor(typeof(InputLockableBehaviour), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    public sealed class InputLockableBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty _tags;

        private void OnEnable()
        {
            _tags = serializedObject.FindProperty("_tags");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            EditorGUILayout.Space(4f);
            if (_tags != null && GUILayout.Button("Add Tag From Catalog ▾"))
            {
                ShowCatalogMenu();
            }

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying && target is InputLockableBehaviour lockable)
            {
                EditorGUILayout.Space(4f);
                var prev = GUI.color;
                GUI.color = lockable.IsLocked ? new Color(1f, 0.6f, 0.5f) : new Color(0.6f, 0.9f, 0.6f);
                EditorGUILayout.LabelField("Status", lockable.IsLocked ? "LOCKED" : "unlocked",
                    EditorStyles.boldLabel);
                GUI.color = prev;
                Repaint();
            }
        }

        private void ShowCatalogMenu()
        {
            var menu = new GenericMenu();
            var added = new HashSet<string>();

            foreach (var name in CollectCatalogTagNames())
            {
                if (!added.Add(name))
                {
                    continue;
                }

                var captured = name;
                menu.AddItem(new GUIContent(captured), false, () => AppendTag(captured));
            }

            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("No InputLockTagCatalog assets found"));
            }

            menu.ShowAsContext();
        }

        private static IEnumerable<string> CollectCatalogTagNames()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(InputLockTagCatalog));
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<InputLockTagCatalog>(path);
                if (catalog == null)
                {
                    continue;
                }

                var entries = catalog.Entries;
                for (var i = 0; i < entries.Count; i++)
                {
                    if (!string.IsNullOrEmpty(entries[i].Name))
                    {
                        yield return entries[i].Name;
                    }
                }
            }
        }

        private void AppendTag(string name)
        {
            serializedObject.Update();
            var index = _tags.arraySize;
            _tags.InsertArrayElementAtIndex(index);
            _tags.GetArrayElementAtIndex(index).stringValue = name;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
