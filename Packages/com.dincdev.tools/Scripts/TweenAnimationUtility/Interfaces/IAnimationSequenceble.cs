using UnityEngine;

namespace _Main.Project.Scripts.Utils.Interfaces
{
    public interface IAnimationSequenceble
    {
        public Transform[] RendererTransforms { get; set; }
        public void PlayEntranceAnimation();
    }
}