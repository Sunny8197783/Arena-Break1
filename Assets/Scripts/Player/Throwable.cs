using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class Throwable : MonoBehaviour
    {
        [SerializeField] private float _fuseTime = 2.5f;

        private Rigidbody _body;
        private float _detonateTime = float.MaxValue;
        private bool _detonated;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        public void Launch(Vector3 velocity, Vector3 spin)
        {
            _body.linearVelocity = velocity;
            _body.angularVelocity = spin;
            _detonateTime = Time.time + _fuseTime;
        }

        private void Update()
        {
            if (_detonated || Time.time < _detonateTime) return;

            _detonated = true;
            Detonate();
        }

        protected abstract void Detonate();

        // 투척물은 몇 초에 한 번 생성되는 수준이라 풀링 대신 Instantiate/Destroy로 처리한다
        protected void SpawnEffect(GameObject effectPrefab, float lifetime)
        {
            GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, lifetime);
        }
    }
}
