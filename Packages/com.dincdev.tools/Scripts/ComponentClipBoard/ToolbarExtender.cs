#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorTools
{
    [InitializeOnLoad]
    public static class ToolbarExtender
    {
        public static readonly List<VisualElement> LeftToolbarElements = new List<VisualElement>();
        public static readonly List<VisualElement> RightToolbarElements = new List<VisualElement>();

        static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        static ScriptableObject _currentToolbar;

        static ToolbarExtender()
        {
            EditorApplication.update -= TryAttach;
            EditorApplication.update += TryAttach;
        }

        static void TryAttach()
        {
            if (_currentToolbar != null) return;

            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            _currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (_currentToolbar == null) return;

            var rootField = _currentToolbar.GetType()
                .GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rootField == null) return;

            var root = rootField.GetValue(_currentToolbar) as VisualElement;
            if (root == null) return;

            InjectZone(root, "ToolbarZoneLeftAlign", LeftToolbarElements);
            InjectZone(root, "ToolbarZoneRightAlign", RightToolbarElements);
        }

        static void InjectZone(VisualElement root, string zoneName, List<VisualElement> elements)
        {
            var zone = root.Q(zoneName);
            if (zone == null) return;

            foreach (var element in elements)
            {
                if (element.parent != zone)
                    zone.Add(element);
            }
        }
    }
}
#endif