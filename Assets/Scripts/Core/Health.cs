using System;
using UnityEngine;

namespace ArenaBreak.Core
{
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;

        private float _current;

        public float Current => _current;
        public float Max => _maxHealth;
        public bool IsDead => _current <= 0f;

        public event Action<float> Damaged;
        public event Action Died;
        public event Action Revived;

        private void OnEnable()
        {
            _current = _maxHealth;
        }

        // 표적은 비활성화 없이 리스폰되므로 OnEnable이 다시 불리지 않아 별도 복구 경로가 필요하다
        public void Revive()
        {
            _current = _maxHealth;
            Revived?.Invoke();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            _current = Mathf.Max(0f, _current - amount);
            Damaged?.Invoke(amount);
            if (IsDead) Died?.Invoke();
        }
    }
}
