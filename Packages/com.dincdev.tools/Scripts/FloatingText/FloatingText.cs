using UnityEngine;
using TMPro;

namespace Main._Project.Scripts.Items.FloatingTexts.Base
{
    public abstract class FloatingText : MonoBehaviour
    {
        [SerializeField] protected TextMeshPro textField;
        [SerializeField] protected float upDuration;
        private Vector3 _initialScale;
        private Color _initialColor;

        protected virtual void Awake()
        {
            if (textField == null)
            {
                textField = GetComponent<TextMeshPro>();
            }

            _initialScale = transform.localScale;
            _initialColor = textField.color;
        }

        private void OnEnable()
        {
            transform.localScale = _initialScale;
            textField.color = _initialColor;
        }

        public abstract void SetValuesAndPlay(Vector3 position, string txt);
    }
}
