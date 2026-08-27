using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float _bulletSpeed = 5;
    private Vector3 _direction = Vector3.right;
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority) 
        {
            transform.Translate(_direction * _bulletSpeed * Runner.DeltaTime);

            if (transform.position.x > 10)
            {
                Runner.Despawn(Object);
            }
        }
    }
}
