using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace ArenaBreak.Enemy
{
    // 리깅된 모델이 없으므로 프리미티브 관절을 코드로 회전시켜 걷기·공격·피격·기절·사망을 표현한다
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class EnemyAnimator : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [FormerlySerializedAs("_patrol")]
        [SerializeField] private EnemyAI _ai;
        [SerializeField] private float _stunArmPitch = -150f;
        [SerializeField] private float _stunBlendSpeed = 6f;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _leftLeg;
        [SerializeField] private Transform _rightLeg;
        [SerializeField] private Transform _leftArm;
        [SerializeField] private Transform _rightArm;
        [SerializeField] private float _strideFrequency = 3f;
        [SerializeField] private float _referenceSpeed = 2.2f;
        [SerializeField] private float _legSwing = 35f;
        [SerializeField] private float _armSwing = 25f;
        [SerializeField] private float _moveThreshold = 0.1f;
        [SerializeField] private float _blendSpeed = 8f;
        [SerializeField] private float _armForwardPitch = -80f;
        [SerializeField] private float _chaseLean = 12f;
        [SerializeField] private float _attackRaisePitch = -150f;
        [SerializeField] private float _attackStrikePitch = -20f;
        [SerializeField] private float _attackSwingTime = 0.5f;
        [SerializeField] private float _attackRaiseRatio = 0.6f;
        [SerializeField] private float _flinchAngle = 18f;
        [SerializeField] private float _flinchRecovery = 10f;
        [SerializeField] private float _deathDuration = 0.5f;
        [SerializeField] private float _deathFallAngle = 90f;
        [SerializeField] private Color _hitColor = Color.white;
        [SerializeField] private float _flashDuration = 0.06f;

        private NavMeshAgent _agent;
        private Health _health;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private float _phase;
        private float _walkWeight;
        private float _chaseWeight;
        private float _flinch;
        private float _stunWeight;
        private float _deathProgress;
        private float _attackStart = float.MinValue;
        private bool _flashing;
        private float _flashEndTime;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _renderers = _visualRoot.GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
            _block.SetColor(BaseColorId, _hitColor);
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Revived += OnRevived;
            _ai.AttackStarted += OnAttackStarted;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Revived -= OnRevived;
            _ai.AttackStarted -= OnAttackStarted;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float speed = _agent.velocity.magnitude;

            bool moving = !_health.IsDead && speed > _moveThreshold;
            _walkWeight = Mathf.MoveTowards(_walkWeight, moving ? 1f : 0f, _blendSpeed * dt);
            _chaseWeight = Mathf.MoveTowards(_chaseWeight, _ai.IsChasing && !_health.IsDead ? 1f : 0f, _blendSpeed * dt);
            // 빨리 뛸수록 보폭 주기도 빨라지게 해서 미끄러지는 느낌을 줄인다
            _phase += dt * _strideFrequency * Mathf.PI * 2f * _walkWeight * Mathf.Max(1f, speed / _referenceSpeed);

            float swing = Mathf.Sin(_phase) * _walkWeight;
            _leftLeg.localRotation = Quaternion.Euler(swing * _legSwing, 0f, 0f);
            _rightLeg.localRotation = Quaternion.Euler(-swing * _legSwing, 0f, 0f);

            // 좀비 특유의 앞으로 뻗은 팔 + 공격 시 위에서 아래로 내려치기 + 섬광 시 눈 가리기
            float armBase = _armForwardPitch;
            float attackT = (Time.time - _attackStart) / _attackSwingTime;
            if (attackT >= 0f && attackT < 1f)
            {
                armBase = attackT < _attackRaiseRatio
                    ? Mathf.Lerp(_armForwardPitch, _attackRaisePitch, attackT / _attackRaiseRatio)
                    : Mathf.Lerp(_attackRaisePitch, _attackStrikePitch, (attackT - _attackRaiseRatio) / (1f - _attackRaiseRatio));
            }
            _stunWeight = Mathf.MoveTowards(_stunWeight, _ai.IsStunned ? 1f : 0f, _stunBlendSpeed * dt);
            float armPitch = Mathf.Lerp(armBase, _stunArmPitch, _stunWeight);
            float armSwing = swing * _armSwing * (1f - _stunWeight);
            _leftArm.localRotation = Quaternion.Euler(armPitch - armSwing, 0f, 0f);
            _rightArm.localRotation = Quaternion.Euler(armPitch + armSwing, 0f, 0f);

            _flinch = Mathf.Lerp(_flinch, 0f, _flinchRecovery * dt);
            if (_health.IsDead) _deathProgress = Mathf.MoveTowards(_deathProgress, 1f, dt / _deathDuration);

            // 사망 시 발밑을 축으로 뒤로 넘어지게 하려고 시각 루트의 피벗을 발에 둔다
            float fall = Mathf.SmoothStep(0f, _deathFallAngle, _deathProgress);
            float lean = _chaseWeight * _chaseLean;
            _visualRoot.localRotation = Quaternion.Euler(lean - _flinch * _flinchAngle - fall, 0f, 0f);

            if (_flashing && Time.time >= _flashEndTime) SetFlash(false);
        }

        private void OnAttackStarted()
        {
            _attackStart = Time.time;
        }

        private void OnDamaged(float amount)
        {
            _flinch = 1f;
            _flashEndTime = Time.time + _flashDuration;
            SetFlash(true);
        }

        private void OnRevived()
        {
            _deathProgress = 0f;
            _flinch = 0f;
            SetFlash(false);
        }

        private void SetFlash(bool on)
        {
            _flashing = on;
            foreach (Renderer r in _renderers)
                r.SetPropertyBlock(on ? _block : null);
        }
    }
}
