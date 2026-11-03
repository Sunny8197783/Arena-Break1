using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    public class Flashbang : Throwable
    {
        [SerializeField] private float _radius = 12f;
        [SerializeField] private float _stunDuration = 3f;
        [SerializeField] private GameObject _flashEffect;
        [SerializeField] private float _effectLifetime = 1f;
        [SerializeField] private LayerMask _hitMask = ~0;

        protected override void Detonate()
        {
            foreach (Collider hit in Physics.OverlapSphere(transform.position, _radius, _hitMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.TryGetComponent(out IStunnable stunnable))
                    stunnable.Stun(_stunDuration);
            }

            SpawnEffect(_flashEffect, _effectLifetime);
            Destroy(gameObject);
        }
    }
}
