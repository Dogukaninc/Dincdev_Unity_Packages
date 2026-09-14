using Sirenix.OdinInspector;
using UnityEngine;

namespace _Scripts.Utilities
{
    public class FaceToCamera : MonoBehaviour
    {
        [Header("Freeze Axes")]
        [SerializeField] private bool freeze_x;
        [SerializeField] private bool freeze_y;
        [SerializeField] private bool freeze_z;
        [SerializeField] private bool setAngelOnStartup;
        [ShowIf("setAngelOnStartup"), SerializeField] private bool setOnStartup_X;
        [ShowIf("setAngelOnStartup"), SerializeField] private bool setOnStartup_Y;
        [ShowIf("setAngelOnStartup"), SerializeField] private bool setOnStartup_Z;

        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            if (setAngelOnStartup)
            {
                SetAngel();
            }
        }

        private void SetAngel()
        {
            if (_mainCamera == null)
            {
                Debug.LogWarning("Main camera not found.");
                return;
            }

            Vector3 euler = _mainCamera.transform.rotation.eulerAngles;
            if (setOnStartup_X) euler.x = 0;
            if (setOnStartup_Y) euler.y = 0;
            if (setOnStartup_Z) euler.z = 0;
            transform.rotation = Quaternion.Euler(euler);
        }

        private void Update()
        {
            if (_mainCamera == null)
            {
                return;
            }

            Vector3 euler = _mainCamera.transform.rotation.eulerAngles;
            if (freeze_x) euler.x = 0;
            if (freeze_y) euler.y = 0;
            if (freeze_z) euler.z = 0;
            transform.rotation = Quaternion.Euler(euler);
        }
    }
}