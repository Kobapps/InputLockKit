using UnityEditor;
using UnityEngine;

namespace Kobapps.InputLockKit.Editor
{
    /// <summary>
    /// Draws a <c>[InputLockGroup]</c> string field as a dropdown of the catalog's reusable groups, with
    /// entries to clear the group, add a new group (added to the catalog) or open the catalog.
    /// </summary>
    [CustomPropertyDrawer(typeof(InputLockGroupAttribute))]
    public sealed class InputLockGroupDrawer : PropertyDrawer
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
            menu.AddItem(new GUIContent("(none)"), string.IsNullOrEmpty(current), () => SetValue(so, path, string.Empty));

            var names = InputLockEditorAssets.AllGroupNames();
            if (!string.IsNullOrEmpty(current) && !names.Contains(current))
            {
                menu.AddItem(new GUIContent(current + "  (not in catalog)"), true, () => SetValue(so, path, current));
            }

            if (names.Count > 0)
            {
                menu.AddSeparator(string.Empty);
                for (var i = 0; i < names.Count; i++)
                {
                    var captured = names[i];
                    menu.AddItem(new GUIContent(captured), captured == current, () =>
                    {
                        InputLockEditorAssets.EnsureGroups(new[] { captured });
                        SetValue(so, path, captured);
                    });
                }
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Add New Group…"), false, () =>
                AddTagPopup.Show(rect, name =>
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return;
                    }

                    name = name.Trim();
                    InputLockEditorAssets.AddGroup(name);
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
}
