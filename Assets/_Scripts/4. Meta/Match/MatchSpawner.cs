using Fusion;
using UnityEngine;

public class MatchSpawner : NetworkBehaviour, IPlayerJoined
{
    [SerializeField] private NetworkPrefabRef _tankPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (Object.HasStateAuthority)
        {
            Runner.Spawn(_tankPrefab, Vector3.zero, Quaternion.identity, player);
        }
    }
}