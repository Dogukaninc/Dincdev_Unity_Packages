using System;
using UnityEngine;

namespace _Main.Project.Scripts.Utils
{
    public static class RaycastProviderUtility
    {
        public static T CastCertainObject<T>(Vector3 position, float rayLength) where T : Component
        {
            Ray downwardRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(downwardRay, out RaycastHit hitInfo, rayLength))
            {
                if (hitInfo.collider.TryGetComponent(out T component))
                {
                    return component;
                }
            }

            return null;
        }

        public static T CastCertainObject<T>(Vector3 position, float rayLength, LayerMask layerMask) where T : Component
        {
            Ray downwardRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(downwardRay, out RaycastHit hitInfo, rayLength, layerMask))
            {
                if (hitInfo.collider.TryGetComponent(out T component))
                {
                    return component;
                }
            }

            return null;
        }
        
        public static T CastCertainType<T>(Vector3 position, float rayLength) where T : IEquatable<T>
        {
            Ray downwardRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(downwardRay, out RaycastHit hitInfo, rayLength))
            {
                if (hitInfo.collider.TryGetComponent(out T component))
                {
                    return component;
                }
            }

            return default(T);
        }
    }
}