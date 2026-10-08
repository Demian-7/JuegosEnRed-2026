using TMPro;
using UnityEngine;

namespace Game.Presentation
{
    public class TitleBehaviour : MonoBehaviour
    {
        private TextMeshProUGUI _title;
        private int _playerCount = 1;

        public void UpdateText(int playerCount)
        {
            if (!_title)
            {
                _title = GetComponent<TextMeshProUGUI>();
            }

            if (playerCount == _playerCount)
                return;

            _title.text = $"{playerCount}/4 players joined";
            _playerCount = playerCount;
        }
    }
}