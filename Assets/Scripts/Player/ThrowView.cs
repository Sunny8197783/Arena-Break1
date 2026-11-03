using UnityEngine;

namespace ArenaBreak.Player
{
    // 투척물 1인칭 모션: 꺼내기 → 왼손으로 핀 뽑기 → 뒤로 젖히고 대기 → 앞으로 휘둘러 투척 → 내리기
    public class ThrowView : MonoBehaviour
    {
        [SerializeField] private ThrowableSystem _throwables;
        [SerializeField] private GameObject _visual;
        [SerializeField] private GameObject[] _heldModels;
        [SerializeField] private Transform[] _pins;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private float _raiseTime = 0.15f;
        [SerializeField] private float _pinPullStart = 0.08f;
        [SerializeField] private float _pinPullEnd = 0.3f;
        [SerializeField] private float _pinPullDistance = 0.15f;
        [SerializeField] private float _loweredDrop = 0.35f;
        [SerializeField] private float _windBackPitch = -30f;
        [SerializeField] private float _windBackTime = 0.15f;
        [SerializeField] private float _throwPitch = 70f;
        [SerializeField] private float _throwSwingTime = 0.12f;
        [SerializeField] private float _lowerTime = 0.2f;
        [SerializeField] private float _tremble = 0.003f;
        [SerializeField] private float _trembleSpeed = 30f;

        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3[] _pinRest;
        private int _slot;
        private float _holdStart;
        private float _throwStart;
        private bool _throwing;

        private void Awake()
        {
            _restPosition = transform.localPosition;
            _restRotation = transform.localRotation;
            _pinRest = new Vector3[_pins.Length];
            for (int i = 0; i < _pins.Length; i++)
                _pinRest[i] = _pins[i].localPosition;
            _visual.SetActive(false);
        }

        private void OnEnable()
        {
            _throwables.HoldStarted += OnHoldStarted;
            _throwables.ThrowStarted += OnThrowStarted;
            _throwables.Finished += OnFinished;
        }

        private void OnDisable()
        {
            _throwables.HoldStarted -= OnHoldStarted;
            _throwables.ThrowStarted -= OnThrowStarted;
            _throwables.Finished -= OnFinished;
        }

        private void Update()
        {
            if (!_visual.activeSelf) return;

            float now = Time.time;
            float t = now - _holdStart;

            Transform pin = _pins[_slot];
            float pinT = Mathf.InverseLerp(_pinPullStart, _pinPullEnd, t);
            pin.localPosition = _pinRest[_slot] + Vector3.right * (pinT * _pinPullDistance);
            pin.gameObject.SetActive(pinT < 1f);
            bool pulling = t < _pinPullEnd;
            _leftHand.gameObject.SetActive(pulling);
            if (pulling) _leftHand.position = pin.position;

            float raise = Mathf.Clamp01(t / _raiseTime);
            float pitch = Mathf.Clamp01((t - _pinPullEnd) / _windBackTime) * _windBackPitch;
            float lower = 0f;
            Vector3 tremble = Vector3.zero;

            if (_throwing)
            {
                float swing = Mathf.Clamp01((now - _throwStart) / _throwSwingTime);
                pitch = Mathf.Lerp(pitch, _throwPitch, swing);
                if (swing >= 1f) _heldModels[_slot].SetActive(false);
                lower = Mathf.Clamp01((now - _throwStart - _throwSwingTime) / _lowerTime);
            }
            else if (t > _pinPullEnd)
            {
                // 핀이 빠진 수류탄을 쥐고 있는 긴장감
                tremble = new Vector3(Mathf.Sin(now * _trembleSpeed), Mathf.Cos(now * _trembleSpeed * 1.3f), 0f) * _tremble;
            }

            float drop = (1f - raise + lower) * _loweredDrop;
            transform.localPosition = _restPosition + Vector3.down * drop + tremble;
            transform.localRotation = _restRotation * Quaternion.Euler(pitch, 0f, 0f);
        }

        private void OnHoldStarted(int slot, float fuse)
        {
            _slot = slot;
            _holdStart = Time.time;
            _throwing = false;
            _visual.SetActive(true);
            for (int i = 0; i < _heldModels.Length; i++)
                _heldModels[i].SetActive(i == slot);
            _pins[slot].localPosition = _pinRest[slot];
            _pins[slot].gameObject.SetActive(true);
        }

        private void OnThrowStarted()
        {
            _throwing = true;
            _throwStart = Time.time;
        }

        private void OnFinished()
        {
            _visual.SetActive(false);
        }
    }
}
