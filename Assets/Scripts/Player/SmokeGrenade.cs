using UnityEngine;

namespace ArenaBreak.Player
{
    public class SmokeGrenade : Throwable
    {
        [SerializeField] private GameObject _smokeCloud;
        [SerializeField] private float _cloudLifetime = 20f;

        protected override void Detonate()
        {
            SpawnEffect(_smokeCloud, _cloudLifetime);
            Destroy(gameObject);
        }
    }
}
