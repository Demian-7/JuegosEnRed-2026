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
    private float _bulletSpeed = 10f;
    private float _bulletLifetime = 10f;


    private NetworkRunner _runner;
    public void Init(NetworkRunner runner)
    {
        _runner = runner;
        GameLauncher.Instance.EventBus.Subscribe<RemoteConfigEvent>(OnRemoteConfigEvent);
    }

    public void TryShoot(PlayerRef shooter, Vector2 aimWorldPosition)
    {
        if (BulletCount <= 0)
            return;
        
        Shoot(shooter, aimWorldPosition);
    }

    private void Shoot(PlayerRef shooter, Vector2 aim)
    {
        LocalShoot();
        HostShoot(shooter, aim);
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

    private void HostShoot(PlayerRef shooter, Vector2 aim)
    {
        if (!Object.HasStateAuthority)
            return;

        Vector2 dir = (aim - (Vector2)transform.position).normalized;
        Vector2 spawnPos = (Vector2)transform.position + dir * 1f;
        
        NetworkObject bullet = Runner.Spawn(_bulletPrefab, spawnPos, Quaternion.identity);
        bullet.GetComponent<Bullet>().Init(spawnPos, dir, shooter, Object.Id, _bulletSpeed, _bulletLifetime);
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


    private void OnRemoteConfigEvent(RemoteConfigEvent evt)
    {
        MaxBullets = evt.ability_max_bullets;
        ReloadTime = evt.ability_reload_time;
        _bulletSpeed = evt.bullet_speed;
        _bulletLifetime = evt.bullet_lifetime;
    }

    private void OnDestroy()
    {
        if (GameLauncher.Instance != null)
        {
            GameLauncher.Instance.EventBus.Unsubscribe<RemoteConfigEvent>(OnRemoteConfigEvent);
        }
    }

}
