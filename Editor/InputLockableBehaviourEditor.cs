using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kobapps.InputLockKit.Editor
{
    /// <summary>
    /// Custom inspector shared by every <see cref="InputLockableBehaviour"/>. Tag fields render as
    /// catalog-backed dropdowns (via <c>[InputLockTag]</c>); any tag used here is added to the catalog
    /// automatically. Also shows a live locked/unlocked readout while playing.
    /// </summary>
    [CustomEditor(typeof(InputLockableBehaviour), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    public sealed class InputLockableBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            var changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (changed)
            {
                SyncToCatalog();
            }

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

        private void SyncToCatalog()
        {
            var tags = new List<string>();
            var groups = new List<string>();
            foreach (var obj in targets)
            {
                if (!(obj is InputLockableBehaviour lockable))
                {
                    continue;
                }

                var lockableTags = lockable.Tags;
                for (var i = 0; i < lockableTags.Count; i++)
                {
                    tags.Add(lockableTags[i]);
                }

                var group = lockable.Group;
                if (group.IsValid)
                {
                    groups.Add(group.Name);
                }
            }

            InputLockEditorAssets.EnsureTags(tags);
            InputLockEditorAssets.EnsureGroups(groups);
        }
    }
}
