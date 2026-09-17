using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Meta
{
    public class MatchSpawner : NetworkBehaviour, IPlayerJoined
    {
        [SerializeField] private NetworkPrefabRef _tankPrefab;

        private readonly HashSet<PlayerRef> _spawnedFor = new HashSet<PlayerRef>();

        public override void Spawned()
        {
            if (!Object.HasStateAuthority) return;

            if (Runner.ActivePlayers != null)
            {
                foreach (var player in Runner.ActivePlayers)
                {
                    TrySpawnFor(player);
                }
            }
        }

        public void PlayerJoined(PlayerRef player)
        {
            if (!Object.HasStateAuthority) return;
            TrySpawnFor(player);
        }

        private void TrySpawnFor(PlayerRef player)
        {
            if (_spawnedFor.Contains(player)) return;

            Runner.Spawn(_tankPrefab, Vector3.zero, Quaternion.identity, player);
            _spawnedFor.Add(player);
        }
    }
}