using UnityEngine;

namespace _Main.Project.Scripts.Utils
{
    public static class GetComponentUtils
    {
        /// <summary>
        /// Searches for the component starting from the specified Collider up to the topmost parent 
        /// (subject to the specified depth limit). Returns null and logs a warning if not found.
        /// </summary>
        public static T GetComponentInParents<T>(this Transform sourceTransform, int maxDepth = 10) where T : class
        {
            if (sourceTransform == null)
            {
                Debug.LogWarning("[GetComponentUtils] Source Transform cannot be null!");
                return null;
            }

            Transform currentTransform = sourceTransform;
            int currentDepth = 0;

            while (currentTransform != null && currentDepth <= maxDepth)
            {
                // Search for the component using TryGetComponent
                if (currentTransform.TryGetComponent<T>(out var component))
                {
                    return component;
                }

                currentTransform = currentTransform.parent;
                currentDepth++;
            }

            Debug.LogWarning($"[GetComponentUtils] '{typeof(T).Name}' component could not be found on '{sourceTransform.name}' or its parent hierarchy (Max Depth: {maxDepth})!");
            return null;
        }

        /// <summary>
        /// Searches for all matching components starting from the specified Transform up to the topmost parent
        /// (subject to the specified depth limit). Returns an empty array and logs a warning if none are found.
        /// </summary>
        public static T[] GetComponentsInParents<T>(this Transform sourceTransform, int maxDepth = 10) where T : class
        {
            if (sourceTransform == null)
            {
                Debug.LogWarning("[GetComponentUtils] Source Transform cannot be null!");
                return System.Array.Empty<T>();
            }

            var components = new System.Collections.Generic.List<T>();
            Transform currentTransform = sourceTransform;
            int currentDepth = 0;

            while (currentTransform != null && currentDepth <= maxDepth)
            {
                AddComponentsOnTransform(currentTransform, components);
                currentTransform = currentTransform.parent;
                currentDepth++;
            }

            if (components.Count == 0)
            {
                Debug.LogWarning($"[GetComponentUtils] '{typeof(T).Name}' components could not be found on '{sourceTransform.name}' or its parent hierarchy (Max Depth: {maxDepth})!");
                return System.Array.Empty<T>();
            }

            return components.ToArray();
        }

        /// <summary>
        /// Searches for the component starting from the specified Transform's object down to the deepest children
        /// (subject to the specified depth limit). Returns null and logs a warning if not found.
        /// </summary>
        public static T GetComponentInChildrenRecursive<T>(this Transform sourceTransform, int maxChildDepth = 10) where T : class
        {
            if (sourceTransform == null)
            {
                Debug.LogWarning("[GetComponentUtils] Source Transform cannot be null!");
                return null;
            }

            // First, check on the object itself
            if (sourceTransform.TryGetComponent<T>(out var selfComponent))
            {
                return selfComponent;
            }

            // Use a recursive approach with depth control when searching child objects
            T foundComponent = SearchInChildrenRecursive<T>(sourceTransform.transform, maxChildDepth, 0);

            if (foundComponent == null)
            {
                Debug.LogWarning($"[GetComponentUtils] '{typeof(T).Name}' component could not be found in the children hierarchy of '{sourceTransform.name}' (Max Depth: {maxChildDepth})!");
            }

            return foundComponent;
        }

        /// <summary>
        /// Searches for all matching components starting from the specified Collider's object down to the deepest children
        /// (subject to the specified depth limit). Returns an empty array and logs a warning if none are found.
        /// </summary>
        public static T[] GetComponentsInChildrenRecursive<T>(this Transform sourceCollider, int maxChildDepth = 10) where T : class
        {
            if (sourceCollider == null)
            {
                Debug.LogWarning("[GetComponentUtils] Source Collider cannot be null!");
                return System.Array.Empty<T>();
            }

            var components = new System.Collections.Generic.List<T>();
            AddComponentsOnTransform(sourceCollider.transform, components);
            SearchComponentsInChildrenRecursive(sourceCollider.transform, maxChildDepth, 0, components);

            if (components.Count == 0)
            {
                Debug.LogWarning($"[GetComponentUtils] '{typeof(T).Name}' components could not be found in the children hierarchy of '{sourceCollider.name}' (Max Depth: {maxChildDepth})!");
                return System.Array.Empty<T>();
            }

            return components.ToArray();
        }

        private static T SearchInChildrenRecursive<T>(Transform currentTransform, int maxDepth, int currentDepth) where T : class
        {
            if (currentDepth >= maxDepth) return null;

            foreach (Transform child in currentTransform)
            {
                if (child.TryGetComponent<T>(out var component))
                {
                    return component;
                }

                // Move down to the next layer
                var deeperComponent = SearchInChildrenRecursive<T>(child, maxDepth, currentDepth + 1);
                if (deeperComponent != null)
                {
                    return deeperComponent;
                }
            }

            return null;
        }

        private static void SearchComponentsInChildrenRecursive<T>(
            Transform currentTransform,
            int maxDepth,
            int currentDepth,
            System.Collections.Generic.List<T> components) where T : class
        {
            if (currentDepth >= maxDepth) return;

            foreach (Transform child in currentTransform)
            {
                AddComponentsOnTransform(child, components);
                SearchComponentsInChildrenRecursive(child, maxDepth, currentDepth + 1, components);
            }
        }

        private static void AddComponentsOnTransform<T>(
            Transform transform,
            System.Collections.Generic.List<T> components) where T : class
        {
            Component[] attachedComponents = transform.GetComponents<Component>();
            for (int i = 0; i < attachedComponents.Length; i++)
            {
                if (attachedComponents[i] is T component)
                {
                    components.Add(component);
                }
            }
        }
    }
}
