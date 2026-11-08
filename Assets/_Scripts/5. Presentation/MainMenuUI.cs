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
        [SerializeField] private TMP_InputField _playerNameInput;
        [SerializeField] private TMP_InputField _roomCodeInput;
        [SerializeField] private Button _createButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private TextMeshProUGUI _feedbackText;
        
        private GameLauncher _gameLauncher;
        
        private void OnEnable()
        {
            _createButton.onClick.AddListener(OnCreateClicked);
            _joinButton.onClick.AddListener(OnJoinClicked);
            _roomCodeInput.onValueChanged.AddListener(OnInputChanged);
            _playerNameInput.onValueChanged.AddListener(OnInputChanged);
            
            _playerNameInput.text = PlayerPrefs.GetString("PlayerName", "");
            
            OnInputChanged("");
        }

        private void OnDisable()
        {
            _createButton.onClick.RemoveListener(OnCreateClicked);
            _joinButton.onClick.RemoveListener(OnJoinClicked);
            _roomCodeInput.onValueChanged.RemoveListener(OnInputChanged);
            _playerNameInput.onValueChanged.RemoveListener(OnInputChanged);
        }

        private void OnInputChanged(string _)
        {
            bool isNameValid = !string.IsNullOrWhiteSpace(_playerNameInput.text);
            bool isRoomValid = !string.IsNullOrWhiteSpace(_roomCodeInput.text);
            bool isValid = isNameValid && isRoomValid;
            
            _createButton.interactable = isValid;
            _joinButton.interactable = isValid;

            if (isValid)
            {
                _feedbackText.text = ""; 
            }
            else if (!isNameValid)
            {
                _feedbackText.text = "Player name cannot be empty.";
            }
            else
            {
                _feedbackText.text = "Room code cannot be empty.";
            }
        }

        private async void OnCreateClicked()
        {
            if (!_gameLauncher) _gameLauncher = GameLauncher.Instance;
            
            string roomCode = _roomCodeInput.text.Trim().ToUpper();
            PlayerPrefs.SetString("PlayerName", _playerNameInput.text.Trim());
            
            SetConnectionState(true);
            _feedbackText.text = "Creating room...";

            var result = await _gameLauncher.StartHost(roomCode);
            HandleConnectionResult(result);
        }

        private async void OnJoinClicked()
        {
            if (!_gameLauncher) _gameLauncher = GameLauncher.Instance;
            
            string roomCode = _roomCodeInput.text.Trim().ToUpper();
            PlayerPrefs.SetString("PlayerName", _playerNameInput.text.Trim());
            
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
                _feedbackText.text = "Room not found. Check the code and try again.";
            else
                _feedbackText.text = $"Connection failed: {result.ShutdownReason}";
        }

        private void SetConnectionState(bool isConnecting)
        {
            _roomCodeInput.interactable = !isConnecting;
            _playerNameInput.interactable = !isConnecting;
            _createButton.interactable = !isConnecting;
            _joinButton.interactable = !isConnecting;

            if (!isConnecting) OnInputChanged("");
        }
    }
}