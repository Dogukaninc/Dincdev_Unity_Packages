using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    // =====================================================================
    //  ComponentClipboard — data, context menus, paste logic
    // =====================================================================
    public static class ComponentClipboard
    {
        public class Entry
        {
            public Type Type;
            public string Json;          // EditorJsonUtility snapshot
            public string SourceName;    // GameObject name
            public bool Selected = true;
            public bool ValuesExpanded;
        }

        public static readonly List<Entry> Entries = new();

        public static event Action Changed;

        public static List<Entry> SelectedEntries => Entries.Where(e => e.Selected).ToList();

        // ---------- COPY (context menu) ----------

        [MenuItem("CONTEXT/Component/Copy → Add to Clipboard", priority = 501)]
        private static void Copy(MenuCommand command)
        {
            var component = (Component)command.context;
            Add(component);
            Debug.Log($"[ComponentClipboard] Added: {component.GetType().Name} ({component.gameObject.name})");

            ComponentClipboardWindow.OpenIfClosed();
        }

        [MenuItem("CONTEXT/Transform/Copy → Add to Clipboard", priority = 501)]
        private static void CopyTransform(MenuCommand command) => Copy(command);

        // ---------- PASTE (context menu) ----------

        [MenuItem("CONTEXT/Component/Paste Selected From Clipboard", priority = 502)]
        private static void PasteSelectedMenu(MenuCommand command)
        {
            var target = ((Component)command.context).gameObject;
            PasteSelected(target);
        }

        [MenuItem("CONTEXT/Transform/Paste Selected From Clipboard", priority = 502)]
        private static void PasteSelectedTransformMenu(MenuCommand command) => PasteSelectedMenu(command);

        [MenuItem("CONTEXT/Component/Paste Selected From Clipboard", validate = true)]
        [MenuItem("CONTEXT/Transform/Paste Selected From Clipboard", validate = true)]
        private static bool ValidatePaste(MenuCommand command) => SelectedEntries.Count > 0;

        // ---------- Data operations ----------

        public static void Add(Component component)
        {
            string sourceName = component.gameObject.name;
            Type type = component.GetType();
            string json = EditorJsonUtility.ToJson(component);

            int i = Entries.FindIndex(e => e.Type == type && e.SourceName == sourceName);
            if (i >= 0)
            {
                var entry = Entries[i];
                entry.Json = json;
                entry.Selected = true;
            }
            else
            {
                Entries.Add(new Entry
                {
                    Type = type,
                    Json = json,
                    SourceName = sourceName,
                    Selected = true
                });
            }

            Changed?.Invoke();
        }

        public static void Remove(Entry entry)
        {
            Entries.Remove(entry);
            Changed?.Invoke();
        }

        public static void Clear()
        {
            Entries.Clear();
            Changed?.Invoke();
        }

        // ---------- Paste logic ----------

        public static void PasteSelected(GameObject target)
        {
            var selected = SelectedEntries;
            if (selected.Count == 0) return;

            Undo.SetCurrentGroupName("Paste Components From Clipboard");
            int group = Undo.GetCurrentGroup();

            int success = 0;
            foreach (var entry in selected)
                if (Paste(entry, target)) success++;

            Undo.CollapseUndoOperations(group);
            Debug.Log($"[ComponentClipboard] {success}/{selected.Count} component(s) pasted onto '{target.name}'.");
        }

        public static bool Paste(Entry entry, GameObject target)
        {
            var existing = target.GetComponent(entry.Type);

            if (existing != null)
            {
                Undo.RecordObject(existing, $"Paste {entry.Type.Name} Values");
                EditorJsonUtility.FromJsonOverwrite(entry.Json, existing);
                EditorUtility.SetDirty(existing);
                return true;
            }

            var added = Undo.AddComponent(target, entry.Type);
            if (added == null)
            {
                Debug.LogWarning($"[ComponentClipboard] {entry.Type.Name} could not be added to '{target.name}' " +
                                 "(RequireComponent / DisallowMultiple restriction).");
                return false;
            }

            EditorJsonUtility.FromJsonOverwrite(entry.Json, added);
            EditorUtility.SetDirty(added);
            return true;
        }
    }

    // =====================================================================
    //  ComponentClipboardWindow — grouped display by source object
    // =====================================================================
    public class ComponentClipboardWindow : EditorWindow
    {
        private Vector2 _scroll;
        private readonly Dictionary<string, bool> _foldouts = new();
        private readonly Dictionary<ComponentClipboard.Entry, (Component comp, string json)> _previews = new();

        [MenuItem("Tools/Component Clipboard")]
        public static void Open()
        {
            var window = GetWindow<ComponentClipboardWindow>("Component Clipboard");
            window.minSize = new Vector2(350, 250);
        }

        public static void OpenIfClosed()
        {
            if (!HasOpenInstances<ComponentClipboardWindow>())
                GetWindow<ComponentClipboardWindow>("Component Clipboard", false);
        }

        private void OnEnable() => ComponentClipboard.Changed += Repaint;
        private void OnDisable()
        {
            ComponentClipboard.Changed -= Repaint;
            DestroyAllPreviews();
        }

        private void OnDestroy() => DestroyAllPreviews();

        private void DestroyAllPreviews()
        {
            foreach (var kvp in _previews)
            {
                if (kvp.Value.comp != null)
                    DestroyImmediate(kvp.Value.comp.gameObject);
            }
            _previews.Clear();
        }

        private Component GetOrCreatePreview(ComponentClipboard.Entry entry)
        {
            if (_previews.TryGetValue(entry, out var existing) && existing.comp != null && existing.json == entry.Json)
                return existing.comp;

            // Remove old preview if json changed
            if (existing.comp != null)
                DestroyImmediate(existing.comp.gameObject);

            var go = new GameObject("__ClipboardPreview__", entry.Type);
            go.hideFlags = HideFlags.HideAndDontSave;
            var comp = go.GetComponent(entry.Type);
            EditorJsonUtility.FromJsonOverwrite(entry.Json, comp);
            _previews[entry] = (comp, entry.Json);
            return comp;
        }

        private void OnGUI()
        {
            var entries = ComponentClipboard.Entries;

            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Clipboard is empty.\n\nRight-click a component's ⋮ menu and " +
                    "choose \"Copy → Add to Clipboard\" to add entries here.",
                    MessageType.Info);
                return;
            }

            DrawToolbar(entries);

            var toRemove = new List<ComponentClipboard.Entry>();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                var grouped = entries.GroupBy(e => e.SourceName)
                                     .OrderBy(g => g.Key)
                                     .ToList();

                foreach (var group in grouped)
                {
                    if (!_foldouts.ContainsKey(group.Key))
                        _foldouts[group.Key] = true;

                    _foldouts[group.Key] = EditorGUILayout.Foldout(_foldouts[group.Key], group.Key, true, EditorStyles.foldoutHeader);

                    if (!_foldouts[group.Key])
                        continue;

                    EditorGUI.indentLevel++;
                    foreach (var entry in group)
                    {
                        DrawEntry(entry, toRemove);
                    }
                    EditorGUI.indentLevel--;
                }

                foreach (var entry in toRemove)
                    ComponentClipboard.Remove(entry);

                // Clean up previews for removed entries
                foreach (var entry in toRemove)
                {
                    if (_previews.TryGetValue(entry, out var data) && data.comp != null)
                        DestroyImmediate(data.comp.gameObject);
                    _previews.Remove(entry);
                }
            }

            EditorGUILayout.Space(4);
            int selectedCount = ComponentClipboard.SelectedEntries.Count;
            EditorGUILayout.LabelField(
                $"{selectedCount}/{entries.Count} selected — " +
                "right-click a component's ⋮ menu on the target to paste.",
                EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawEntry(ComponentClipboard.Entry entry, List<ComponentClipboard.Entry> toRemove)
        {
            var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope())
            {
                entry.Selected = EditorGUILayout.ToggleLeft(" ", entry.Selected, GUILayout.Width(24), GUILayout.Height(20));

                GUILayout.Space(10);

                var icon = GetIcon(entry.Type);
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(18));

                GUILayout.Space(4);

                EditorGUILayout.LabelField(
                    ObjectNames.NicifyVariableName(entry.Type.Name),
                    EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                entry.ValuesExpanded = GUILayout.Toggle(entry.ValuesExpanded, "▼", EditorStyles.miniButton, GUILayout.Width(22));

                if (GUILayout.Button("✕", GUILayout.Width(22)))
                    toRemove.Add(entry);
            }

            if (entry.ValuesExpanded)
            {
                var preview = GetOrCreatePreview(entry);
                if (preview != null)
                {
                    EditorGUI.indentLevel++;
                    using (var so = new SerializedObject(preview))
                    {
                        var prop = so.GetIterator();
                        if (prop.NextVisible(true))
                        {
                            while (prop.NextVisible(false))
                                EditorGUILayout.PropertyField(prop, true);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar(List<ComponentClipboard.Entry> entries)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Select All", EditorStyles.toolbarButton))
                    entries.ForEach(e => e.Selected = true);

                if (GUILayout.Button("Deselect All", EditorStyles.toolbarButton))
                    entries.ForEach(e => e.Selected = false);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear List", EditorStyles.toolbarButton))
                    ComponentClipboard.Clear();
            }
        }

        private static Texture GetIcon(Type type)
        {
            var icon = EditorGUIUtility.ObjectContent(null, type).image;
            return icon != null ? icon : EditorGUIUtility.IconContent("cs Script Icon").image;
        }
    }
}
