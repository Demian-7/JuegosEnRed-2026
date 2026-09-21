using UnityEngine;
using Fusion;

namespace Game.Simulation
{
    //[RequireComponent(typeof(Rigidbody))]
    public class CharacterMovement : NetworkBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        private Rigidbody _rb;
 
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }
 
        public void Simulate(Vector2 moveInput)
        {
            //if (!Object.HasInputAuthority)
            //    return;


            Debug.Log(moveInput);
            //_rb.linearVelocity = moveInput * _moveSpeed;
            transform.position += (Vector3)moveInput * Runner.DeltaTime;
        }
    }
}