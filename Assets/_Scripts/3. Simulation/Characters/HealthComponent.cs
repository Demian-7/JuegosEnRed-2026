using Fusion;
using Game.Core;
using Game.Foundation;
using Game.Presentation;

namespace Game.Simulation 
{
    public class HealthComponent : NetworkBehaviour, IDamageable
    {
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
            CameraShake.AddTrauma(1f); 
            
            gameObject.SetActive(false);
            
            // should also trigger death VFX/SFX here.
        }
    }
}