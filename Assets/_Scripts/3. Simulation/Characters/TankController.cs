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
                {
                    Shoot(abilityInput.AimWorldPosition);
                }

                _previousButtons = abilityInput.Buttons;
            }
        }

        private void Shoot(Vector2 aimWorldPosition)
        {
            if (!Object.HasStateAuthority) return;

            // Direction from this tank to where the teammate is aiming
            Vector3 direction = (Vector3)aimWorldPosition - transform.position;
            direction.z = 0f;
            direction.Normalize();

            // Spawn a bit in front so the bullet doesn't start inside the tank
            Vector3 spawnPosition = transform.position + direction * 1f;

            NetworkObject bullet = Runner.Spawn(_bulletPrefab, spawnPosition, Quaternion.identity);
            bullet.GetComponent<Bullet>().Init(direction);
        }
    }
}