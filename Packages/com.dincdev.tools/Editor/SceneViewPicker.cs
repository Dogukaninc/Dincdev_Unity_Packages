#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SceneViewPicker
{
    private static readonly List<PickerCandidate> UiCandidates = new();
    private static readonly List<PickerCandidate> WorldCandidates = new();
    private static readonly List<RaycastResult> UiRaycastResults = new();
    private static bool _isShortcutHeld;
    private static EventSystem _temporaryEventSystem;

    static SceneViewPicker()
    {
        SceneView.duringSceneGui += OnSceneGui;
    }

    private static void OnSceneGui(SceneView sceneView)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null)
        {
            return;
        }

        HandleShortcutState(currentEvent);
        DrawOverlay(sceneView);

        if (!_isShortcutHeld)
        {
            return;
        }

        if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
        {
            return;
        }

        if (currentEvent.alt)
        {
            return;
        }

        BuildCandidates(sceneView, currentEvent.mousePosition);
        SceneViewPickerWindow.ShowPicker(UiCandidates, WorldCandidates);
        currentEvent.Use();
        GUIUtility.ExitGUI();
    }

    private static void HandleShortcutState(Event currentEvent)
    {
        SceneViewPickerShortcutSettings settings = SceneViewPickerShortcutSettings.instance;

        if (currentEvent.type == EventType.KeyDown && MatchesShortcut(currentEvent, settings))
        {
            _isShortcutHeld = true;
            currentEvent.Use();
            SceneView.RepaintAll();
            return;
        }

        if (currentEvent.type == EventType.KeyUp && currentEvent.keyCode == settings.Key)
        {
            _isShortcutHeld = false;
            SceneView.RepaintAll();
        }

        if (currentEvent.type == EventType.MouseLeaveWindow)
        {
            _isShortcutHeld = false;
        }
    }

    private static bool MatchesShortcut(Event currentEvent, SceneViewPickerShortcutSettings settings)
    {
        if (currentEvent.keyCode != settings.Key)
        {
            return false;
        }

        EventModifiers modifiers = currentEvent.modifiers;
        bool shift = (modifiers & EventModifiers.Shift) != 0;
        bool control = (modifiers & EventModifiers.Control) != 0;
        bool alt = (modifiers & EventModifiers.Alt) != 0;
        bool command = (modifiers & EventModifiers.Command) != 0;

        return
            shift == settings.RequireShift &&
            control == settings.RequireControl &&
            alt == settings.RequireAlt &&
            command == settings.RequireCommand;
    }

    private static void DrawOverlay(SceneView sceneView)
    {
        if (!_isShortcutHeld || Event.current.type != EventType.Repaint)
        {
            return;
        }

        Handles.BeginGUI();

        Rect rect = new Rect(12f, 12f, 290f, 42f);
        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
        GUI.Label(
            rect,
            "Scene Picker aktif\nSol tik: pencere ac | " + SceneViewPickerShortcutSettings.instance.GetDisplayString(),
            EditorStyles.whiteLabel);

        Handles.EndGUI();
    }

    private static void BuildCandidates(SceneView sceneView, Vector2 mousePosition)
    {
        UiCandidates.Clear();
        WorldCandidates.Clear();

        CollectUiCandidates(sceneView.camera, mousePosition, UiCandidates);
        CollectWorldCandidates(sceneView.camera, mousePosition, WorldCandidates);
    }

    private static void CollectUiCandidates(Camera sceneCamera, Vector2 mousePosition, List<PickerCandidate> results)
    {
        Vector2 screenPixelPosition = HandleUtility.GUIPointToScreenPixelCoordinate(mousePosition);
        PointerEventData eventData = new PointerEventData(GetEventSystem())
        {
            position = screenPixelPosition
        };

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (!canvas.isActiveAndEnabled || SceneVisibilityManager.instance.IsHidden(canvas.gameObject))
            {
                continue;
            }

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                UiRaycastResults.Clear();
                raycaster.Raycast(eventData, UiRaycastResults);

                for (int index = 0; index < UiRaycastResults.Count; index++)
                {
                    RaycastResult raycastResult = UiRaycastResults[index];
                    GameObject candidate = raycastResult.gameObject;
                    if (candidate == null || SceneVisibilityManager.instance.IsHidden(candidate))
                    {
                        continue;
                    }

                    Graphic graphic = candidate.GetComponent<Graphic>();
                    RectTransform rectTransform = candidate.transform as RectTransform;
                    if (rectTransform == null)
                    {
                        continue;
                    }

                    AddOrReplaceCandidate(
                        results,
                        candidate,
                        "UI",
                        GetUiDescriptor(rectTransform, graphic),
                        raycastResult.depth,
                        ComputeUiRaycastScore(raycastResult, graphic, rectTransform));
                }

                continue;
            }

            CollectUiRectFallback(sceneCamera, mousePosition, results, canvas);
        }

        results.Sort(PickerCandidateComparer.Instance);
    }

    private static void CollectUiRectFallback(Camera sceneCamera, Vector2 mousePosition, List<PickerCandidate> results, Canvas canvas)
    {
            RectTransform[] rectTransforms = canvas.GetComponentsInChildren<RectTransform>(false);
            if (rectTransforms == null || rectTransforms.Length == 0)
            {
                return;
            }

            for (int index = 0; index < rectTransforms.Length; index++)
            {
                RectTransform rectTransform = rectTransforms[index];
                if (!IsRectTransformPickable(rectTransform, mousePosition, sceneCamera))
                {
                    continue;
                }

                Graphic graphic = rectTransform.GetComponent<Graphic>();
                if (graphic != null && (!graphic.isActiveAndEnabled || graphic.canvasRenderer.cull))
                {
                    continue;
                }

                if (graphic != null && graphic.raycastTarget && !graphic.Raycast(mousePosition, sceneCamera))
                {
                    continue;
                }

                AddOrReplaceCandidate(
                    results,
                    rectTransform.gameObject,
                    "UI",
                    GetUiDescriptor(rectTransform, graphic),
                    graphic != null ? graphic.depth : GetTransformDepth(rectTransform),
                    ComputeUiScore(canvas, rectTransform, graphic, sceneCamera));
            }
    }

    private static EventSystem GetEventSystem()
    {
        if (EventSystem.current != null)
        {
            return EventSystem.current;
        }

        if (_temporaryEventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("SceneViewPickerEventSystem")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _temporaryEventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        return _temporaryEventSystem;
    }

    private static bool IsRectTransformPickable(RectTransform rectTransform, Vector2 mousePosition, Camera sceneCamera)
    {
        if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (SceneVisibilityManager.instance.IsHidden(rectTransform.gameObject))
        {
            return false;
        }

        Rect rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition, sceneCamera);
    }

    private static void CollectWorldCandidates(Camera sceneCamera, Vector2 mousePosition, List<PickerCandidate> results)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        RaycastHit[] hits3D = Physics.RaycastAll(ray, sceneCamera.farClipPlane);
        for (int index = 0; index < hits3D.Length; index++)
        {
            RaycastHit hit = hits3D[index];
            if (hit.collider == null)
            {
                continue;
            }

            GameObject candidate = hit.collider.gameObject;
            if (SceneVisibilityManager.instance.IsHidden(candidate))
            {
                continue;
            }

            AddOrReplaceCandidate(
                results,
                candidate,
                "3D",
                hit.collider.GetType().Name,
                0,
                -hit.distance);
        }

        RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(ray, sceneCamera.farClipPlane);
        for (int index = 0; index < hits2D.Length; index++)
        {
            RaycastHit2D hit = hits2D[index];
            if (hit.collider == null)
            {
                continue;
            }

            GameObject candidate = hit.collider.gameObject;
            if (SceneVisibilityManager.instance.IsHidden(candidate))
            {
                continue;
            }

            AddOrReplaceCandidate(
                results,
                candidate,
                "2D",
                hit.collider.GetType().Name,
                0,
                -hit.distance);
        }

        GameObject topScenePick = HandleUtility.PickGameObject(mousePosition, false);
        if (topScenePick != null && !SceneVisibilityManager.instance.IsHidden(topScenePick))
        {
            AddOrReplaceCandidate(results, topScenePick, "Scene", "Renderer", 0, 0.5f);
        }

        results.Sort(PickerCandidateComparer.Instance);
    }

    private static void AddOrReplaceCandidate(
        List<PickerCandidate> results,
        GameObject target,
        string group,
        string descriptor,
        int depth,
        float score)
    {
        if (target == null)
        {
            return;
        }

        for (int index = 0; index < results.Count; index++)
        {
            if (results[index].Target != target)
            {
                continue;
            }

            if (score > results[index].Score)
            {
                results[index] = new PickerCandidate(target, group, descriptor, depth, score);
            }

            return;
        }

        results.Add(new PickerCandidate(target, group, descriptor, depth, score));
    }

    private static float ComputeUiScore(Canvas canvas, RectTransform rectTransform, Graphic graphic, Camera sceneCamera)
    {
        int sortingLayerValue = SortingLayer.GetLayerValueFromID(canvas.sortingLayerID);
        int sortingOrder = canvas.sortingOrder;
        int renderOrder = canvas.rootCanvas != null ? canvas.rootCanvas.renderOrder : 0;
        int graphicDepth = graphic != null ? graphic.depth : 0;
        int transformDepth = GetTransformDepth(rectTransform);
        int siblingIndex = rectTransform.GetSiblingIndex();

        float distanceBonus = 0f;
        if (canvas.renderMode == RenderMode.WorldSpace && sceneCamera != null)
        {
            distanceBonus = -Vector3.Distance(sceneCamera.transform.position, rectTransform.position) * 0.001f;
        }

        return
            sortingLayerValue * 1000000f +
            sortingOrder * 10000f +
            renderOrder * 100f +
            transformDepth * 10f +
            siblingIndex +
            graphicDepth * 0.01f +
            distanceBonus;
    }

    private static float ComputeUiRaycastScore(RaycastResult raycastResult, Graphic graphic, RectTransform rectTransform)
    {
        int transformDepth = GetTransformDepth(rectTransform);
        int graphicDepth = graphic != null ? graphic.depth : 0;

        return
            raycastResult.sortingOrder * 10000f +
            raycastResult.depth * 100f +
            transformDepth * 10f +
            graphicDepth +
            -raycastResult.distance;
    }

    private static int GetTransformDepth(Transform transform)
    {
        int depth = 0;
        while (transform != null)
        {
            depth++;
            transform = transform.parent;
        }

        return depth;
    }

    private static string GetUiDescriptor(RectTransform rectTransform, Graphic graphic)
    {
        if (graphic != null)
        {
            return graphic.GetType().Name;
        }

        Selectable selectable = rectTransform.GetComponent<Selectable>();
        if (selectable != null)
        {
            return selectable.GetType().Name;
        }

        return "RectTransform";
    }

    internal static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    internal static void SelectCandidate(PickerCandidate candidate)
    {
        if (candidate.Target == null)
        {
            return;
        }

        Selection.activeGameObject = candidate.Target;
        EditorGUIUtility.PingObject(candidate.Target);
    }

    internal readonly struct PickerCandidate
    {
        public readonly GameObject Target;
        public readonly string Group;
        public readonly string Descriptor;
        public readonly int Depth;
        public readonly float Score;

        public PickerCandidate(GameObject target, string group, string descriptor, int depth, float score)
        {
            Target = target;
            Group = group;
            Descriptor = descriptor;
            Depth = depth;
            Score = score;
        }
    }

    private sealed class PickerCandidateComparer : IComparer<PickerCandidate>
    {
        public static readonly PickerCandidateComparer Instance = new();

        public int Compare(PickerCandidate left, PickerCandidate right)
        {
            int scoreCompare = right.Score.CompareTo(left.Score);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            int depthCompare = right.Depth.CompareTo(left.Depth);
            if (depthCompare != 0)
            {
                return depthCompare;
            }

            return string.CompareOrdinal(left.Target != null ? left.Target.name : string.Empty, right.Target != null ? right.Target.name : string.Empty);
        }
    }
}

[FilePath("UserSettings/SceneViewPickerShortcutSettings.asset", FilePathAttribute.Location.ProjectFolder)]
public sealed class SceneViewPickerShortcutSettings : ScriptableSingleton<SceneViewPickerShortcutSettings>
{
    [SerializeField] private KeyCode _key = KeyCode.Q;
    [SerializeField] private bool _requireShift = true;
    [SerializeField] private bool _requireControl;
    [SerializeField] private bool _requireAlt;
    [SerializeField] private bool _requireCommand;

    public KeyCode Key => _key;
    public bool RequireShift => _requireShift;
    public bool RequireControl => _requireControl;
    public bool RequireAlt => _requireAlt;
    public bool RequireCommand => _requireCommand;

    public void Apply(KeyCode key, bool requireShift, bool requireControl, bool requireAlt, bool requireCommand)
    {
        _key = key;
        _requireShift = requireShift;
        _requireControl = requireControl;
        _requireAlt = requireAlt;
        _requireCommand = requireCommand;
        Save(true);
    }

    public string GetDisplayString()
    {
        List<string> parts = new List<string>();

        if (_requireControl)
        {
            parts.Add("Ctrl");
        }

        if (_requireShift)
        {
            parts.Add("Shift");
        }

        if (_requireAlt)
        {
            parts.Add("Alt");
        }

        if (_requireCommand)
        {
            parts.Add("Cmd");
        }

        parts.Add(_key.ToString());
        return string.Join(" + ", parts);
    }
}

public sealed class SceneViewPickerWindow : EditorWindow
{
    [SerializeField] private TreeViewState _treeViewState;
    [SerializeField] private string _searchString;

    private SceneViewPickerTreeView _treeView;
    private SearchField _searchField;

    internal static void ShowPicker(
        IReadOnlyList<SceneViewPicker.PickerCandidate> uiCandidates,
        IReadOnlyList<SceneViewPicker.PickerCandidate> worldCandidates)
    {
        SceneViewPickerWindow window = GetWindow<SceneViewPickerWindow>("Scene Picker");
        window.minSize = new Vector2(320f, 240f);
        window.Show();
        window.Focus();
        window.SetCandidates(uiCandidates, worldCandidates);
    }

    private void OnEnable()
    {
        _treeViewState ??= new TreeViewState();
        _searchField ??= new SearchField();
        _treeView ??= new SceneViewPickerTreeView(_treeViewState);
        _treeView.searchString = _searchString ?? string.Empty;
    }

    private void OnGUI()
    {
        DrawToolbar();

        Rect treeRect = GUILayoutUtility.GetRect(0f, 100000f, 0f, 100000f);
        _treeView.OnGUI(treeRect);
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Scene Picker", EditorStyles.toolbarButton, GUILayout.Width(90f));
            GUILayout.FlexibleSpace();

            _treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString);
            _searchString = _treeView.searchString;
        }
    }

    private void SetCandidates(
        IReadOnlyList<SceneViewPicker.PickerCandidate> uiCandidates,
        IReadOnlyList<SceneViewPicker.PickerCandidate> worldCandidates)
    {
        _treeView ??= new SceneViewPickerTreeView(_treeViewState ??= new TreeViewState());
        _treeView.SetCandidates(uiCandidates, worldCandidates);
        Repaint();
    }

    private sealed class SceneViewPickerTreeView : TreeView
    {
        private readonly List<SceneViewPicker.PickerCandidate> _uiCandidates = new();
        private readonly List<SceneViewPicker.PickerCandidate> _worldCandidates = new();
        private readonly Dictionary<int, SceneViewPicker.PickerCandidate> _leafCandidates = new();

        private int _nextId;

        public SceneViewPickerTreeView(TreeViewState state) : base(state)
        {
            showBorder = true;
            Reload();
        }

        public void SetCandidates(
            IReadOnlyList<SceneViewPicker.PickerCandidate> uiCandidates,
            IReadOnlyList<SceneViewPicker.PickerCandidate> worldCandidates)
        {
            _uiCandidates.Clear();
            _worldCandidates.Clear();

            if (uiCandidates != null)
            {
                _uiCandidates.AddRange(uiCandidates);
            }

            if (worldCandidates != null)
            {
                _worldCandidates.AddRange(worldCandidates);
            }

            Reload();
            ExpandAll();
        }

        protected override TreeViewItem BuildRoot()
        {
            _nextId = 1;
            _leafCandidates.Clear();

            TreeViewItem root = new TreeViewItem(0, -1, "Root")
            {
                children = new List<TreeViewItem>()
            };

            BuildCategory(root, "UI", _uiCandidates);
            BuildCategory(root, "World", _worldCandidates);

            if (root.children.Count == 0)
            {
                root.children.Add(new TreeViewItem(GetNextId(), 0, "No results"));
            }

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override void SingleClickedItem(int id)
        {
            if (_leafCandidates.TryGetValue(id, out SceneViewPicker.PickerCandidate candidate))
            {
                SceneViewPicker.SelectCandidate(candidate);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            if (_leafCandidates.TryGetValue(id, out SceneViewPicker.PickerCandidate candidate) && candidate.Target != null)
            {
                Selection.activeGameObject = candidate.Target;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        private void BuildCategory(TreeViewItem root, string categoryName, List<SceneViewPicker.PickerCandidate> candidates)
        {
            if (candidates.Count == 0)
            {
                return;
            }

            TreeViewItem categoryRoot = new TreeViewItem(GetNextId(), 0, categoryName)
            {
                children = new List<TreeViewItem>()
            };

            foreach (SceneViewPicker.PickerCandidate candidate in candidates)
            {
                AddCandidatePath(categoryRoot, candidate);
            }

            SortTreeRecursive(categoryRoot);
            root.children.Add(categoryRoot);
        }

        private void AddCandidatePath(TreeViewItem categoryRoot, SceneViewPicker.PickerCandidate candidate)
        {
            if (candidate.Target == null)
            {
                return;
            }

            List<string> segments = GetHierarchySegments(candidate.Target.transform);
            TreeViewItem current = categoryRoot;

            for (int index = 0; index < segments.Count; index++)
            {
                bool isLeaf = index == segments.Count - 1;
                string displayName = isLeaf ? segments[index] + " [" + candidate.Descriptor + "]" : segments[index];
                current.children ??= new List<TreeViewItem>();

                TreeViewItem child = current.children.FirstOrDefault(item => item.displayName == displayName);
                if (child == null)
                {
                    child = new TreeViewItem(GetNextId(), current.depth + 1, displayName)
                    {
                        children = new List<TreeViewItem>()
                    };
                    current.children.Add(child);
                }

                current = child;
            }

            _leafCandidates[current.id] = candidate;
        }

        private static List<string> GetHierarchySegments(Transform transform)
        {
            List<string> segments = new List<string>();

            while (transform != null)
            {
                segments.Add(transform.name);
                transform = transform.parent;
            }

            segments.Reverse();
            return segments;
        }

        private static void SortTreeRecursive(TreeViewItem item)
        {
            if (item.children == null || item.children.Count == 0)
            {
                return;
            }

            item.children.Sort((left, right) => string.CompareOrdinal(left.displayName, right.displayName));

            for (int index = 0; index < item.children.Count; index++)
            {
                SortTreeRecursive(item.children[index]);
            }
        }

        private int GetNextId()
        {
            return _nextId++;
        }
    }
}

public sealed class SceneViewPickerShortcutSettingsWindow : EditorWindow
{
    private KeyCode _key;
    private bool _requireShift;
    private bool _requireControl;
    private bool _requireAlt;
    private bool _requireCommand;

    [MenuItem("Flickle/Scene Picker Shortcut")]
    private static void Open()
    {
        GetWindow<SceneViewPickerShortcutSettingsWindow>("Scene Picker Shortcut");
    }

    private void OnEnable()
    {
        LoadFromSettings();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scene Picker Shortcut", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Scene view odaktayken bu kombinasyonu basili tutup sol tik yaptiginda picker acilir.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        _key = (KeyCode)EditorGUILayout.EnumPopup("Key", _key);
        _requireShift = EditorGUILayout.Toggle("Shift", _requireShift);
        _requireControl = EditorGUILayout.Toggle("Ctrl", _requireControl);
        _requireAlt = EditorGUILayout.Toggle("Alt", _requireAlt);
        _requireCommand = EditorGUILayout.Toggle("Cmd", _requireCommand);

        GUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Default"))
            {
                _key = KeyCode.Q;
                _requireShift = true;
                _requireControl = false;
                _requireAlt = false;
                _requireCommand = false;
            }

            if (GUILayout.Button("Save"))
            {
                SaveToSettings();
            }
        }

        GUILayout.Space(8f);
        EditorGUILayout.LabelField("Current", SceneViewPickerShortcutSettings.instance.GetDisplayString());

        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }
    }

    private void LoadFromSettings()
    {
        SceneViewPickerShortcutSettings settings = SceneViewPickerShortcutSettings.instance;
        _key = settings.Key;
        _requireShift = settings.RequireShift;
        _requireControl = settings.RequireControl;
        _requireAlt = settings.RequireAlt;
        _requireCommand = settings.RequireCommand;
    }

    private void SaveToSettings()
    {
        SceneViewPickerShortcutSettings.instance.Apply(_key, _requireShift, _requireControl, _requireAlt, _requireCommand);
        SceneView.RepaintAll();
    }
}
#endif
