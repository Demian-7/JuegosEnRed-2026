using Game.Core;
using TMPro;
using UnityEngine;

namespace Game.Presentation
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _systemMessageText;

        private void Start()
        {
            var launcher = FindAnyObjectByType<GameLauncher>();

            string feedback = launcher.ConsumeFeedback();
            if (!string.IsNullOrEmpty(feedback))
            {
                _systemMessageText.text = feedback;
            }
            else
            {
                _systemMessageText.text = "Waiting for players...";
            }
        }
    }
}