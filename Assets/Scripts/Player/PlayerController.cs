using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _sprintMultiplier = 1.6f;
        [SerializeField] private float _crouchMultiplier = 0.5f;
        [SerializeField] private float _jumpHeight = 1.2f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _lookSensitivity = 0.1f;
        [SerializeField] private float _maxPitch = 85f;
        [SerializeField] private float _recoilRecovery = 12f;
        [SerializeField] private float _standHeight = 2f;
        [SerializeField] private float _crouchHeight = 1.2f;
        [SerializeField] private float _crouchSpeed = 10f;

        private CharacterController _controller;
        private float _pitch;
        private float _recoil;
        private float _verticalVelocity;
        private float _standEyeHeight;
        private float _currentHeight;

        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsGrounded => _controller.isGrounded;
        public float CrouchAmount => Mathf.InverseLerp(_standHeight, _crouchHeight, _currentHeight);
        public bool SprintBlocked { get; set; }
        public float SensitivityMultiplier { get; set; } = 1f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _standEyeHeight = _cameraPivot.localPosition.y;
            _currentHeight = _standHeight;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            Look();
            UpdateCrouch();
            Move();
        }

        // 반동은 시점에 더해지는 값으로 두어, 마우스를 내리지 않아도 자연스럽게 원위치로 복구되게 한다
        public void AddRecoil(float pitchDegrees)
        {
            _recoil += pitchDegrees;
        }

        private void Look()
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * (_lookSensitivity * SensitivityMultiplier);
            transform.Rotate(0f, delta.x, 0f);

            _pitch = Mathf.Clamp(_pitch - delta.y, -_maxPitch, _maxPitch);
            _recoil = Mathf.Lerp(_recoil, 0f, _recoilRecovery * Time.deltaTime);
            _cameraPivot.localRotation = Quaternion.Euler(_pitch - _recoil, 0f, 0f);
        }

        private void UpdateCrouch()
        {
            IsCrouching = Keyboard.current.leftCtrlKey.isPressed;
            float target = IsCrouching ? _crouchHeight : _standHeight;
            _currentHeight = Mathf.MoveTowards(_currentHeight, target, _crouchSpeed * Time.deltaTime);

            // 발 위치를 고정한 채 키만 줄이려면 중심을 높이 변화의 절반만큼 내려야 한다
            _controller.height = _currentHeight;
            _controller.center = new Vector3(0f, (_currentHeight - _standHeight) * 0.5f, 0f);
            Vector3 pivot = _cameraPivot.localPosition;
            pivot.y = _standEyeHeight - (_standHeight - _currentHeight);
            _cameraPivot.localPosition = pivot;
        }

        private void Move()
        {
            Keyboard keyboard = Keyboard.current;
            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);

            IsSprinting = keyboard.leftShiftKey.isPressed && z > 0f && !IsCrouching && !SprintBlocked;
            float speed = _moveSpeed;
            if (IsSprinting) speed *= _sprintMultiplier;
            else if (IsCrouching) speed *= _crouchMultiplier;

            Vector3 horizontal = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f) * speed;

            if (_controller.isGrounded)
            {
                _verticalVelocity = -1f;
                if (keyboard.spaceKey.wasPressedThisFrame && !IsCrouching)
                    _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
            _verticalVelocity += _gravity * Time.deltaTime;

            _controller.Move((horizontal + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }
    }
}
