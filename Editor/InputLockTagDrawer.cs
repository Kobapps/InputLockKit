using System;
using UnityEditor;
using UnityEngine;

namespace Kobapps.InputLockKit.Editor
{
    /// <summary>
    /// Draws a <c>[InputLockTag]</c> string field as a dropdown of the catalog's tags, with entries to
    /// add a new tag (added to the catalog) or open the catalog. Applies to each element of a
    /// <c>List&lt;string&gt;</c> when the attribute decorates the list.
    /// </summary>
    [CustomPropertyDrawer(typeof(InputLockTagAttribute))]
    public sealed class InputLockTagDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var content = EditorGUI.BeginProperty(position, label, property);
            var rect = EditorGUI.PrefixLabel(position, content);

            var current = property.stringValue;
            var display = string.IsNullOrEmpty(current) ? "(none)" : current;
            if (EditorGUI.DropdownButton(rect, new GUIContent(display), FocusType.Keyboard))
            {
                ShowMenu(rect, property, current);
            }

            EditorGUI.EndProperty();
        }

        private static void ShowMenu(Rect rect, SerializedProperty property, string current)
        {
            var so = property.serializedObject;
            var path = property.propertyPath;

            var menu = new GenericMenu();
            var names = InputLockEditorAssets.AllTagNames();

            if (!string.IsNullOrEmpty(current) && !names.Contains(current))
            {
                // Show the current (unlisted) value so it stays selectable / visible.
                menu.AddItem(new GUIContent(current + "  (not in catalog)"), true, () => SetValue(so, path, current));
                menu.AddSeparator(string.Empty);
            }

            if (names.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No tags in catalog"));
            }
            else
            {
                for (var i = 0; i < names.Count; i++)
                {
                    var captured = names[i];
                    menu.AddItem(new GUIContent(captured), captured == current, () =>
                    {
                        InputLockEditorAssets.EnsureTags(new[] { captured });
                        SetValue(so, path, captured);
                    });
                }
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Add New Tag…"), false, () =>
                AddTagPopup.Show(rect, name =>
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return;
                    }

                    name = name.Trim();
                    InputLockEditorAssets.AddTag(name);
                    SetValue(so, path, name);
                }));
            menu.AddItem(new GUIContent("Open Tag Catalog…"), false, () =>
                SettingsService.OpenProjectSettings(InputLockSettingsProvider.Path));

            menu.DropDown(rect);
        }

        private static void SetValue(SerializedObject so, string path, string value)
        {
            if (so == null || so.targetObject == null)
            {
                return;
            }

            so.Update();
            var prop = so.FindProperty(path);
            if (prop != null)
            {
                prop.stringValue = value;
                so.ApplyModifiedProperties();
            }
        }
    }

    /// <summary>Tiny popup to type a new tag name.</summary>
    internal sealed class AddTagPopup : PopupWindowContent
    {
        private Action<string> _onAdd;
        private string _text = string.Empty;
        private bool _focused;

        public static void Show(Rect activator, Action<string> onAdd)
        {
            PopupWindow.Show(activator, new AddTagPopup { _onAdd = onAdd });
        }

        public override Vector2 GetWindowSize() => new Vector2(240f, 62f);

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("New tag name", EditorStyles.boldLabel);

            GUI.SetNextControlName("ILK_NewTag");
            _text = EditorGUILayout.TextField(_text);
            if (!_focused)
            {
                EditorGUI.FocusTextInControl("ILK_NewTag");
                _focused = true;
            }

            var e = Event.current;
            var enter = e.type == EventType.KeyDown &&
                        (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add", GUILayout.Width(70f)) || enter)
                {
                    _onAdd?.Invoke(_text);
                    editorWindow.Close();
                }
            }
        }
    }
}
