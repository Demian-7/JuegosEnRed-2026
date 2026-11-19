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
        private readonly ITeammatePairing _pairing = new TeamAssignmentService();

        public void SpawnAll()
        {
            int teamASlot = 0;
            int teamBSlot = 2;

            foreach (var player in Runner.ActivePlayers)
            {
                int teamIndex = TeamAssignmentService.GetTeamIndex(player, Runner);
                int spawnIndex;

                if (teamIndex == 0)
                {
                    spawnIndex = Mathf.Clamp(teamASlot, 0, _spawnPoints.Length - 1);
                    teamASlot++;
                }
                else
                {
                    spawnIndex = Mathf.Clamp(teamBSlot, 0, _spawnPoints.Length - 1);
                    teamBSlot++;
                }

                Vector3 position = _spawnPoints[spawnIndex].position;
                PlayerRef owner = player;

                _tanks.Add(Runner.Spawn(_tankPrefab, position, Quaternion.identity, owner,
                    (runner, obj) =>
                    {
                        var tank = obj.GetComponent<TankController>();
                        tank.MovementSource = owner;
                        tank.AbilitySource = _pairing.GetAbilitySourceFor(owner, runner);
                    }));
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