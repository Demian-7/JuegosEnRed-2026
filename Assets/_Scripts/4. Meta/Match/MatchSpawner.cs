using System.Collections.Generic;
using Fusion;
using Game.Simulation;
using UnityEngine;

namespace Game.Meta
{
    public class MatchSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkPrefabRef _tankPrefab;
        [SerializeField] private Transform[] _spawnPoints;

        private readonly List<NetworkObject> _tanks = new List<NetworkObject>();

        private ITeammatePairing _pairing => TeamAssignmentService.Instance;

        // Called by MatchManager at the start of every round.
        public void SpawnAll()
        {
            int index = 0;
            foreach (var player in Runner.ActivePlayers)
            {
                Vector3 position = _spawnPoints[index].position;
                PlayerRef owner = player;

                //  every client knows both sources inside Spawned()
                _tanks.Add(Runner.Spawn(_tankPrefab, position, Quaternion.identity, owner,
                    (runner, obj) =>
                    {
                        var tank = obj.GetComponent<TankController>();
                        tank.MovementSource = owner;
                        tank.AbilitySource = _pairing.GetAbilitySourceFor(owner, runner);
                    }));

                index++;
            }
        }

        public void DespawnAll()
        {
            foreach (var tank in _tanks)
            {
                if (tank != null) Runner.Despawn(tank);
            }
            _tanks.Clear();
        }
    }
}
