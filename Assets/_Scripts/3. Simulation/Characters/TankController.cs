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
            }

            Colorize();
            _movement.Init(Runner);
            _ability.Init(Runner);
        }

        // Local-only tint: my tank blue, teammate's green, enemies red
        // set by MatchSpawner before the tank is replicated, runs on every client.
        private void Colorize()
        {
            Color color = new Color(255, 0, 201);
            if (MovementSource == Runner.LocalPlayer) color = Color.green;
            else if (AbilitySource == Runner.LocalPlayer) color = new Color(0, 225, 255);

            GetComponentInChildren<SpriteRenderer>().color = color;
        }

        public override void FixedUpdateNetwork()
        {
            if (Object.HasStateAuthority && AbilitySource.IsNone && !MovementSource.IsNone)
            {
                // Fallback for tanks not spawned by MatchSpawner (which already sets both sources).
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