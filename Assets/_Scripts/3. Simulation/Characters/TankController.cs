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

        // TEMPORARY: swap for the real TeamAssignmentService once it exists (Game.Meta).
        // TankController shouldn't need to change when that happens, only this field.
        private ITeammatePairing _pairing = new StubTeammatePairing();

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                // Object.InputAuthority is who THIS tank belongs to (since MatchSpawner should spawn each
                // tank with inputAuthority: player), not Runner.LocalPlayer, which would
                // always resolve to the host, since state authority for every tank lives on
                // the host regardless of which player it represents.
                MovementSource = Object.InputAuthority;
                Debug.Log($"Movement assigned to: {MovementSource}");
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
                    Debug.Log($"Client {AbilitySource} triggered DASH!");

                if (abilityInput.Buttons.WasPressed(_previousButtons, EAbilityButton.Shoot))
                    Debug.Log($"Client {AbilitySource} triggered SHOOT! In {MovementSource}'s entity!");

                _previousButtons = abilityInput.Buttons;
            }
        }
    }
}