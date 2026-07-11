using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kobapps.InputLockKit.Editor
{
    /// <summary>
    /// <b>Edit ▸ Project Settings ▸ Input Lock</b> — create/edit the project's InputLockKit settings and
    /// tag catalog (auto-registered at game load, and the authoritative list of tags), and install the
    /// InputLockKit AI skill into the project's <c>.claude/skills</c> folder.
    /// </summary>
    internal static class InputLockSettingsProvider
    {
        public const string Path = "Project/Input Lock";

        private static SerializedObject _serialized;
        private static UnityEditor.Editor _catalogEditor;
        private static Vector2 _scroll;

        [MenuItem("Tools/Input Lock/Settings", priority = 1)]
        private static void OpenFromMenu() => SettingsService.OpenProjectSettings(Path);

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider(Path, SettingsScope.Project)
            {
                label = "Input Lock",
                guiHandler = _ => OnGUI(),
                deactivateHandler = Cleanup,
                keywords = new[] { "input", "lock", "tag", "catalog", "kobapps", "skill" },
            };
        }

        private static void Cleanup()
        {
            _serialized = null;
            if (_catalogEditor != null)
            {
                Object.DestroyImmediate(_catalogEditor);
                _catalogEditor = null;
            }
        }

        private static void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUIUtility.labelWidth = 170f;
            GUILayout.Space(6f);

            var settings = InputLockEditorAssets.FindSettings();
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "No Input Lock settings asset yet. Create one to configure a tag catalog that is " +
                    "registered automatically when the game starts.", MessageType.Info);
                if (GUILayout.Button("Create Input Lock Settings", GUILayout.Height(26f)))
                {
                    Selection.activeObject = InputLockEditorAssets.GetOrCreateSettings();
                }

                EditorGUILayout.EndScrollView();
                return;
            }

            if (_serialized == null || _serialized.targetObject != settings)
            {
                _serialized = new SerializedObject(settings);
            }

            _serialized.Update();
            var catalogProp = _serialized.FindProperty("_catalog");
            var autoProp = _serialized.FindProperty("_autoRegisterOnLoad");

            Header("Tag Catalog");
            EditorGUILayout.PropertyField(catalogProp, new GUIContent("Catalog"));
            EditorGUILayout.PropertyField(autoProp, new GUIContent("Auto Register On Load"));

            if (catalogProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "No catalog assigned. Create one — it becomes the authoritative list of tags and " +
                    "feeds the tag dropdowns, LockAll and the debugger.", MessageType.None);
                if (GUILayout.Button("Create & Assign Tag Catalog"))
                {
                    catalogProp.objectReferenceValue = InputLockEditorAssets.GetOrCreateCatalog();
                    _serialized.ApplyModifiedProperties();
                    Save(settings);
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Select Asset"))
                    {
                        Selection.activeObject = catalogProp.objectReferenceValue;
                        EditorGUIUtility.PingObject(catalogProp.objectReferenceValue);
                    }

                    if (GUILayout.Button("Collect Tags & Groups Used In Project"))
                    {
                        var added = InputLockEditorAssets.CollectFromProject();
                        Debug.Log($"[InputLockKit] Collected project tags & groups into the catalog (+{added} new).");
                    }
                }

                EditorGUILayout.HelpBox(
                    autoProp.boolValue
                        ? "This catalog is registered automatically before the first scene loads, and it " +
                          "accumulates any tag you use on a lockable."
                        : "Auto-register is OFF — register the catalog yourself (installer or code).",
                    autoProp.boolValue ? MessageType.Info : MessageType.Warning);
            }

            if (_serialized.ApplyModifiedProperties())
            {
                Save(settings);
            }

            if (catalogProp.objectReferenceValue is InputLockTagCatalog editable)
            {
                GUILayout.Space(6f);
                Header("Edit Catalog Tags");
                if (_catalogEditor == null || _catalogEditor.target != editable)
                {
                    if (_catalogEditor != null)
                    {
                        Object.DestroyImmediate(_catalogEditor);
                    }

                    _catalogEditor = UnityEditor.Editor.CreateEditor(editable);
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    _catalogEditor.OnInspectorGUI();
                }
            }

            GUILayout.Space(10f);
            Header("Claude AI Skill");
            var installed = SkillIsInstalled();
            EditorGUILayout.HelpBox(
                "Install the InputLockKit AI skill into this project's .claude/skills folder so Claude " +
                "can install and use the package." + (installed ? "\n\nAlready installed." : ""),
                installed ? MessageType.Info : MessageType.None);
            if (GUILayout.Button(installed ? "Reinstall AI Skill to .claude/skills"
                    : "Add AI Skill to .claude/skills", GUILayout.Height(24f)))
            {
                InstallAiSkill();
            }

            EditorGUILayout.EndScrollView();
        }

        private static void Header(string title)
        {
            GUILayout.Space(4f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void Save(Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static string SkillDestination()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return System.IO.Path.Combine(projectRoot, ".claude", "skills", "inputlockkit");
        }

        private static bool SkillIsInstalled()
        {
            return File.Exists(System.IO.Path.Combine(SkillDestination(), "SKILL.md"));
        }

        private static void InstallAiSkill()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(InputLockSettings).Assembly);
            var source = package != null
                ? System.IO.Path.Combine(package.resolvedPath, "AISkill~")
                : null;

            if (source == null || !Directory.Exists(source))
            {
                Debug.LogError("[InputLockKit] Bundled AI skill not found in the package (AISkill~).");
                return;
            }

            var dest = SkillDestination();
            try
            {
                Directory.CreateDirectory(System.IO.Path.Combine(dest, "scripts"));
                File.Copy(System.IO.Path.Combine(source, "SKILL.md"),
                    System.IO.Path.Combine(dest, "SKILL.md"), overwrite: true);
                File.Copy(System.IO.Path.Combine(source, "scripts", "install.py"),
                    System.IO.Path.Combine(dest, "scripts", "install.py"), overwrite: true);
                Debug.Log($"[InputLockKit] AI skill installed to {dest}");
                EditorUtility.RevealInFinder(dest);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InputLockKit] Failed to install AI skill: {e.Message}");
            }
        }
    }
}
