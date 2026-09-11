using UnityEngine;
using Fusion;

namespace Game.Simulation
{
    [RequireComponent(typeof(Rigidbody2D))]

    public class CharacterMovement : NetworkBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
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
    }
}