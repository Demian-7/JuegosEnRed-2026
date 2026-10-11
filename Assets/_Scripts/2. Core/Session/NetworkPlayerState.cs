using Fusion;
using Game.Meta;
using UnityEngine;

namespace Game.Core
{
    public class NetworkPlayerState : NetworkBehaviour
    {
        [Networked] public PlayerRef PlayerRef { get; set; }
        [Networked] public NetworkString<_32> PlayerName { get; set; }
        [Networked] public NetworkBool IsReady { get; set; }
        [Networked] public NetworkBool IsHost { get; set; }
        [Networked] public byte TeamId { get; set; }

        public override void Spawned()
        {
            DontDestroyOnLoad(gameObject);

            if (Object.HasStateAuthority)
            {
                PlayerRef = Object.InputAuthority;
                IsHost = PlayerRef == Runner.LocalPlayer;
                TeamId = MatchConstants.UNASSIGNED_TEAM;
            }
            
            Debug.Log($"Player {PlayerRef} joined the lobby. Host: {IsHost}");
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetTeam(byte teamId)
        {
            //TODO: validation (team was already full) before assigning, or after, on Match Start.
            
            TeamId = teamId;
        }

        // called from Ready button on the client
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetReady(NetworkBool ready)
        {
            IsReady = ready;
        }
    }
}