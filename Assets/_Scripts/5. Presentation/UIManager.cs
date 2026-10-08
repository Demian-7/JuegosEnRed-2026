using System.Linq;
using Fusion;
using UnityEngine;

namespace Game.Presentation
{
    public class UIManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        [SerializeField] private TitleBehaviour _title;

        public override void Spawned()
        {
            _title.UpdateText(Runner.ActivePlayers.Count());
        }

        public void PlayerJoined(PlayerRef player)
        {
            if (HasStateAuthority)
            {
                Rpc_UpdatePlayerCount(Runner.ActivePlayers.Count());
            }
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (HasStateAuthority)
            {
                Rpc_UpdatePlayerCount(Runner.ActivePlayers.Count());
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_UpdatePlayerCount(int newCount)
        {
            _title.UpdateText(newCount);
        }
    }
}