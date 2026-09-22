using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Fusion;

namespace Game.Presentation
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameLauncher _gameLauncher;
        [SerializeField] private TMP_InputField _roomCodeInput;
        [SerializeField] private Button _createButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private TextMeshProUGUI _feedbackText;

        private void OnEnable()
        {
            _createButton.onClick.AddListener(OnCreateClicked);
            _joinButton.onClick.AddListener(OnJoinClicked);
            _roomCodeInput.onValueChanged.AddListener(OnInputChanged);
            
            OnInputChanged(_roomCodeInput.text);
        }

        private void OnDisable()
        {
            _createButton.onClick.RemoveListener(OnCreateClicked);
            _joinButton.onClick.RemoveListener(OnJoinClicked);
            _roomCodeInput.onValueChanged.RemoveListener(OnInputChanged);
        }

        private void OnInputChanged(string newText)
        {
            bool isValid = !string.IsNullOrWhiteSpace(newText);
            
            _createButton.interactable = isValid;
            _joinButton.interactable = isValid;

            if (isValid)
            {
                _feedbackText.text = ""; 
            }
            else
            {
                _feedbackText.text = "Room code cannot be empty.";
            }
        }

        private async void OnCreateClicked()
        {
            string roomCode = _roomCodeInput.text.Trim().ToUpper();
            
            SetConnectionState(true);
            _feedbackText.text = "Creating room...";

            var result = await _gameLauncher.StartHost(roomCode);
            HandleConnectionResult(result);
        }

        private async void OnJoinClicked()
        {
            string roomCode = _roomCodeInput.text.Trim().ToUpper();
            
            SetConnectionState(true);
            _feedbackText.text = "Searching for room...";

            var result = await _gameLauncher.StartClient(roomCode);
            HandleConnectionResult(result);
        }

        private void HandleConnectionResult(StartGameResult result)
        {
            if (result.Ok)
            {
                _feedbackText.text = "Connected!";
                return;
            }

            SetConnectionState(false);

            if (result.ShutdownReason == ShutdownReason.GameNotFound)
            {
                _feedbackText.text = "Room not found. Check the code and try again.";
            }
            else
            {
                _feedbackText.text = $"Connection failed: {result.ShutdownReason}";
            }
        }

        private void SetConnectionState(bool isConnecting)
        {
            _roomCodeInput.interactable = !isConnecting;
            _createButton.interactable = !isConnecting;
            _joinButton.interactable = !isConnecting;

            if (!isConnecting)
            {
                OnInputChanged(_roomCodeInput.text);
            }
        }
    }
}