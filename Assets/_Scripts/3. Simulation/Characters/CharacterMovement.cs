using UnityEngine;
using Fusion;

namespace Game.Simulation
{
    //[RequireComponent(typeof(Rigidbody))]
    public class CharacterMovement : NetworkBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        private Rigidbody2D _rb;
        private Vector2 _lastMoveInput;
 
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
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
            _rb.AddForce(_lastMoveInput, ForceMode2D.Impulse);
        }
    }
}