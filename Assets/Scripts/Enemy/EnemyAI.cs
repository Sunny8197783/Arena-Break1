using System;
using System.Collections;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    // 좀비: 순찰하다 플레이어를 보거나 맞으면 추격하고, 붙으면 근접 공격한다
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class EnemyAI : MonoBehaviour, IStunnable
    {
        private enum State
        {
            Patrol,
            Chase,
            Attack
        }

        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _arriveDistance = 0.5f;
        [SerializeField] private float _waitAtPoint = 1.5f;
        [SerializeField] private float _respawnDelay = 3f;
        [SerializeField] private Health _target;
        [SerializeField] private float _patrolSpeed = 1.2f;
        [SerializeField] private float _chaseSpeed = 4f;
        [SerializeField] private float _detectRange = 18f;
        [SerializeField] private float _loseRange = 35f;
        [SerializeField] private float _aggroMemory = 10f;
        [SerializeField] private float _senseInterval = 0.25f;
        [SerializeField] private float _eyeHeight = 1.6f;
        [SerializeField] private float _targetAimHeight = 0.5f;
        [SerializeField] private LayerMask _sightMask = ~0;
        [SerializeField] private float _repathInterval = 0.2f;
        [SerializeField] private float _attackRange = 1.6f;
        [SerializeField] private float _attackHitRange = 2.2f;
        [SerializeField] private float _attackWindup = 0.45f;
        [SerializeField] private float _attackCooldown = 1.2f;
        [SerializeField] private float _attackDamage = 15f;
        [SerializeField] private float _turnSpeed = 8f;

        private NavMeshAgent _agent;
        private Health _health;
        private Collider _collider;
        private State _state;
        private int _index;
        private bool _waiting;
        private float _waitEndTime;
        private float _stunEndTime;
        private float _nextSenseTime;
        private float _nextRepathTime;
        private float _attackHitTime;
        private float _nextAttackTime;
        private bool _attackPending;
        private float _aggroEndTime;

        public bool IsStunned { get; private set; }
        public bool IsChasing => _state != State.Patrol;

        public event Action AttackStarted;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _collider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
            _health.Damaged -= OnDamaged;
        }

        private void Start()
        {
            BeginPatrol();
        }

        public void Stun(float duration)
        {
            if (_health.IsDead) return;
            IsStunned = true;
            _attackPending = false;
            _stunEndTime = Mathf.Max(_stunEndTime, Time.time + duration);
            _agent.isStopped = true;
        }

        private void Update()
        {
            if (IsStunned)
            {
                if (Time.time < _stunEndTime) return;
                IsStunned = false;
                if (!_health.IsDead) _agent.isStopped = _state == State.Attack;
            }

            if (_health.IsDead) return;

            switch (_state)
            {
                case State.Patrol:
                    UpdatePatrol();
                    break;
                case State.Chase:
                    UpdateChase();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (Time.time >= _nextSenseTime)
            {
                _nextSenseTime = Time.time + _senseInterval;
                if (CanSeeTarget())
                {
                    BeginChase();
                    return;
                }
            }

            if (_agent.pathPending || _agent.remainingDistance > _arriveDistance) return;

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

        private void UpdateChase()
        {
            float distance = DistanceToTarget();
            if (_target.IsDead || (distance > _loseRange && Time.time >= _aggroEndTime))
            {
                BeginPatrol();
                return;
            }

            if (distance <= _attackRange && Time.time >= _nextAttackTime)
            {
                BeginAttack();
                return;
            }

            if (Time.time < _nextRepathTime) return;
            _nextRepathTime = Time.time + _repathInterval;
            _agent.SetDestination(_target.transform.position);
        }

        private void UpdateAttack()
        {
            FaceTarget();

            if (_attackPending && Time.time >= _attackHitTime)
            {
                _attackPending = false;
                // 휘두르는 동안 피했다면 빗나간다
                if (!_target.IsDead && DistanceToTarget() <= _attackHitRange)
                    _target.TakeDamage(_attackDamage);
            }

            if (_attackPending || Time.time < _nextAttackTime) return;

            if (_target.IsDead) BeginPatrol();
            else if (DistanceToTarget() <= _attackRange) BeginAttack();
            else BeginChase();
        }

        private void BeginPatrol()
        {
            _state = State.Patrol;
            _attackPending = false;
            _agent.speed = _patrolSpeed;
            _agent.isStopped = IsStunned;
            GoTo(_index);
        }

        private void BeginChase()
        {
            _state = State.Chase;
            _agent.speed = _chaseSpeed;
            _agent.isStopped = IsStunned;
            _nextRepathTime = 0f;
        }

        private void BeginAttack()
        {
            _state = State.Attack;
            _agent.isStopped = true;
            _agent.ResetPath();
            _attackPending = true;
            _attackHitTime = Time.time + _attackWindup;
            _nextAttackTime = Time.time + _attackCooldown;
            AttackStarted?.Invoke();
        }

        private void GoTo(int index)
        {
            _index = index;
            _waiting = false;
            _agent.SetDestination(_waypoints[_index].position);
        }

        private bool CanSeeTarget()
        {
            if (_target.IsDead || DistanceToTarget() > _detectRange) return false;

            Vector3 eye = transform.position + Vector3.up * _eyeHeight;
            Vector3 targetPoint = _target.transform.position + Vector3.up * _targetAimHeight;
            if (!Physics.Linecast(eye, targetPoint, out RaycastHit hit, _sightMask, QueryTriggerInteraction.Ignore))
                return true;
            return hit.transform == _target.transform;
        }

        private float DistanceToTarget()
        {
            return Vector3.Distance(transform.position, _target.transform.position);
        }

        private void FaceTarget()
        {
            Vector3 direction = _target.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                _turnSpeed * Time.deltaTime);
        }

        private void OnDamaged(float amount)
        {
            // 멀리서 저격당해도 쏜 쪽으로 몰려오도록 피격 시 바로 추격
            if (_health.IsDead || _target.IsDead) return;
            _aggroEndTime = Time.time + _aggroMemory;
            if (_state == State.Patrol) BeginChase();
        }

        private void OnDied()
        {
            _attackPending = false;
            _agent.isStopped = true;
            _collider.enabled = false;
            StartCoroutine(Respawn());
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(_respawnDelay);

            // 쓰러진 자리에서 바로 일어나면 어색하므로 다른 순찰 지점에서 다시 나타나게 한다
            int spawnIndex = UnityEngine.Random.Range(0, _waypoints.Length);
            _agent.Warp(_waypoints[spawnIndex].position);
            IsStunned = false;
            _collider.enabled = true;
            _health.Revive();
            _index = (spawnIndex + 1) % _waypoints.Length;
            BeginPatrol();
        }
    }
}
