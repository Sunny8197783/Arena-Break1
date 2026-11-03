using System.Collections;
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

        private WeaponSystem _weapon;
        private Coroutine _flash;

        private void OnEnable()
        {
            _switcher.WeaponChanged += Bind;
            _aim.AimChanged += OnAimChanged;
            _aim.ScopeChanged += OnScopeChanged;
            _aim.RedDotChanged += OnRedDotChanged;
            _throwables.CountChanged += OnThrowableCountChanged;
            Bind(_switcher.Current);
        }

        private void OnDisable()
        {
            _switcher.WeaponChanged -= Bind;
            _aim.AimChanged -= OnAimChanged;
            _aim.ScopeChanged -= OnScopeChanged;
            _aim.RedDotChanged -= OnRedDotChanged;
            _throwables.CountChanged -= OnThrowableCountChanged;
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
