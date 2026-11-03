using System.Collections;
using ArenaBreak.Core;
using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private WeaponSwitcher _switcher;
        [SerializeField] private AimController _aim;
        [SerializeField] private GameObject _crosshair;
        [SerializeField] private GameObject _hitMarker;
        [SerializeField] private GameObject _scopeOverlay;
        [SerializeField] private Text _ammoText;
        [SerializeField] private Text _weaponNameText;
        [SerializeField] private float _hitMarkerDuration = 0.1f;
        [SerializeField] private string _reloadingLabel = "RELOADING";
        [SerializeField] private ThrowableSystem _throwables;
        [SerializeField] private GameObject _redDot;
        [SerializeField] private Text[] _slotLabels;
        [SerializeField] private Text[] _throwableLabels;
        [SerializeField] private Color _slotActiveColor = new Color(1f, 0.8f, 0.35f);
        [SerializeField] private Color _slotIdleColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private Health _playerHealth;
        [SerializeField] private PlayerDeath _playerDeath;
        [SerializeField] private Image _healthFill;
        [SerializeField] private Text _healthText;
        [SerializeField] private Image _damageFlash;
        [SerializeField] private float _damageFlashAlpha = 0.45f;
        [SerializeField] private float _damageFlashFade = 0.4f;
        [SerializeField] private GameObject _deathScreen;
        [SerializeField] private Text _respawnText;
        [SerializeField] private GameObject _cookPanel;
        [SerializeField] private Text _cookText;
        [SerializeField] private float _countdownTick = 0.05f;

        private WeaponSystem _weapon;
        private Coroutine _flash;
        private Coroutine _damageRoutine;
        private Coroutine _cookRoutine;

        private void OnEnable()
        {
            _switcher.WeaponChanged += Bind;
            _aim.AimChanged += OnAimChanged;
            _aim.ScopeChanged += OnScopeChanged;
            _aim.RedDotChanged += OnRedDotChanged;
            _throwables.CountChanged += OnThrowableCountChanged;
            _throwables.HoldStarted += OnHoldStarted;
            _throwables.ThrowStarted += OnCookEnded;
            _throwables.Finished += OnCookEnded;
            _playerHealth.Damaged += OnPlayerDamaged;
            _playerHealth.Died += OnPlayerDied;
            _playerHealth.Revived += OnPlayerRevived;
            Bind(_switcher.Current);
        }

        private void OnDisable()
        {
            _switcher.WeaponChanged -= Bind;
            _aim.AimChanged -= OnAimChanged;
            _aim.ScopeChanged -= OnScopeChanged;
            _aim.RedDotChanged -= OnRedDotChanged;
            _throwables.CountChanged -= OnThrowableCountChanged;
            _throwables.HoldStarted -= OnHoldStarted;
            _throwables.ThrowStarted -= OnCookEnded;
            _throwables.Finished -= OnCookEnded;
            _playerHealth.Damaged -= OnPlayerDamaged;
            _playerHealth.Died -= OnPlayerDied;
            _playerHealth.Revived -= OnPlayerRevived;
            Unbind();
        }

        private void Start()
        {
            // WeaponSystem.Awake에서 탄약이 채워진 뒤의 값을 보여주기 위해 Start에서 한 번 더 갱신
            Refresh();
            _hitMarker.SetActive(false);
            _scopeOverlay.SetActive(false);
            _redDot.SetActive(false);

            for (int i = 0; i < _slotLabels.Length; i++)
            {
                bool used = i < _switcher.Weapons.Count;
                _slotLabels[i].gameObject.SetActive(used);
                if (used) _slotLabels[i].text = $"{i + 1}  {_switcher.Weapons[i].Data.DisplayName}";
            }
            HighlightSlot();

            for (int i = 0; i < _throwables.Slots.Count; i++)
                OnThrowableCountChanged(i, _throwables.Slots[i].Count);

            UpdateHealth();
            _deathScreen.SetActive(false);
            _cookPanel.SetActive(false);
            SetDamageAlpha(0f);
        }

        private void UpdateHealth()
        {
            _healthFill.fillAmount = _playerHealth.Current / _playerHealth.Max;
            _healthText.text = Mathf.CeilToInt(_playerHealth.Current).ToString();
        }

        private void OnPlayerDamaged(float amount)
        {
            UpdateHealth();
            if (_damageRoutine != null) StopCoroutine(_damageRoutine);
            _damageRoutine = StartCoroutine(DamageFlash());
        }

        private IEnumerator DamageFlash()
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / _damageFlashFade)
            {
                SetDamageAlpha(_damageFlashAlpha * (1f - t));
                yield return null;
            }
            SetDamageAlpha(0f);
            _damageRoutine = null;
        }

        private void SetDamageAlpha(float alpha)
        {
            Color c = _damageFlash.color;
            c.a = alpha;
            _damageFlash.color = c;
        }

        private void OnPlayerDied()
        {
            _deathScreen.SetActive(true);
            OnCookEnded();
            StartCoroutine(RespawnCountdown());
        }

        private IEnumerator RespawnCountdown()
        {
            float end = Time.time + _playerDeath.RespawnDelay;
            while (Time.time < end)
            {
                _respawnText.text = $"{end - Time.time:0.0}초 후 부활";
                yield return new WaitForSeconds(_countdownTick);
            }
        }

        private void OnPlayerRevived()
        {
            _deathScreen.SetActive(false);
            UpdateHealth();
        }

        private void OnHoldStarted(int slot, float fuse)
        {
            if (_cookRoutine != null) StopCoroutine(_cookRoutine);
            _cookRoutine = StartCoroutine(CookCountdown(_throwables.Slots[slot].Label, Time.time + fuse));
        }

        // 핀을 뽑은 뒤 남은 신관 시간을 보여줘서 원하는 타이밍에 던질 수 있게 한다
        private IEnumerator CookCountdown(string label, float detonateTime)
        {
            _cookPanel.SetActive(true);
            while (Time.time < detonateTime)
            {
                _cookText.text = $"{label}  {detonateTime - Time.time:0.0}s";
                yield return new WaitForSeconds(_countdownTick);
            }
            _cookPanel.SetActive(false);
            _cookRoutine = null;
        }

        private void OnCookEnded()
        {
            if (_cookRoutine != null) StopCoroutine(_cookRoutine);
            _cookRoutine = null;
            _cookPanel.SetActive(false);
        }

        private void Bind(WeaponSystem weapon)
        {
            Unbind();
            _weapon = weapon;
            _weapon.Fired += OnFired;
            _weapon.AmmoChanged += OnAmmoChanged;
            _weapon.ReloadStarted += OnReloadStarted;
            Refresh();
            HighlightSlot();
        }

        private void HighlightSlot()
        {
            for (int i = 0; i < _slotLabels.Length; i++)
                _slotLabels[i].color = i == _switcher.CurrentIndex ? _slotActiveColor : _slotIdleColor;
        }

        private void OnThrowableCountChanged(int index, int count)
        {
            ThrowableSystem.Slot slot = _throwables.Slots[index];
            _throwableLabels[index].text = $"[{slot.Key}] {slot.Label}  x{count}";
            _throwableLabels[index].color = count > 0 ? _slotActiveColor : _slotIdleColor;
        }

        private void OnRedDotChanged(bool on)
        {
            _redDot.SetActive(on);
        }

        private void Unbind()
        {
            if (_weapon == null) return;
            _weapon.Fired -= OnFired;
            _weapon.AmmoChanged -= OnAmmoChanged;
            _weapon.ReloadStarted -= OnReloadStarted;
            _weapon = null;
        }

        private void Refresh()
        {
            _weaponNameText.text = _weapon.Data.DisplayName;
            OnAmmoChanged(_weapon.Ammo, _weapon.Reserve);
        }

        private void OnAmmoChanged(int ammo, int reserve)
        {
            _ammoText.text = $"{ammo} / {reserve}";
        }

        private void OnReloadStarted(float duration)
        {
            _ammoText.text = _reloadingLabel;
        }

        private void OnAimChanged(bool aiming)
        {
            _crosshair.SetActive(!aiming);
        }

        private void OnScopeChanged(bool scoped)
        {
            _scopeOverlay.SetActive(scoped);
        }

        private void OnFired(bool hit)
        {
            if (!hit) return;
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(HitMarker());
        }

        private IEnumerator HitMarker()
        {
            _hitMarker.SetActive(true);
            yield return new WaitForSeconds(_hitMarkerDuration);
            _hitMarker.SetActive(false);
            _flash = null;
        }
    }
}
