using UnityEngine;

namespace _main.Scriptable_Objects
{
    [CreateAssetMenu(fileName = "so_color_settings", menuName = "Scriptable Objects/Color Settings", order = 0)]
    public class ColorSettings : ScriptableObject
    {
        public Color colorBlue = new(0.15f, 0.35f, 0.90f);
        public Color colorRed = new(0.90f, 0.15f, 0.15f);
        public Color colorBlueDarker = new(0.15f, 0.35f, 1f);
        public Color colorRedDarker = new(1f, 0f, 0.15f);
        public Color colorNeutral = new(0.28f, 0.28f, 0.33f);
        public Color colorWhite = new(1f, 1f, 1f);
        public Color colorPreviewOk = new(0.10f, 0.90f, 0.30f);
        public Color colorPreviewBad = new(0.90f, 0.20f, 0.10f);

        public Texture2D playerTeamTurretSprite;
        public Texture2D botTeamTurretSprite;
    }
}