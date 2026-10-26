using Fusion;
using Game.Core;
using Game.Foundation;
using Game.Simulation;
using System;
using UnityEngine;

public class AbilityController : NetworkBehaviour
{

    [SerializeField] private NetworkPrefabRef _bulletPrefab;


    [Networked, OnChangedRender(nameof(OnCurrentBulletsUpdatePublish))] 
    public int BulletCount { get; set; }

    [Networked, OnChangedRender(nameof(OnMaxBulletsUpdatePublish))]
    public int MaxBullets { get; set; }

    [Networked]
    public float ReloadTime { get; set; }

    [Networked, OnChangedRender(nameof(OnReloadingStatePublish))]
    public bool Reloading { get; set; }
    private float _reloadTimer = 0;


    private NetworkRunner _runner;
    public void Init(NetworkRunner runner)
    {
        _runner = runner;
    }

    public void TryShoot(Vector2 aimWorldPosition)
    {
        //Intentar resolver disparo si las condiciones se cumplen: - Municion disponible - Interrupciones

        if (!(BulletCount > 0))
            return;




        //Llamar al disparo
        Shoot(aimWorldPosition);
    }

    private void Shoot(Vector2 aimWorldPosition)
    {
        LocalShoot();
        HostShoot(aimWorldPosition);
    }

    private void ReloadWeapon()
    {
        BulletCount = MaxBullets;
        Reloading = false;
        _reloadTimer = 0;
    }

    public override void FixedUpdateNetwork()
    {
        if (Reloading)
        {
            _reloadTimer += Runner.DeltaTime;

            if (_reloadTimer >= ReloadTime)
            {
                ReloadWeapon();
            }
        }
    }



    private void LocalShoot()
    {
        Debug.Log("Local shoot");
        BulletCount--;



        if (BulletCount == 0)
        {
            Reloading = true;
            
        }
    }

    private void HostShoot(Vector2 aimWorldPosition)
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
    }


    
    private void OnCurrentBulletsUpdatePublish()
    {
        Debug.Log("Change render bullet count");
        
        GameLauncher.Instance.EventBus.Publish(new CurrentBulletsUpdateEvent(BulletCount));   
    }

    private void OnMaxBulletsUpdatePublish()
    {
        GameLauncher.Instance.EventBus.Publish(new MaxBulletsUpdateEvent(MaxBullets));
    }

    private void OnReloadingStatePublish()
    {
        GameLauncher.Instance.EventBus.Publish(new ReloadingStateChangedEvent(Reloading));
    }

}
