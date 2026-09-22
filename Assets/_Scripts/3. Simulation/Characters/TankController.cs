using Fusion;
using Game.Foundation;
using Game.Meta;
using UnityEngine;

namespace Game.Simulation
{
    public class TankController : NetworkBehaviour
    {
        [Networked] public PlayerRef MovementSource { get; set; }
        [Networked] public PlayerRef AbilitySource { get; set; }
        [Networked] private NetworkButtons _previousButtons { get; set; }

        [SerializeField] private CharacterMovement _movement;
        [SerializeField] private AbilityController _ability;
        [SerializeField] private NetworkPrefabRef _bulletPrefab;

        // TEMPORARY: swap for the real TeamAssignmentService once it exists (Game.Meta).
        private ITeammatePairing _pairing = new StubTeammatePairing();

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                // Object.InputAuthority is who this tank belongs to
                MovementSource = Object.InputAuthority;
                Debug.Log($"Movement assigned to: {MovementSource}");
                GetComponent<HealthComponent>().Init(Object, Runner);

            }
        }

        public override void FixedUpdateNetwork()
        {
            if (Object.HasStateAuthority && AbilitySource.IsNone && !MovementSource.IsNone)
            {
                AbilitySource = _pairing.GetAbilitySourceFor(MovementSource, Runner);
                if (!AbilitySource.IsNone)
                    Debug.Log($"Ability assigned to: {AbilitySource}");
            }

            if (Runner.TryGetInputForPlayer<NetworkInputData>(MovementSource, out var moveInput))
            {
                _movement.Simulate(moveInput.MovementInput);
            }

            if (Runner.TryGetInputForPlayer<NetworkInputData>(AbilitySource, out var abilityInput))
            {
                if (abilityInput.Buttons.WasPressed(_previousButtons, EAbilityButton.Dash))
                {
                    _movement.TryDash();
                }

                if (abilityInput.Buttons.WasPressed(_previousButtons, EAbilityButton.Shoot))
                {
                    _ability.TryShoot(abilityInput.AimWorldPosition);
                }

                _previousButtons = abilityInput.Buttons;
            }
        }
    }
}