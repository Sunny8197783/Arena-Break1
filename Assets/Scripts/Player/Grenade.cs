using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    public class Grenade : Throwable
    {
        [SerializeField] private float _radius = 6f;
        [SerializeField] private float _maxDamage = 150f;
        [SerializeField] private GameObject _explosionEffect;
        [SerializeField] private float _effectLifetime = 3f;
        [SerializeField] private LayerMask _hitMask = ~0;

        protected override void Detonate()
        {
            Vector3 center = transform.position;
            foreach (Collider hit in Physics.OverlapSphere(center, _radius, _hitMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.TryGetComponent(out IDamageable damageable)) continue;

                // 폭심에서 멀수록 피해가 선형으로 줄어든다
                float distance = Vector3.Distance(center, hit.ClosestPoint(center));
                float falloff = 1f - Mathf.Clamp01(distance / _radius);
                damageable.TakeDamage(_maxDamage * falloff);
            }

            SpawnEffect(_explosionEffect, _effectLifetime);
            Destroy(gameObject);
        }
    }
}
