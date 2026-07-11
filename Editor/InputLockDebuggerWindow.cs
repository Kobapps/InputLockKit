using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kobapps.InputLockKit.Editor
{
    /// <summary>
    /// Professional live view of the input-lock system: a status header, every known tag with its
    /// locked state / reference count / owners / affected components, the registered groups with their
    /// members, plus a search filter and a rolling event log. Repaints are event-driven (it listens to
    /// the service's <see cref="IInputLockService.LocksChanged"/>), not per-frame polling. Live data is
    /// cleared when leaving Play Mode.
    /// </summary>
    public sealed class InputLockDebuggerWindow : EditorWindow
    {
        private const int MaxLogEntries = 200;

        private readonly List<string> _ownerBuffer = new List<string>(16);
        private readonly HashSet<int> _expandedTags = new HashSet<int>();
        private readonly HashSet<int> _expandedGroups = new HashSet<int>();
        private readonly List<LogEntry> _log = new List<LogEntry>(MaxLogEntries);
        private readonly Dictionary<int, InputLockHandle> _groupHandles = new Dictionary<int, InputLockHandle>();

        private IInputLockService _hookedService;
        private string _customTag = string.Empty;
        private string _search = string.Empty;
        private bool _onlyLocked;
        private bool _showLog;
        private Vector2 _scroll;

        private Skin _skin;
        private Skin S => _skin ?? (_skin = new Skin());

        private struct LogEntry
        {
            public string Time;
            public string Tag;
            public bool Locked;
        }

        // ------------------------------------------------------------------ Palette + styles

        private sealed class Skin
        {
            public readonly Color Accent = new Color(0.36f, 0.60f, 0.96f);
            public readonly Color Locked = new Color(0.91f, 0.42f, 0.34f);
            public readonly Color Open = new Color(0.40f, 0.73f, 0.46f);
            public readonly Color Muted = new Color(0.62f, 0.62f, 0.64f);

            public readonly Texture2D White;
            public readonly GUIStyle Card;
            public readonly GUIStyle PillText;
            public readonly GUIStyle SectionTitle;
            public readonly GUIStyle SectionRight;
            public readonly GUIStyle MetricValue;
            public readonly GUIStyle MetricLabel;
            public readonly GUIStyle RowTitle;
            public readonly GUIStyle Mini;
            public readonly GUIStyle CountLabel;
            public readonly GUIStyle IconOnly;
            public readonly GUIContent LockOn;
            public readonly GUIContent LockOff;

            private readonly Texture2D _cardBg;

            public Skin()
            {
                White = Solid(Color.white);
                var pro = EditorGUIUtility.isProSkin;
                _cardBg = Solid(pro ? new Color(1f, 1f, 1f, 0.035f) : new Color(0f, 0f, 0f, 0.04f));

                Card = new GUIStyle
                {
                    normal = { background = _cardBg },
                    border = new RectOffset(1, 1, 1, 1),
                    padding = new RectOffset(8, 8, 6, 6),
                    margin = new RectOffset(0, 0, 0, 4),
                };

                PillText = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                };

                SectionTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 11,
                    normal = { textColor = pro ? new Color(0.85f, 0.85f, 0.87f) : new Color(0.15f, 0.15f, 0.17f) },
                };

                SectionRight = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = Muted },
                };

                MetricValue = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleLeft,
                };

                MetricLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = Muted },
                };

                RowTitle = new GUIStyle(EditorStyles.label);
                Mini = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Muted } };
                CountLabel = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = Accent } };
                IconOnly = new GUIStyle { imagePosition = ImagePosition.ImageOnly, alignment = TextAnchor.MiddleCenter };
                LockOn = new GUIContent(EditorGUIUtility.IconContent("LockIcon-On")) { tooltip = "Locked — click to unlock" };
                LockOff = new GUIContent(EditorGUIUtility.IconContent("LockIcon")) { tooltip = "Open — click to lock" };
            }

            public void Dispose()
            {
                if (White != null) DestroyImmediate(White);
                if (_cardBg != null) DestroyImmediate(_cardBg);
            }

            private static Texture2D Solid(Color c)
            {
                var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                t.SetPixel(0, 0, c);
                t.Apply();
                return t;
            }
        }

        [MenuItem("Kobapps/Input Lock/Debugger", priority = 0)]
        private static void Open()
        {
            var window = GetWindow<InputLockDebuggerWindow>();
            window.titleContent = new GUIContent("Input Lock", EditorGUIUtility.IconContent("AssemblyLock").image);
            window.minSize = new Vector2(360f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Unhook();
            _skin?.Dispose();
            _skin = null;
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode || change == PlayModeStateChange.EnteredEditMode)
            {
                Unhook();
                ClearViewState();
            }

            Repaint();
        }

        private void ClearViewState()
        {
            _log.Clear();
            _expandedTags.Clear();
            _expandedGroups.Clear();
            _groupHandles.Clear();
            _search = string.Empty;
        }

        private void EnsureHooked()
        {
            var service = Application.isPlaying ? InputLock.Service : null;
            if (ReferenceEquals(service, _hookedService))
            {
                return;
            }

            Unhook();
            _hookedService = service;
            if (_hookedService != null)
            {
                _hookedService.LocksChanged += Repaint;
                _hookedService.TagStateChanged += OnTagStateChanged;
            }
        }

        private void Unhook()
        {
            if (_hookedService == null)
            {
                return;
            }

            _hookedService.LocksChanged -= Repaint;
            _hookedService.TagStateChanged -= OnTagStateChanged;
            _hookedService = null;
        }

        private void OnTagStateChanged(InputLockTag tag, bool locked)
        {
            if (_log.Count >= MaxLogEntries)
            {
                _log.RemoveAt(0);
            }

            _log.Add(new LogEntry { Time = DateTime.Now.ToString("HH:mm:ss.fff"), Tag = tag.Name, Locked = locked });
        }

        // ------------------------------------------------------------------ Draw

        private void OnGUI()
        {
            EnsureHooked();

            if (!Application.isPlaying)
            {
                DrawIdleState();
                return;
            }

            var service = InputLock.Service;
            if (service == null)
            {
                DrawIdleState();
                return;
            }

            DrawSummary(service);
            DrawToolbar(service);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawTagSection(service);
            GUILayout.Space(4f);
            DrawGroupSection(service);
            GUILayout.Space(4f);
            DrawLog();
            EditorGUILayout.EndScrollView();
        }

        private void DrawIdleState()
        {
            GUILayout.Space(24f);
            var icon = EditorGUIUtility.IconContent("AssemblyLock").image;
            var rect = GUILayoutUtility.GetRect(48f, 48f);
            rect.x = (position.width - 48f) * 0.5f;
            rect.width = 48f;
            if (icon != null)
            {
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
                GUI.color = prev;
            }

            var centered = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            var centeredMini = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = S.Muted },
            };
            GUILayout.Label("Enter Play Mode to inspect live input locks.", centered);
            GUILayout.Label("Tags, reference counts, owners, groups and events appear here while playing.",
                centeredMini);

            GUILayout.Space(12f);
            DrawConfiguredTags();
        }

        private void DrawConfiguredTags()
        {
            var names = CollectCatalogTagNames();
            if (names.Count == 0)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(S.Card))
            {
                Section("CONFIGURED TAGS", names.Count.ToString());
                for (var i = 0; i < names.Count; i++)
                {
                    EditorGUILayout.LabelField("   " + names[i], S.Mini);
                }
            }
        }

        private void DrawSummary(IInputLockService service)
        {
            var concrete = service as InputLockService;
            var known = service.KnownTagIds;
            var lockedTags = 0;
            for (var i = 0; i < known.Count; i++)
            {
                if (service.GetLockCount(new InputLockTag(known[i])) > 0)
                {
                    lockedTags++;
                }
            }

            var groups = 0;
            if (concrete != null)
            {
                foreach (var _ in concrete.GroupIds)
                {
                    groups++;
                }
            }

            using (new EditorGUILayout.HorizontalScope(S.Card))
            {
                LockIconStatus(service.IsAnyLocked);
                GUILayout.Label(service.IsAnyLocked ? "Locked" : "Idle", EditorStyles.boldLabel, GUILayout.Width(52f));

                GUILayout.FlexibleSpace();
                Metric(concrete != null ? concrete.ActiveLockCount.ToString() : "–", "locks");
                Metric($"{lockedTags}/{known.Count}", "tags");
                Metric(groups.ToString(), "groups");
            }
        }

        private void Metric(string value, string label)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(72f)))
            {
                GUILayout.Label(value, S.MetricValue);
                GUILayout.Label(label, S.MetricLabel);
            }
        }

        private void DrawToolbar(IInputLockService service)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Lock All", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    service.LockAll("Debugger");
                }

                if (GUILayout.Button("Release All", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                {
                    service.Reset();
                    _groupHandles.Clear();
                }

                GUILayout.Space(6f);
                _onlyLocked = GUILayout.Toggle(_onlyLocked, "Locked only", EditorStyles.toolbarButton,
                    GUILayout.Width(84f));

                GUILayout.FlexibleSpace();
                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(90f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _customTag = EditorGUILayout.TextField("Inject tag", _customTag);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_customTag)))
                {
                    if (GUILayout.Button("Lock", GUILayout.Width(56f)))
                    {
                        service.Lock(_customTag.Trim(), "Debugger");
                        _customTag = string.Empty;
                        GUI.FocusControl(null);
                    }
                }
            }

            GUILayout.Space(2f);
        }

        private void DrawTagSection(IInputLockService service)
        {
            var concrete = service as InputLockService;
            var known = service.KnownTagIds;

            using (new EditorGUILayout.VerticalScope(S.Card))
            {
                Section("TAGS", known.Count.ToString());

                var shown = 0;
                for (var i = 0; i < known.Count; i++)
                {
                    var tag = new InputLockTag(known[i]);
                    var count = service.GetLockCount(tag);
                    if (_onlyLocked && count == 0)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(_search) &&
                        tag.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    DrawTagRow(service, concrete, tag, count, shown++);
                }

                if (shown == 0)
                {
                    GUILayout.Label("   (no matching tags)", S.Mini);
                }
            }
        }

        private void DrawTagRow(IInputLockService service, InputLockService concrete, InputLockTag tag, int count, int index)
        {
            var isLocked = count > 0;
            var row = EditorGUILayout.BeginVertical();
            if (index % 2 == 1)
            {
                Fill(row, new Color(1f, 1f, 1f, 0.02f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var expanded = _expandedTags.Contains(tag.Id);
                var newExpanded = EditorGUILayout.Foldout(expanded, GUIContent.none, true);
                if (newExpanded != expanded)
                {
                    Toggle(_expandedTags, tag.Id, newExpanded);
                }

                if (LockIconButton(isLocked))
                {
                    if (!isLocked)
                    {
                        service.Lock(tag, "Debugger");
                    }
                    else if (concrete != null)
                    {
                        concrete.DebugForceUnlockTag(tag.Id);
                    }
                }

                GUILayout.Label(tag.Name, S.RowTitle);
                GUILayout.FlexibleSpace();
                if (isLocked)
                {
                    GUILayout.Label("×" + count, S.CountLabel);
                }
            }

            if (_expandedTags.Contains(tag.Id))
            {
                DrawTagDetails(concrete, tag);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTagDetails(InputLockService concrete, InputLockTag tag)
        {
            EditorGUI.indentLevel++;
            if (concrete != null)
            {
                concrete.CollectOwners(tag.Id, _ownerBuffer);
                EditorGUILayout.LabelField("Owners", S.Mini);
                if (_ownerBuffer.Count == 0)
                {
                    EditorGUILayout.LabelField("      (none)", S.Mini);
                }
                else
                {
                    for (var i = 0; i < _ownerBuffer.Count; i++)
                    {
                        EditorGUILayout.LabelField("      " + ShortOwner(_ownerBuffer[i]), S.Mini);
                    }
                }

                var subscribers = concrete.GetSubscribers(tag.Id);
                EditorGUILayout.LabelField("Affected components", S.Mini);
                if (subscribers == null || subscribers.Count == 0)
                {
                    EditorGUILayout.LabelField("      (none)", S.Mini);
                }
                else
                {
                    for (var i = 0; i < subscribers.Count; i++)
                    {
                        EditorGUILayout.ObjectField(subscribers[i], typeof(InputLockableBehaviour), true);
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawGroupSection(IInputLockService service)
        {
            if (!(service is InputLockService concrete))
            {
                return;
            }

            var count = 0;
            foreach (var _ in concrete.GroupIds)
            {
                count++;
            }

            using (new EditorGUILayout.VerticalScope(S.Card))
            {
                Section("GROUPS", count.ToString());
                if (count == 0)
                {
                    GUILayout.Label("   (no groups)", S.Mini);
                    return;
                }

                foreach (var groupId in concrete.GroupIds)
                {
                    DrawGroupRow(service, groupId);
                }
            }
        }

        private void DrawGroupRow(IInputLockService service, int groupId)
        {
            var group = new InputLockGroup(groupId);
            var members = service.GetGroupMembers(group);
            var lockedMembers = CountLockedMembers(members);

            using (new EditorGUILayout.HorizontalScope())
            {
                var expanded = _expandedGroups.Contains(groupId);
                var newExpanded = EditorGUILayout.Foldout(expanded, GUIContent.none, true);
                if (newExpanded != expanded)
                {
                    Toggle(_expandedGroups, groupId, newExpanded);
                }

                var isDebugLocked = _groupHandles.TryGetValue(groupId, out var handle) && handle.IsActive;
                if (LockIconButton(isDebugLocked))
                {
                    if (!isDebugLocked)
                    {
                        _groupHandles[groupId] = service.LockGroup(group, "Debugger");
                    }
                    else if (handle.IsActive)
                    {
                        handle.Dispose();
                        _groupHandles.Remove(groupId);
                    }
                }

                GUILayout.Label($"{group.Name}  ({members.Count})", S.RowTitle);
                GUILayout.FlexibleSpace();
                if (lockedMembers > 0)
                {
                    var prev = GUI.color;
                    GUI.color = S.Locked;
                    GUILayout.Label($"{lockedMembers}/{members.Count} locked", S.CountLabel);
                    GUI.color = prev;
                }
            }

            if (_expandedGroups.Contains(groupId))
            {
                DrawGroupMembers(members);
            }
        }

        private void DrawGroupMembers(IReadOnlyList<InputLockableBehaviour> members)
        {
            EditorGUI.indentLevel++;
            if (members.Count == 0)
            {
                EditorGUILayout.LabelField("      (no members)", S.Mini);
            }
            else
            {
                for (var i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    if (member == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        LockIconStatus(member.IsLocked);
                        EditorGUILayout.ObjectField(member, typeof(InputLockableBehaviour), true);
                        if (!string.IsNullOrEmpty(member.Identifier))
                        {
                            GUILayout.Label(member.Identifier, S.Mini, GUILayout.Width(92f));
                        }
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawLog()
        {
            using (new EditorGUILayout.VerticalScope(S.Card))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _showLog = EditorGUILayout.Foldout(_showLog, GUIContent.none, true);
                    Section("EVENT LOG", _log.Count.ToString());
                }

                if (!_showLog)
                {
                    return;
                }

                for (var i = _log.Count - 1; i >= 0 && i > _log.Count - 40; i--)
                {
                    var entry = _log[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        LockIconStatus(entry.Locked);
                        GUILayout.Label(entry.Tag, S.RowTitle);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(entry.Time, S.Mini);
                    }
                }

                if (_log.Count > 0 && GUILayout.Button("Clear Log", EditorStyles.miniButton))
                {
                    _log.Clear();
                }
            }
        }

        // ------------------------------------------------------------------ Helpers

        private void Section(string title, string right)
        {
            var rect = GUILayoutUtility.GetRect(1f, 20f, GUILayout.ExpandWidth(true));
            Fill(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), S.Accent);
            GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), title, S.SectionTitle);
            if (!string.IsNullOrEmpty(right))
            {
                GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), right, S.SectionRight);
            }

            GUILayout.Space(2f);
        }

        private void Pill(string text, Color color, float width)
        {
            var rect = GUILayoutUtility.GetRect(width, 16f, GUILayout.Width(width));
            rect.y += 1f;
            rect.height = 14f;
            Fill(rect, color);
            GUI.Label(rect, text, S.PillText);
        }

        /// <summary>A tinted padlock icon that acts as the lock toggle. Returns true when clicked.</summary>
        private bool LockIconButton(bool locked)
        {
            var content = locked ? S.LockOn : S.LockOff;
            if (content.image == null)
            {
                return GUILayout.Button(locked ? "Unlock" : "Lock", EditorStyles.miniButton, GUILayout.Width(52f));
            }

            var prev = GUI.color;
            GUI.color = locked ? S.Locked : new Color(0.74f, 0.74f, 0.76f);
            var clicked = GUILayout.Button(content, EditorStyles.iconButton, GUILayout.Width(22f), GUILayout.Height(18f));
            GUI.color = prev;
            return clicked;
        }

        /// <summary>A small, non-interactive padlock status icon (closed = locked, open = unlocked).</summary>
        private void LockIconStatus(bool locked)
        {
            var content = locked ? S.LockOn : S.LockOff;
            if (content.image == null)
            {
                var fallback = new GUIStyle(S.Mini) { normal = { textColor = locked ? S.Locked : S.Open } };
                GUILayout.Label(locked ? "L" : "U", fallback, GUILayout.Width(16f));
                return;
            }

            var prev = GUI.color;
            GUI.color = locked ? S.Locked : S.Open;
            GUILayout.Label(content, S.IconOnly, GUILayout.Width(20f), GUILayout.Height(16f));
            GUI.color = prev;
        }

        private void Fill(Rect rect, Color color)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, S.White);
            GUI.color = prev;
        }

        private static void Toggle(HashSet<int> set, int id, bool on)
        {
            if (on)
            {
                set.Add(id);
            }
            else
            {
                set.Remove(id);
            }
        }

        private static int CountLockedMembers(IReadOnlyList<InputLockableBehaviour> members)
        {
            var locked = 0;
            for (var i = 0; i < members.Count; i++)
            {
                if (members[i] != null && members[i].IsLocked)
                {
                    locked++;
                }
            }

            return locked;
        }

        private static List<string> CollectCatalogTagNames()
        {
            var result = new List<string>();
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
                    if (!string.IsNullOrEmpty(entries[i].Name) && !result.Contains(entries[i].Name))
                    {
                        result.Add(entries[i].Name);
                    }
                }
            }

            return result;
        }

        private static string ShortOwner(string owner)
        {
            if (string.IsNullOrEmpty(owner))
            {
                return "Unknown";
            }

            return owner.IndexOf('/') >= 0 || owner.IndexOf('\\') >= 0
                ? Path.GetFileNameWithoutExtension(owner)
                : owner;
        }
    }
}
