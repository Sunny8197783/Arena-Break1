using UnityEngine;

namespace ArenaBreak.Player
{
    [CreateAssetMenu(menuName = "ArenaBreak/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [SerializeField] private string _displayName = "Rifle";
        [SerializeField] private float _damage = 25f;
        [SerializeField] private float _range = 150f;
        [SerializeField] private float _fireInterval = 0.12f;
        [SerializeField] private bool _automatic = true;
        [SerializeField] private float _recoilDegrees = 1.2f;
        [SerializeField] private float _hipSpread = 1.5f;
        [SerializeField] private float _aimSpread = 0.2f;
        [SerializeField] private int _magazineSize = 30;
        [SerializeField] private int _startReserve = 120;
        [SerializeField] private float _reloadTime = 1.6f;
        [SerializeField] private float _aimFov = 50f;
        [SerializeField] private float _aimSensitivity = 0.7f;
        [SerializeField] private bool _scopeOverlay;
        [SerializeField] private bool _redDot;
        [SerializeField] private int _pellets = 1;

        public string DisplayName => _displayName;
        public float Damage => _damage;
        public float Range => _range;
        public float FireInterval => _fireInterval;
        public bool Automatic => _automatic;
        public float RecoilDegrees => _recoilDegrees;
        public float HipSpread => _hipSpread;
        public float AimSpread => _aimSpread;
        public int MagazineSize => _magazineSize;
        public int StartReserve => _startReserve;
        public float ReloadTime => _reloadTime;
        public float AimFov => _aimFov;
        public float AimSensitivity => _aimSensitivity;
        public bool ScopeOverlay => _scopeOverlay;
        public bool RedDot => _redDot;
        public int Pellets => _pellets;
    }
}
