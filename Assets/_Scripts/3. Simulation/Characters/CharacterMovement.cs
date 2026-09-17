using UnityEngine;
using Fusion;

namespace Game.Simulation
{
    [RequireComponent(typeof(Rigidbody2D))]

    public class CharacterMovement : NetworkBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _dashForce = 10f;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public void Simulate(Vector2 moveInput)
        {
            if (!Object.HasStateAuthority) return;
            _rb.linearVelocity = moveInput * _moveSpeed;
        }

        public void DashAbility()
        {
            if (!Object.HasStateAuthority) return;

            _rb.AddForce(Vector2.zero, ForceMode2D.Impulse);
           
        }
    }
}