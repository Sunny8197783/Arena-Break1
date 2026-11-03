using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    public class ThrowableSystem : MonoBehaviour
    {
        [Serializable]
        public class Slot
        {
            [SerializeField] private string _label;
            [SerializeField] private Key _key;
            [SerializeField] private Throwable _prefab;
            [SerializeField] private int _count = 2;

            public string Label => _label;
            public Key Key => _key;
            public Throwable Prefab => _prefab;
            public int Count { get => _count; set => _count = value; }
        }

        private enum State
        {
            Idle,
            Holding,
            Throwing,
            Recovering
        }

        [SerializeField] private Transform _throwOrigin;
        [SerializeField] private WeaponSwitcher _switcher;
        [SerializeField] private Slot[] _slots;
        [SerializeField] private float _spawnForward = 0.6f;
        [SerializeField] private float _throwSpeed = 16f;
        [SerializeField] private float _upwardSpeed = 3f;
        [SerializeField] private float _spinSpeed = 10f;
        [SerializeField] private float _pinPullTime = 0.3f;
        [SerializeField] private float _throwWindup = 0.12f;
        [SerializeField] private float _recoverTime = 0.35f;

        private State _state;
        private int _slotIndex;
        private float _cookStartTime;
        private float _phaseEndTime;
        private int[] _startCounts;

        public IReadOnlyList<Slot> Slots => _slots;
        public bool IsBusy => _state != State.Idle;

        public event Action<int, int> CountChanged;
        public event Action<int, float> HoldStarted;
        public event Action ThrowStarted;
        public event Action Finished;

        private void Awake()
        {
            _startCounts = new int[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
                _startCounts[i] = _slots[i].Count;
        }

        private void OnDisable()
        {
            // 핀을 뽑은 채 쓰러지면 수류탄을 발밑에 떨어뜨린다
            if (_state == State.Holding) Spawn(Vector3.zero, Vector3.zero);
            if (_state != State.Idle) EndThrow();
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Idle:
                    PollStart();
                    break;
                case State.Holding:
                    UpdateHolding();
                    break;
                case State.Throwing:
                    if (Time.time < _phaseEndTime) break;
                    Vector3 forward = _throwOrigin.forward;
                    Spawn(forward * _throwSpeed + Vector3.up * _upwardSpeed,
                        UnityEngine.Random.insideUnitSphere * _spinSpeed);
                    BeginRecover();
                    break;
                case State.Recovering:
                    if (Time.time >= _phaseEndTime) EndThrow();
                    break;
            }
        }

        public void Refill()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Count = _startCounts[i];
                CountChanged?.Invoke(i, _slots[i].Count);
            }
        }

        private void PollStart()
        {
            Keyboard keyboard = Keyboard.current;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!keyboard[_slots[i].Key].wasPressedThisFrame || _slots[i].Count <= 0) continue;

                _slotIndex = i;
                _slots[i].Count--;
                _cookStartTime = Time.time;
                _state = State.Holding;
                _switcher.SetHolstered(true);
                CountChanged?.Invoke(i, _slots[i].Count);
                HoldStarted?.Invoke(i, _slots[i].Prefab.FuseTime);
                return;
            }
        }

        private void UpdateHolding()
        {
            float cooked = Time.time - _cookStartTime;
            if (cooked >= _slots[_slotIndex].Prefab.FuseTime)
            {
                // 너무 오래 쥐고 있으면 손에서 터진다
                Spawn(Vector3.zero, Vector3.zero);
                BeginRecover();
                return;
            }

            bool released = !Keyboard.current[_slots[_slotIndex].Key].isPressed;
            if (!released || cooked < _pinPullTime) return;

            _state = State.Throwing;
            _phaseEndTime = Time.time + _throwWindup;
            ThrowStarted?.Invoke();
        }

        private void Spawn(Vector3 velocity, Vector3 spin)
        {
            Vector3 forward = _throwOrigin.forward;
            Vector3 position = _throwOrigin.position + forward * _spawnForward;
            Throwable thrown = Instantiate(_slots[_slotIndex].Prefab, position, Quaternion.LookRotation(forward));
            thrown.Launch(velocity, spin, Time.time - _cookStartTime);
        }

        private void BeginRecover()
        {
            _state = State.Recovering;
            _phaseEndTime = Time.time + _recoverTime;
        }

        private void EndThrow()
        {
            _state = State.Idle;
            _switcher.SetHolstered(false);
            Finished?.Invoke();
        }
    }
}
