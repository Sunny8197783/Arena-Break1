using System.Collections;
using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(Health))]
    public class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private CharacterController _controller;
        [SerializeField] private WeaponSwitcher _switcher;
        [SerializeField] private ThrowableSystem _throwables;
        [SerializeField] private Behaviour[] _disableOnDeath;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private float _respawnDelay = 3f;
        [SerializeField] private float _deathCameraDrop = 1.4f;
        [SerializeField] private float _deathCameraRoll = 70f;
        [SerializeField] private float _deathFallTime = 0.6f;

        private Health _health;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Vector3 _pivotRestPosition;

        public float RespawnDelay => _respawnDelay;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _pivotRestPosition = _cameraPivot.localPosition;
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        private void OnDied()
        {
            foreach (Behaviour b in _disableOnDeath)
                b.enabled = false;
            _switcher.SetHolstered(true);
            StartCoroutine(DieAndRespawn());
        }

        private IEnumerator DieAndRespawn()
        {
            // 카메라를 옆으로 쓰러뜨려 사망을 1인칭으로 보여준다
            Vector3 startPos = _cameraPivot.localPosition;
            Quaternion startRot = _cameraPivot.localRotation;
            Quaternion fallenRot = startRot * Quaternion.Euler(0f, 0f, _deathCameraRoll);
            for (float t = 0f; t < 1f; t += Time.deltaTime / _deathFallTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                _cameraPivot.localPosition = startPos + Vector3.down * (_deathCameraDrop * k);
                _cameraPivot.localRotation = Quaternion.Slerp(startRot, fallenRot, k);
                yield return null;
            }

            yield return new WaitForSeconds(_respawnDelay - _deathFallTime);

            // CharacterController가 켜져 있으면 위치 대입이 무시되므로 잠시 끈다
            _controller.enabled = false;
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            _controller.enabled = true;
            _cameraPivot.localPosition = _pivotRestPosition;
            _cameraPivot.localRotation = Quaternion.identity;

            _switcher.RefillAll();
            _throwables.Refill();
            _health.Revive();
            foreach (Behaviour b in _disableOnDeath)
                b.enabled = true;
            _switcher.SetHolstered(false);
        }
    }
}
