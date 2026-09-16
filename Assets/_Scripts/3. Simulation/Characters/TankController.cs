using Fusion;
using Game.Foundation;
using UnityEngine;

namespace Game.Simulation
{
    public class TankController : NetworkBehaviour
    {
        [Networked] public PlayerRef MovementSource { get; set; }
        [Networked] public PlayerRef AbilitySource { get; set; }
        [Networked] private NetworkButtons _previousButtons { get; set; }

        [SerializeField] private CharacterMovement _movement;

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                MovementSource = Runner.LocalPlayer;
                Debug.Log($"Movement assigned to Host: {MovementSource}");
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (Object.HasStateAuthority && AbilitySource.IsNone && Runner.ActivePlayers != null)
            {
                foreach (var player in Runner.ActivePlayers)
                {
                    if (player != MovementSource)
                    {
                        AbilitySource = player;
                        Debug.Log($"Ability assigned to Client: {AbilitySource}");
                        break;
                    }
                }
            }

            if (Runner.TryGetInputForPlayer<NetworkInputData>(MovementSource, out var moveInput))
            {
                _movement.Simulate(moveInput.MovementInput);
            }

            if (Runner.TryGetInputForPlayer<NetworkInputData>(AbilitySource, out var abilityInput))
            {
                if (abilityInput.Buttons.WasPressed(_previousButtons, EAbilityButton.Dash))
                {
                    // Ejecutar accion de dash en characterm movement  utilizando input de direccion
                    Debug.Log($"Client {AbilitySource} triggered DASH!");
                }
                    

                if (abilityInput.Buttons.WasPressed(_previousButtons, EAbilityButton.Shoot))
                {
                    Debug.Log($"Client {AbilitySource} triggered SHOOT!");
                    // Invocar spawn de bala con la posicion del transform y la direccion de ability input mouse
                }
                    

                _previousButtons = abilityInput.Buttons;
            }
        }
    }
}