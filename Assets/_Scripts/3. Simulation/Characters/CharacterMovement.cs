using Fusion;
using Game.Core;
using Game.Foundation;
using UnityEngine;

namespace Game.Simulation
{
    //[RequireComponent(typeof(Rigidbody))]
    public class CharacterMovement : NetworkBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _dashForce = 800f;
        [SerializeField] private float _dashCooldown = 3f;

        private Rigidbody2D _rb;
        private NetworkRunner _runner;
        private Vector2 _lastMoveInput;

        private float _dashTimer = 0;
        [Networked, OnChangedRender(nameof(OnDashStateChanged))]
        private bool CanDash { get; set; } = false;
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
        }
 
        public void Init(NetworkRunner runner)
        {
            _runner = runner;
        }
        public void Simulate(Vector2 moveInput)
        {
            //if (!Object.HasInputAuthority)
            //    return;

            _lastMoveInput = moveInput;
            //Debug.Log(moveInput);
            _rb.linearVelocity = moveInput * _moveSpeed * Runner.DeltaTime;
            //transform.position += (Vector3)moveInput * _moveSpeed * Runner.DeltaTime;
        }

        public void DashAction()
        {
            Debug.Log("Dash Action");
            _rb.AddForce(_lastMoveInput * _dashForce, ForceMode2D.Impulse);
            CanDash = false;
        }

        public void TryDash()
        {
            // Si no puede dashear -> return
            if (!(CanDash))
                return;

            //Si puede dashear -> llamar a la accion
            DashAction();
        }

        public override void FixedUpdateNetwork()
        {
            if (!CanDash)
            {
                _dashTimer += Runner.DeltaTime;

                if (_dashTimer >= _dashCooldown)
                {
                    EnableDash();
                }
            }
        }

        private void EnableDash()
        {
            CanDash = true;
            _dashTimer = 0;
            
        }

        private void OnDashStateChanged()
        {
            _runner.GetComponentInParent<GameLauncher>().EventBus.Publish(new DashStateChangedEvent(CanDash));
        }
    }
}