using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    // 리깅된 모델이 없으므로 프리미티브 관절을 코드로 회전시켜 걷기·피격·사망을 표현한다
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class EnemyAnimator : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private EnemyPatrol _patrol;
        [SerializeField] private float _stunArmPitch = -150f;
        [SerializeField] private float _stunBlendSpeed = 6f;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _leftLeg;
        [SerializeField] private Transform _rightLeg;
        [SerializeField] private Transform _leftArm;
        [SerializeField] private Transform _rightArm;
        [SerializeField] private float _strideFrequency = 3f;
        [SerializeField] private float _legSwing = 35f;
        [SerializeField] private float _armSwing = 25f;
        [SerializeField] private float _moveThreshold = 0.1f;
        [SerializeField] private float _blendSpeed = 8f;
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
        private float _flinch;
        private float _stunWeight;
        private float _deathProgress;
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
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Revived -= OnRevived;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            bool moving = !_health.IsDead && _agent.velocity.sqrMagnitude > _moveThreshold * _moveThreshold;
            _walkWeight = Mathf.MoveTowards(_walkWeight, moving ? 1f : 0f, _blendSpeed * dt);
            _phase += dt * _strideFrequency * Mathf.PI * 2f * _walkWeight;

            float swing = Mathf.Sin(_phase) * _walkWeight;
            _leftLeg.localRotation = Quaternion.Euler(swing * _legSwing, 0f, 0f);
            _rightLeg.localRotation = Quaternion.Euler(-swing * _legSwing, 0f, 0f);
            // 섬광에 당하면 두 팔로 눈을 가리는 자세
            _stunWeight = Mathf.MoveTowards(_stunWeight, _patrol.IsStunned ? 1f : 0f, _stunBlendSpeed * dt);
            float stunPitch = _stunWeight * _stunArmPitch;
            _leftArm.localRotation = Quaternion.Euler(-swing * _armSwing + stunPitch, 0f, 0f);
            _rightArm.localRotation = Quaternion.Euler(swing * _armSwing + stunPitch, 0f, 0f);

            _flinch = Mathf.Lerp(_flinch, 0f, _flinchRecovery * dt);
            if (_health.IsDead) _deathProgress = Mathf.MoveTowards(_deathProgress, 1f, dt / _deathDuration);

            // 사망 시 발밑을 축으로 뒤로 넘어지게 하려고 시각 루트의 피벗을 발에 둔다
            float fall = Mathf.SmoothStep(0f, _deathFallAngle, _deathProgress);
            _visualRoot.localRotation = Quaternion.Euler(-_flinch * _flinchAngle - fall, 0f, 0f);

            if (_flashing && Time.time >= _flashEndTime) SetFlash(false);
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
