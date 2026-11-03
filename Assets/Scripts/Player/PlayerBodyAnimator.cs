using UnityEngine;

namespace ArenaBreak.Player
{
    // 리깅된 모델이 없으므로 프리미티브 관절을 코드로 회전시켜 걷기·달리기·앉기·점프를 표현한다
    public class PlayerBodyAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _leftLeg;
        [SerializeField] private Transform _rightLeg;
        [SerializeField] private Transform _leftArm;
        [SerializeField] private Transform _rightArm;
        [SerializeField] private float _strideScale = 1.6f;
        [SerializeField] private float _legSwing = 35f;
        [SerializeField] private float _sprintLegSwing = 55f;
        [SerializeField] private float _moveThreshold = 0.1f;
        [SerializeField] private float _blendSpeed = 8f;
        [SerializeField] private float _crouchDrop = 0.42f;
        [SerializeField] private float _crouchKnee = 60f;
        [SerializeField] private float _airTuck = 30f;
        [SerializeField] private float _holdArmPitch = -70f;
        [SerializeField] private float _sprintArmPitch = -30f;

        private float _phase;
        private float _walkWeight;
        private float _sprintWeight;
        private float _airWeight;
        private Vector3 _visualRestPosition;

        private void Awake()
        {
            _visualRestPosition = _visualRoot.localPosition;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector3 velocity = _controller.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;

            _walkWeight = Mathf.MoveTowards(_walkWeight, speed > _moveThreshold ? 1f : 0f, _blendSpeed * dt);
            _sprintWeight = Mathf.MoveTowards(_sprintWeight, _player.IsSprinting ? 1f : 0f, _blendSpeed * dt);
            _airWeight = Mathf.MoveTowards(_airWeight, _player.IsGrounded ? 0f : 1f, _blendSpeed * dt);
            _phase += dt * speed * _strideScale;

            float crouch = _player.CrouchAmount;
            float swing = Mathf.Sin(_phase) * _walkWeight * (1f - _airWeight)
                * Mathf.Lerp(_legSwing, _sprintLegSwing, _sprintWeight);
            float bend = crouch * _crouchKnee + _airWeight * _airTuck;

            _leftLeg.localRotation = Quaternion.Euler(swing - bend, 0f, 0f);
            _rightLeg.localRotation = Quaternion.Euler(-swing - bend, 0f, 0f);

            // 총을 든 자세를 기본으로 두고, 달릴 때만 팔을 내려 흔든다
            float armPitch = Mathf.Lerp(_holdArmPitch, _sprintArmPitch, _sprintWeight);
            float armSwing = swing * _sprintWeight;
            _leftArm.localRotation = Quaternion.Euler(armPitch - armSwing, 0f, 0f);
            _rightArm.localRotation = Quaternion.Euler(armPitch + armSwing, 0f, 0f);

            _visualRoot.localPosition = _visualRestPosition + Vector3.down * (crouch * _crouchDrop);
        }
    }
}
