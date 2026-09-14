using DG.Tweening;
using Main._Project.Scripts.Items.FloatingTexts.Base;
using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;

namespace Main._Project.Scripts.Items.FloatingTexts
{
    public class DamageText : FloatingText
    {
        [SerializeField] private Color defaultColor;
        [SerializeField] private float scaleToReach;
        [SerializeField] private float critYPositionIncreaser;
        [SerializeField] private float yOffset;
        [SerializeField] private float posOffset;
        [SerializeField] private bool renderInFront;

        private TextMeshPro _tmpText;
        private Camera _camera;

        protected override void Awake()
        {
            base.Awake();
            _tmpText = textField;
            _camera = Camera.main;
        }

        private void Start()
        {
            if (renderInFront)
            {
                _tmpText.fontMaterial.renderQueue = 4000;
            }
        }

        public void SetText(Vector3 position, string text, bool isCrit = false, bool isRandomizePos = false, bool isHeal = false)
        {
            textField.color = defaultColor;
            var offsetPos = position + (Vector3.up * posOffset);
            var randomPos = position + Random.insideUnitSphere * posOffset;
            position = isRandomizePos ? randomPos : offsetPos;
            position = isCrit ? position + new Vector3(0f, critYPositionIncreaser, 0f) : position;
            SetValuesAndPlay(position, text);
        }

        public override void SetValuesAndPlay(Vector3 position, string txt)
        {
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(_camera.transform.forward);
            textField.text = txt;
            transform.localScale = Vector3.zero;
            var seq = DOTween.Sequence();
            seq.Append(transform.DOScale(Vector3.one * scaleToReach, upDuration).SetLoops(2, LoopType.Yoyo));
            seq.Join(transform.DOMoveY(transform.position.y + yOffset, upDuration * 2));
            seq.OnComplete(() => gameObject.SetActive(false));
        }
    }
}