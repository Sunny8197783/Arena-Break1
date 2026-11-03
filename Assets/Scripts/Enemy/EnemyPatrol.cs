using System.Collections;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class EnemyPatrol : MonoBehaviour, IStunnable
    {
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _arriveDistance = 0.5f;
        [SerializeField] private float _waitAtPoint = 1.5f;
        [SerializeField] private float _respawnDelay = 3f;

        private NavMeshAgent _agent;
        private Health _health;
        private Collider _collider;
        private int _index;
        private bool _waiting;
        private float _waitEndTime;
        private float _stunEndTime;

        public bool IsStunned { get; private set; }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _collider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        private void Start()
        {
            GoTo(_index);
        }

        public void Stun(float duration)
        {
            if (_health.IsDead) return;
            IsStunned = true;
            _stunEndTime = Mathf.Max(_stunEndTime, Time.time + duration);
            _agent.isStopped = true;
        }

        private void Update()
        {
            if (IsStunned)
            {
                if (Time.time < _stunEndTime) return;
                IsStunned = false;
                if (!_health.IsDead) _agent.isStopped = false;
            }

            if (_health.IsDead || _agent.pathPending || _agent.remainingDistance > _arriveDistance) return;

            if (!_waiting)
            {
                _waiting = true;
                _waitEndTime = Time.time + _waitAtPoint;
            }
            else if (Time.time >= _waitEndTime)
            {
                GoTo((_index + 1) % _waypoints.Length);
            }
        }

        private void GoTo(int index)
        {
            _index = index;
            _waiting = false;
            _agent.SetDestination(_waypoints[_index].position);
        }

        private void OnDied()
        {
            _agent.isStopped = true;
            _collider.enabled = false;
            StartCoroutine(Respawn());
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(_respawnDelay);

            // 쓰러진 자리에서 바로 일어나면 어색하므로 다른 순찰 지점에서 다시 나타나게 한다
            int spawnIndex = Random.Range(0, _waypoints.Length);
            _agent.Warp(_waypoints[spawnIndex].position);
            IsStunned = false;
            _agent.isStopped = false;
            _collider.enabled = true;
            _health.Revive();
            GoTo((spawnIndex + 1) % _waypoints.Length);
        }
    }
}
