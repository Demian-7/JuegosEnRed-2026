using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Meta
{
    public class MatchSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkPrefabRef _tankPrefab;
        [SerializeField] private Transform[] _spawnPoints;
        private readonly List<NetworkObject> _tanks = new List<NetworkObject>();

        // Called by MatchManager at the start of every round.
        public void SpawnAll()
        {
            int index = 0;
            foreach (var player in Runner.ActivePlayers)
            {
                Vector3 position = _spawnPoints[index].position;
                _tanks.Add(Runner.Spawn(_tankPrefab, position, Quaternion.identity, player));
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
