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

        [SerializeField] private Transform _throwOrigin;
        [SerializeField] private Slot[] _slots;
        [SerializeField] private float _spawnForward = 0.6f;
        [SerializeField] private float _throwSpeed = 16f;
        [SerializeField] private float _upwardSpeed = 3f;
        [SerializeField] private float _spinSpeed = 10f;
        [SerializeField] private float _throwInterval = 0.8f;

        private float _nextThrowTime;

        public IReadOnlyList<Slot> Slots => _slots;

        public event Action<int, int> CountChanged;

        private void Update()
        {
            if (Time.time < _nextThrowTime) return;

            Keyboard keyboard = Keyboard.current;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!keyboard[_slots[i].Key].wasPressedThisFrame) continue;
                TryThrow(i);
                return;
            }
        }

        private void TryThrow(int index)
        {
            Slot slot = _slots[index];
            if (slot.Count <= 0) return;

            slot.Count--;
            _nextThrowTime = Time.time + _throwInterval;

            Vector3 forward = _throwOrigin.forward;
            Vector3 position = _throwOrigin.position + forward * _spawnForward;
            Throwable thrown = Instantiate(slot.Prefab, position, Quaternion.LookRotation(forward));
            thrown.Launch(forward * _throwSpeed + Vector3.up * _upwardSpeed, UnityEngine.Random.insideUnitSphere * _spinSpeed);

            CountChanged?.Invoke(index, slot.Count);
        }
    }
}
