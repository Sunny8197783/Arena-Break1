using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    public class AimController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private WeaponSwitcher _switcher;
        [SerializeField] private PlayerController _player;
        [SerializeField] private float _aimSpeed = 8f;
        [SerializeField] private float _scopeThreshold = 0.9f;

        private float _defaultFov;
        private bool _scoped;
        private bool _redDot;

        public bool IsAiming { get; private set; }
        public float AimWeight { get; private set; }

        public event Action<bool> AimChanged;
        public event Action<bool> ScopeChanged;
        public event Action<bool> RedDotChanged;

        private void Awake()
        {
            _defaultFov = _camera.fieldOfView;
        }

        private void OnEnable()
        {
            _switcher.WeaponChanged += OnWeaponChanged;
        }

        private void OnDisable()
        {
            _switcher.WeaponChanged -= OnWeaponChanged;
        }

        private void Update()
        {
            WeaponSystem weapon = _switcher.Current;
            bool pressed = Mouse.current.rightButton.isPressed;
            _player.SprintBlocked = pressed;
            SetAiming(pressed && !weapon.IsReloading);

            AimWeight = Mathf.MoveTowards(AimWeight, IsAiming ? 1f : 0f, _aimSpeed * Time.deltaTime);
            _camera.fieldOfView = Mathf.Lerp(_defaultFov, weapon.Data.AimFov, AimWeight);
            _player.SensitivityMultiplier = Mathf.Lerp(1f, weapon.Data.AimSensitivity, AimWeight);

            // 스코프 화면은 총을 거의 다 올렸을 때 전환해야 확대와 오버레이가 어긋나 보이지 않는다
            bool raised = AimWeight >= _scopeThreshold;
            SetScoped(weapon.Data.ScopeOverlay && raised);
            SetRedDot(weapon.Data.RedDot && raised);
        }

        private void OnWeaponChanged(WeaponSystem weapon)
        {
            AimWeight = 0f;
            SetAiming(false);
            SetScoped(false);
            SetRedDot(false);
        }

        private void SetRedDot(bool on)
        {
            if (on == _redDot) return;
            _redDot = on;
            RedDotChanged?.Invoke(on);
        }

        private void SetAiming(bool aiming)
        {
            if (aiming == IsAiming) return;
            IsAiming = aiming;
            AimChanged?.Invoke(aiming);
        }

        private void SetScoped(bool scoped)
        {
            if (scoped == _scoped) return;
            _scoped = scoped;
            ScopeChanged?.Invoke(scoped);
        }
    }
}
