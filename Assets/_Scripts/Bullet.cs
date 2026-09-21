using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private float _lifetime = 2f;

    [Networked] private Vector3 Direction { get; set; }
    [Networked] private TickTimer LifeTimer { get; set; }

    [SerializeField] private Rigidbody2D _rb;
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

        _rb.linearVelocity = Direction * _bulletSpeed;
        //transform.position += Direction * _bulletSpeed * Runner.DeltaTime;

        // TODO hit detection, and killing the tank
        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }
}
