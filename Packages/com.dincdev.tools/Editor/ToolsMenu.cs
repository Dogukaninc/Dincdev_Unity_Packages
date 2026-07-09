using UnityEditor;
using UnityEngine;

using static System.IO.Directory;
using static System.IO.Path;
using static UnityEngine.Application;
using static UnityEditor.AssetDatabase;

namespace dincdev
{
    public static class ToolsMenu
    {

        [MenuItem("Tools/Create Default Folders")]
        public static void CreateDefaultFolders()
        {
            Dir("_Main", "Scripts", "Art");
            SubDir("_Main", "Scripts", "Player", "Managers", "Operations", "UI", "Prefabs");
            SubDir("_Main", "Art", "SFX", "Models", "Prefabs", "UI", "Materials", "Textures", "Animations");
            CreateHierarchyObjects();
            Refresh();
        }

        public static void Dir(string root, params string[] dir)
        {
            var fullPath = Combine(dataPath, root);
            foreach (var newDirectory in dir) CreateDirectory(Combine(fullPath, newDirectory));
        }

        public static void SubDir(string root, string sub, params string[] dir)
        {
            var fullPath = Combine(dataPath, root, sub);
            foreach (var newDirectory in dir) CreateDirectory(Combine(fullPath, newDirectory));
        }

        public static void CreateHierarchyObjects()
        {
            new GameObject("--SETUP--");
            new GameObject("--HANDLERS--");
            new GameObject("--ENVIRONMENT--");
        }
    }
}
