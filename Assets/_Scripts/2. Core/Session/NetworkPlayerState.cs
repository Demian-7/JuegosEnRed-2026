using Fusion;
using UnityEngine;

namespace Game.Core
{
    public class NetworkPlayerState : NetworkBehaviour
    {
        [Networked] public PlayerRef PlayerRef { get; set; }
        [Networked] public NetworkString<_32> PlayerName { get; set; }
        [Networked] public NetworkBool IsReady { get; set; }
        [Networked] public NetworkBool IsHost { get; set; }

        public override void Spawned()
        {
            DontDestroyOnLoad(gameObject);

            if (Object.HasStateAuthority)
            {
                PlayerRef = Object.InputAuthority;

                IsHost = (PlayerRef == Runner.LocalPlayer);
            }
            

            Debug.Log($"Player {PlayerRef} joined the lobby. Host: {IsHost}");

            // TODO: Register with a local UI Lobby Manager to update the visual roster
        }

        // called from Ready button on the client
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetReady(NetworkBool ready)
        {
            IsReady = ready;
        }
    }
}