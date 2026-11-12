using Fusion;
using Game.Foundation;
using Game.Meta;
using Game.Presentation;
using UnityEngine;

namespace Game.Simulation
{
    public class TankController : NetworkBehaviour
    {
        public static TankController LocalTank { get; private set; }

        [Networked] public PlayerRef MovementSource { get; set; }
        [Networked] public PlayerRef AbilitySource { get; set; }
        [Networked] private NetworkButtons _previousButtons { get; set; }

        [SerializeField] private CharacterMovement _movement;
        [SerializeField] private AbilityController _ability;
        [SerializeField] private NetworkPrefabRef _bulletPrefab;

        private ITeammatePairing _pairing = new StubTeammatePairing();

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                MovementSource = Object.InputAuthority;
            }

            if (MovementSource == Runner.LocalPlayer)
            {
                LocalTank = this;
            }

            Colorize();
            _movement.Init(Runner);
            _ability.Init(Runner);
            
            GetComponentInChildren<TankUI>()?.Init(this);
        }

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
                AbilitySource = _pairing.GetAbilitySourceFor(MovementSource, Runner);
            }

            if (MatchManager.Instance == null || MatchManager.Instance.Phase != MatchPhase.Playing) 
                return;

            if (Runner.TryGetInputForPlayer<NetworkInputData>(MovementSource, out var moveInput))
            {
                _movement.Simulate(moveInput.MovementInput);
            }

            if (Runner.TryGetInputForPlayer<NetworkInputData>(AbilitySource, out var abilityInput))
            {
                if (abilityInput.Buttons.WasPressed(_previousButtons, EAbilityButton.Dash))
                    _movement.TryDash();

                if (abilityInput.Buttons.WasPressed(_previousButtons, EAbilityButton.Shoot))
                    _ability.TryShoot(abilityInput.AimWorldPosition);

                _previousButtons = abilityInput.Buttons;
            }
        }
    }
}