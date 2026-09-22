using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;

namespace Game.Presentation
{
    public class UIManager : NetworkBehaviour, IPlayerJoined
    {
        [SerializeField] private TitleBehaviour _title;

        private int _playerCount => Runner.ActivePlayers.Count();

        public void PlayerJoined(PlayerRef _)
        {
            _title.UpdateText(_playerCount);
        }
    }
}