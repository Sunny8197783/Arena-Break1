using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    public class WeaponSwitcher : MonoBehaviour
    {
        [SerializeField] private WeaponSystem[] _weapons;

        private int _index;

        public WeaponSystem Current => _weapons[_index];
        public int CurrentIndex => _index;
        public IReadOnlyList<WeaponSystem> Weapons => _weapons;

        public event Action<WeaponSystem> WeaponChanged;

        private void Awake()
        {
            for (int i = 0; i < _weapons.Length; i++)
                _weapons[i].gameObject.SetActive(i == _index);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            // Key 열거형에서 Digit1~Digit9가 연속된 값이라 슬롯 번호로 바로 매핑할 수 있다
            for (int i = 0; i < _weapons.Length && i < 9; i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                {
                    Equip(i);
                    return;
                }
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0f) Equip((_index + _weapons.Length - 1) % _weapons.Length);
            else if (scroll < 0f) Equip((_index + 1) % _weapons.Length);
        }

        private void Equip(int index)
        {
            if (index == _index || index >= _weapons.Length) return;

            _weapons[_index].gameObject.SetActive(false);
            _index = index;
            _weapons[_index].gameObject.SetActive(true);
            WeaponChanged?.Invoke(Current);
        }
    }
}
