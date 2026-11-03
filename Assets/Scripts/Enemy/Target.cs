using System.Collections;
using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    [RequireComponent(typeof(Health))]
    public class Target : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _hitColor = Color.white;
        [SerializeField] private float _flashDuration = 0.08f;
        [SerializeField] private float _punchScale = 1.15f;
        [SerializeField] private float _respawnDelay = 2f;

        private Health _health;
        private MaterialPropertyBlock _block;
        private Color _baseColor;
        private Vector3 _baseScale;
        private Collider _collider;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _collider = GetComponent<Collider>();
            _block = new MaterialPropertyBlock();
            _baseColor = _renderer.sharedMaterial.GetColor(BaseColorId);
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void OnDamaged(float amount)
        {
            if (_health.IsDead) return;
            StopAllCoroutines();
            StartCoroutine(HitFeedback());
        }

        private void OnDied()
        {
            StopAllCoroutines();
            StartCoroutine(DieAndRespawn());
        }

        private IEnumerator HitFeedback()
        {
            SetColor(_hitColor);
            transform.localScale = _baseScale * _punchScale;
            yield return new WaitForSeconds(_flashDuration);
            SetColor(_baseColor);
            transform.localScale = _baseScale;
        }

        private IEnumerator DieAndRespawn()
        {
            // 오브젝트를 비활성화하면 코루틴이 멈추므로 렌더러/콜라이더만 끈다
            SetColor(_hitColor);
            yield return new WaitForSeconds(_flashDuration);
            transform.localScale = _baseScale;
            _renderer.enabled = false;
            _collider.enabled = false;

            yield return new WaitForSeconds(_respawnDelay);

            SetColor(_baseColor);
            _renderer.enabled = true;
            _collider.enabled = true;
            _health.Revive();
        }

        private void SetColor(Color color)
        {
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
