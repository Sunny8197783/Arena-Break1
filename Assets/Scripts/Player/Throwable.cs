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

        public float FuseTime => _fuseTime;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        // 손에 쥐고 있던 시간(cookedTime)만큼 신관이 이미 타 있으므로 남은 시간만 기다린다
        public void Launch(Vector3 velocity, Vector3 spin, float cookedTime = 0f)
        {
            _body.linearVelocity = velocity;
            _body.angularVelocity = spin;
            _detonateTime = Time.time + Mathf.Max(0f, _fuseTime - cookedTime);
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
