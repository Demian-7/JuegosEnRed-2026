using Fusion;
using Game.Simulation;
using System;
using UnityEngine;

public class AbilityController : NetworkBehaviour
{

    [SerializeField] private NetworkPrefabRef _bulletPrefab;
    [SerializeField] private int _bulletCount = 1;
    [SerializeField] private int _maxBullets = 1;
    [SerializeField] private float reloadTime;

    private bool _reloading = false;
    private float _reloadTimer = 0;

    public void TryShoot(Vector2 aimWorldPosition)
    {
        //Intentar resolver disparo si las condiciones se cumplen: - Municion disponible - Interrupciones

        if (!(_bulletCount > 0))
            return;




        //Llamar al disparo
        Shoot(aimWorldPosition);
    }

    private void Shoot(Vector2 aimWorldPosition)
    {
        if (!Object.HasStateAuthority)
            return;

        // Direction from this tank to where the teammate is aiming
        Vector3 direction = (Vector3)aimWorldPosition - transform.position;
        direction.z = 0f;
        direction.Normalize();

        // Spawn a bit in front so the bullet doesn't start inside the tank
        Vector3 spawnPosition = transform.position + direction * 1f;

        NetworkObject bullet = Runner.Spawn(_bulletPrefab, spawnPosition, Quaternion.identity);
        bullet.GetComponent<Bullet>().Init(direction);

        _bulletCount --;
        if (_bulletCount == 0)
        {
            _reloading = true;
            
        }
    }

    private void ReloadWeapon()
    {
        _bulletCount = _maxBullets;
        _reloading = false;
        _reloadTimer = 0;
    }

    public override void FixedUpdateNetwork()
    {
        if (_reloading)
        {
            _reloadTimer += Runner.DeltaTime;

            if (_reloadTimer >= reloadTime)
            {
                ReloadWeapon();
            }
        }
    }
}
