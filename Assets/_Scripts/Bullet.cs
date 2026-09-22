using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Simulation
{
    public class Bullet : NetworkBehaviour
    {
        public static readonly HashSet<Bullet> ActiveBullets = new HashSet<Bullet>();

        [SerializeField] private float _bulletSpeed = 10f;
        [SerializeField] private float _lifetime = 2f;

        [Networked] private Vector3 Direction { get; set; }
        [Networked] private TickTimer LifeTimer { get; set; }

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                ActiveBullets.Add(this);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (hasState)
            {
                ActiveBullets.Remove(this);
            }
        }

        public void Init(Vector3 direction)
        {
            Direction = direction;
            LifeTimer = TickTimer.CreateFromSeconds(Runner, _lifetime);

            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }
        }

        public override void FixedUpdateNetwork()
        {
            _rb.linearVelocity = Direction * _bulletSpeed * Runner.DeltaTime;

            if (LifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!Object.HasStateAuthority)
                return;

            if (collision.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.DamageableHit();
            }

            Runner.Despawn(Object);
        }
    }
}