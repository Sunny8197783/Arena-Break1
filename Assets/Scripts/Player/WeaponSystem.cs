using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    public class WeaponSystem : MonoBehaviour
    {
        [SerializeField] private WeaponData _data;
        [SerializeField] private Camera _camera;
        [SerializeField] private PlayerController _player;
        [SerializeField] private AimController _aim;
        [SerializeField] private LayerMask _hitMask = ~0;

        private float _nextFireTime;
        private float _reloadEndTime;
        private int _ammo;
        private int _reserve;

        public WeaponData Data => _data;
        public int Ammo => _ammo;
        public int Reserve => _reserve;
        public bool IsReloading { get; private set; }

        public event Action<bool> Fired;
        public event Action<Vector3, Vector3> Impact;
        public event Action<int, int> AmmoChanged;
        public event Action<float> ReloadStarted;

        private void Awake()
        {
            _ammo = _data.MagazineSize;
            _reserve = _data.StartReserve;
        }

        public void Refill()
        {
            _ammo = _data.MagazineSize;
            _reserve = _data.StartReserve;
            IsReloading = false;
            AmmoChanged?.Invoke(_ammo, _reserve);
        }

        private void OnDisable()
        {
            // 무기를 바꾸면 장전이 취소된다. 탄은 FinishReload에서만 옮기므로 손실 없음
            IsReloading = false;
        }

        private void Update()
        {
            if (IsReloading)
            {
                if (Time.time >= _reloadEndTime) FinishReload();
                return;
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                TryReload();
                return;
            }

            bool trigger = _data.Automatic
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.leftButton.wasPressedThisFrame;
            if (!trigger || Time.time < _nextFireTime) return;

            if (_ammo <= 0)
            {
                TryReload();
                return;
            }

            _nextFireTime = Time.time + _data.FireInterval;
            Fire();
        }

        private void Fire()
        {
            _ammo--;
            AmmoChanged?.Invoke(_ammo, _reserve);
            _player.AddRecoil(_data.RecoilDegrees);

            float spread = _aim.IsAiming ? _data.AimSpread : _data.HipSpread;
            bool hitDamageable = false;
            // 샷건은 한 번 쏠 때 여러 발의 산탄을 각각 판정한다
            for (int i = 0; i < _data.Pellets; i++)
                hitDamageable |= FirePellet(spread);

            Fired?.Invoke(hitDamageable);
        }

        private bool FirePellet(float spread)
        {
            Transform camTransform = _camera.transform;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spread;
            Vector3 direction = camTransform.rotation * Quaternion.Euler(offset.y, offset.x, 0f) * Vector3.forward;

            if (!Physics.Raycast(camTransform.position, direction, out RaycastHit hit, _data.Range, _hitMask,
                    QueryTriggerInteraction.Ignore))
                return false;

            Impact?.Invoke(hit.point, hit.normal);
            if (!hit.collider.TryGetComponent(out IDamageable damageable)) return false;

            damageable.TakeDamage(_data.Damage);
            return true;
        }

        private void TryReload()
        {
            if (_ammo >= _data.MagazineSize || _reserve <= 0) return;

            IsReloading = true;
            _reloadEndTime = Time.time + _data.ReloadTime;
            ReloadStarted?.Invoke(_data.ReloadTime);
        }

        private void FinishReload()
        {
            int loaded = Mathf.Min(_data.MagazineSize - _ammo, _reserve);
            _ammo += loaded;
            _reserve -= loaded;
            IsReloading = false;
            AmmoChanged?.Invoke(_ammo, _reserve);
        }
    }
}
