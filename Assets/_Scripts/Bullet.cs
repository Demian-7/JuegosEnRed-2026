using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private float _lifetime = 2f;

    [Networked] private Vector3 Direction { get; set; }
    [Networked] private TickTimer LifeTimer { get; set; }

    private Rigidbody2D _rb;
    // Called by the tank right after spawning it (host only).
    private void Awake()
    {
      _rb = GetComponent<Rigidbody2D>();   
    }

    public void Init(Vector3 direction)
    {
        Direction = direction;
        LifeTimer = TickTimer.CreateFromSeconds(Runner, _lifetime);

        //GetComponent<Rigidbody>().linearVelocity = direction;
    }

    public override void FixedUpdateNetwork()
    {
        //if (!Object.HasStateAuthority) return;

        _rb.linearVelocity = Direction * _bulletSpeed * Runner.DeltaTime;
        //transform.position += Direction * _bulletSpeed * Runner.DeltaTime;

        // TODO hit detection, and killing the tank
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
        else if (false)
        {
            //TODO: Colision con props destruibles
        }

        Runner.Despawn(Object);
    }
}
