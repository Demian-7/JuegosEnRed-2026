using Fusion;
using Game.Core;
using Game.Foundation;
using Game.Presentation;
using UnityEngine;

namespace Game.Simulation 
{
    public class HealthComponent : NetworkBehaviour, IDamageable
    {
        [Header("Death presentation")]
        [SerializeField] private TankExplosionVFX _explosionPrefab;

        [Networked] public NetworkBool IsAlive { get; set; } = true;

        public void DamageableHit()
        {
            if (!Object.HasStateAuthority || !IsAlive) 
                return;

            IsAlive = false;

            PlayerRef victim = Object.InputAuthority; 
            if (TryGetComponent<TankController>(out var character))
            {
                victim = character.MovementSource;
            }

            GameLauncher.Instance.EventBus.Publish(new PlayerDiedEvent(victim));

            Rpc_TriggerDeathPresentation();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_TriggerDeathPresentation()
        {
            CameraRig.AddTrauma(1f); 

            TankShotScreenFX.Play(ShotImpact.Kill);

            TankExplosionVFX.Spawn(_explosionPrefab, transform.position, ResolveTankColor());
            
            gameObject.SetActive(false);
            
            // should also trigger death SFX here.
        }

        // TODO: replace with your real per-viewer color source (green = own, turquoise = teammate, magenta = enemy).
        // Fallback: reads the color of the tank's SpriteRenderer, if it has one.
        private Color ResolveTankColor()
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            return sr != null ? sr.color : Color.white;
        }
    }
}