using Fusion;
using Game.Foundation;
using Game.Meta;
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

        private ITeammatePairing _pairing = new TeamAssignmentService();

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

            var sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) sprite.enabled = true;
            
            var canvas = GetComponentInChildren<Canvas>();
            if (canvas != null) canvas.enabled = true;

            Colorize();
            _movement.Init(Runner);
            _ability.Init(Runner);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (LocalTank == this)
            {
                LocalTank = null;
            }

            var sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) sprite.enabled = false;
            
            var canvas = GetComponentInChildren<Canvas>();
            if (canvas != null) canvas.enabled = false;
        }

        private void Colorize()
        {
            Color color = new Color(255, 0, 201);
            if (MovementSource == Runner.LocalPlayer) color = Color.green;
            else if (AbilitySource == Runner.LocalPlayer) color = new Color(0, 225, 255);

            var sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) sprite.color = color;
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
                    _ability.TryShoot(AbilitySource, abilityInput.AimWorldPosition);

                _previousButtons = abilityInput.Buttons;
            }
        }
    }
}