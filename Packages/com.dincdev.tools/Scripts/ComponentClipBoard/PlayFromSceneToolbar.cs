#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorTools
{
    [InitializeOnLoad]
    public static class PlayFromSceneToolbar
    {
        const string PrefKey = "PlayFromScene.SelectedScenePath";

        static EditorToolbarDropdown _dropdown;

        static PlayFromSceneToolbar()
        {
            _dropdown = BuildDropdown();
            ToolbarExtender.LeftToolbarElements.Add(_dropdown);

            string saved = EditorPrefs.GetString(PrefKey, string.Empty);
            ApplyStartScene(saved);
            RefreshLabel(saved);
        }

        static EditorToolbarDropdown BuildDropdown()
        {
            var dropdown = new EditorToolbarDropdown
            {
                tooltip = "Play'e basinca baslayacak sahne"
            };

            dropdown.style.marginLeft = StyleKeyword.Auto;
            dropdown.style.marginRight = 4;

            var icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            if (icon != null) dropdown.icon = icon;

            dropdown.clicked += () => ShowMenu(dropdown);
            return dropdown;
        }

        static void ShowMenu(VisualElement anchor)
        {
            string currentPath = EditorPrefs.GetString(PrefKey, string.Empty);
            var menu = new GenericMenu();

            menu.AddItem(
                new GUIContent("Aktif Sahne (Varsayilan)"),
                string.IsNullOrEmpty(currentPath),
                () => SelectScene(string.Empty));

            menu.AddSeparator(string.Empty);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();
            if (scenes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("Build Settings'te etkin sahne yok"));
            }
            else
            {
                foreach (var scene in scenes)
                {
                    string path = scene.path;
                    string name = Path.GetFileNameWithoutExtension(path);
                    menu.AddItem(new GUIContent(name), path == currentPath, () => SelectScene(path));
                }
            }

            menu.DropDown(anchor.worldBound);
        }

        static void SelectScene(string path)
        {
            EditorPrefs.SetString(PrefKey, path);
            ApplyStartScene(path);
            RefreshLabel(path);

            if (!EditorApplication.isPlaying && !string.IsNullOrEmpty(path))
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(path);
        }

        static void ApplyStartScene(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (sceneAsset == null)
            {
                EditorSceneManager.playModeStartScene = null;
                EditorPrefs.SetString(PrefKey, string.Empty);
                return;
            }

            EditorSceneManager.playModeStartScene = sceneAsset;
        }

        static void RefreshLabel(string path)
        {
            if (_dropdown == null) return;
            _dropdown.text = string.IsNullOrEmpty(path)
                ? "Current Active Scene"
                : Path.GetFileNameWithoutExtension(path);
        }
    }
}
#endif
