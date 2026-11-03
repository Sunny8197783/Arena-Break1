using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    public class WeaponView : MonoBehaviour
    {
        [SerializeField] private WeaponSystem _weapon;
        [SerializeField] private AimController _aim;
        [SerializeField] private PlayerController _player;
        [SerializeField] private CharacterController _body;
        [SerializeField] private GameObject _muzzleFlash;
        [SerializeField] private ParticleSystem _impactParticles;
        [SerializeField] private int _impactParticleCount = 12;
        [SerializeField] private ParticleSystem _muzzleFlame;
        [SerializeField] private int _muzzleFlameCount = 8;
        [SerializeField] private ParticleSystem _muzzleSmoke;
        [SerializeField] private int _muzzleSmokeCount = 2;
        [SerializeField] private float _flashDuration = 0.04f;
        [SerializeField] private float _kickBack = 0.06f;
        [SerializeField] private float _kickPitch = 5f;
        [SerializeField] private float _kickMax = 1.5f;
        [SerializeField] private float _kickReturn = 14f;
        [SerializeField] private float _swayAmount = 0.002f;
        [SerializeField] private float _swayMax = 0.05f;
        [SerializeField] private float _swaySmooth = 8f;
        [SerializeField] private float _bobFrequency = 10f;
        [SerializeField] private float _bobAmplitude = 0.015f;
        [SerializeField] private float _bobMinSpeed = 0.1f;
        [SerializeField] private float _reloadDip = 0.25f;
        [SerializeField] private float _reloadTilt = 35f;
        [SerializeField] private Vector3 _aimPosition = new Vector3(0f, -0.085f, 0.3f);
        [SerializeField] private float _aimMotionScale = 0.2f;
        [SerializeField] private Vector3 _sprintOffset = new Vector3(-0.05f, -0.06f, -0.05f);
        [SerializeField] private Vector3 _sprintEuler = new Vector3(10f, -35f, 10f);
        [SerializeField] private float _sprintBlendSpeed = 8f;
        [SerializeField] private float _equipTime = 0.35f;
        [SerializeField] private float _equipDrop = 0.3f;
        [SerializeField] private float _equipTilt = 30f;
        [SerializeField] private float _sprintBobMultiplier = 1.5f;

        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Renderer[] _renderers;
        private float _kick;
        private Vector2 _sway;
        private Vector2 _bob;
        private float _bobPhase;
        private float _flashOffTime;
        private float _reloadStart;
        private float _reloadDuration;
        private bool _reloading;
        private float _sprintWeight;
        private float _equipStart;

        private void Awake()
        {
            _restPosition = transform.localPosition;
            _restRotation = transform.localRotation;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _muzzleFlash.SetActive(false);
        }

        private void OnEnable()
        {
            _weapon.Fired += OnFired;
            _weapon.Impact += OnImpact;
            _weapon.ReloadStarted += OnReloadStarted;
            _aim.ScopeChanged += OnScopeChanged;

            _equipStart = Time.time;
            _reloading = false;
            _kick = 0f;
        }

        private void OnDisable()
        {
            _weapon.Fired -= OnFired;
            _weapon.Impact -= OnImpact;
            _weapon.ReloadStarted -= OnReloadStarted;
            _aim.ScopeChanged -= OnScopeChanged;
            SetVisible(true);
            _muzzleFlash.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float aim = _aim.AimWeight;
            // 조준 중에는 흔들림을 줄여야 가늠자가 화면 중앙에서 벗어나지 않는다
            float motionScale = Mathf.Lerp(1f, _aimMotionScale, aim);

            _kick = Mathf.Lerp(_kick, 0f, _kickReturn * dt);

            // 시점 이동 반대 방향으로 총이 살짝 뒤처지게 해서 무게감을 준다
            Vector2 swayTarget = Vector2.ClampMagnitude(-Mouse.current.delta.ReadValue() * _swayAmount, _swayMax);
            _sway = Vector2.Lerp(_sway, swayTarget, _swaySmooth * dt);

            Vector3 velocity = _body.velocity;
            velocity.y = 0f;
            Vector2 bobTarget = Vector2.zero;
            if (_body.isGrounded && velocity.magnitude > _bobMinSpeed)
            {
                _bobPhase += dt * _bobFrequency * (_player.IsSprinting ? _sprintBobMultiplier : 1f);
                bobTarget = new Vector2(Mathf.Sin(_bobPhase), -Mathf.Abs(Mathf.Cos(_bobPhase))) * _bobAmplitude;
            }
            _bob = Vector2.Lerp(_bob, bobTarget, _swaySmooth * dt);

            float reloadWeight = 0f;
            if (_reloading)
            {
                float t = (Time.time - _reloadStart) / _reloadDuration;
                if (t >= 1f) _reloading = false;
                else reloadWeight = Mathf.Sin(t * Mathf.PI);
            }

            _sprintWeight = Mathf.MoveTowards(_sprintWeight, _player.IsSprinting ? 1f : 0f, _sprintBlendSpeed * dt);
            float equip = 1f - Mathf.Clamp01((Time.time - _equipStart) / _equipTime);

            Vector3 basePosition = Vector3.Lerp(_restPosition, _aimPosition, aim);
            transform.localPosition = basePosition
                + new Vector3(_sway.x + _bob.x, _sway.y + _bob.y, 0f) * motionScale
                + new Vector3(0f, -reloadWeight * _reloadDip - equip * equip * _equipDrop, -_kick * _kickBack * motionScale)
                + _sprintOffset * _sprintWeight;
            transform.localRotation = _restRotation
                * Quaternion.Euler(_sprintEuler * _sprintWeight)
                * Quaternion.Euler(-_kick * _kickPitch * motionScale + reloadWeight * _reloadTilt * 0.5f + equip * _equipTilt,
                    0f, reloadWeight * _reloadTilt);

            if (_muzzleFlash.activeSelf && Time.time >= _flashOffTime) _muzzleFlash.SetActive(false);
        }

        private void OnFired(bool hit)
        {
            _kick = Mathf.Min(_kick + 1f, _kickMax);
            _muzzleFlash.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            _muzzleFlash.SetActive(true);
            _flashOffTime = Time.time + _flashDuration;
            _muzzleFlame.Emit(_muzzleFlameCount);
            _muzzleSmoke.Emit(_muzzleSmokeCount);
        }

        private void OnImpact(Vector3 point, Vector3 normal)
        {
            // 파티클 하나를 옮겨가며 Emit해서 탄착마다 오브젝트를 생성하지 않는다 (풀링 불필요)
            _impactParticles.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
            _impactParticles.Emit(_impactParticleCount);
        }

        private void OnReloadStarted(float duration)
        {
            _reloading = true;
            _reloadStart = Time.time;
            _reloadDuration = duration;
        }

        // 스코프 오버레이가 켜지면 총 모델이 화면을 가리지 않도록 숨긴다
        private void OnScopeChanged(bool scoped)
        {
            SetVisible(!scoped);
        }

        private void SetVisible(bool visible)
        {
            foreach (Renderer r in _renderers)
                r.enabled = visible;
        }
    }
}
