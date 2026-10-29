using System;
using Fusion;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Meta
{
    /// <summary>
    /// Class for the < Text > behavior.
    /// </summary>
    [Serializable]
    public class UISelector
    {
        public Button LeftButton;
        public Button RightButton;
        public TextMeshProUGUI DisplayText;
        [Tooltip("The text options in order (e.g., 'Manual', 'Random')")]
        public string[] Options;

        private int _currentIndex;
        private Action<int> _onValueChanged;

        public void Initialize(Action<int> onValueChanged)
        {
            _onValueChanged = onValueChanged;
            LeftButton.onClick.AddListener(() => Shift(-1));
            RightButton.onClick.AddListener(() => Shift(1));
        }

        private void Shift(int direction)
        {
            _currentIndex = (_currentIndex + direction + Options.Length) % Options.Length;
            _onValueChanged?.Invoke(_currentIndex);
        }

        public void SyncVisuals(int targetIndex, bool isHost)
        {
            if (targetIndex >= 0 && targetIndex < Options.Length)
            {
                _currentIndex = targetIndex;
                DisplayText.text = Options[_currentIndex];
            }

            LeftButton.interactable = isHost;
            RightButton.interactable = isHost;
        }
    }

    public class LobbyUIView : MonoBehaviour
    {
        [Header("Settings Selectors")]
        [SerializeField] private UISelector _teamModeSelector;
        [SerializeField] private UISelector _mapModeSelector;
        [SerializeField] private UISelector _roundsSelector;

        [Header("Settings Toggles")]
        [SerializeField] private Toggle _modifiersToggle;
        [SerializeField] private Toggle _powerUpsToggle;

        [Header("Team Selection")]
        [SerializeField] private GameObject _teamSelectionPanel;
        [SerializeField] private Button _joinTeamAButton;
        [SerializeField] private TextMeshProUGUI _teamARosterText;
        [SerializeField] private Button _joinTeamBButton;
        [SerializeField] private TextMeshProUGUI _teamBRosterText;

        private MatchSettings _matchSettings;
        private NetworkRunner _runner;

        private void Start()
        {
            _teamModeSelector.Initialize(index => { if (IsHost()) _matchSettings.TeamMode = (ETeamSelectionMode)index; });
            _mapModeSelector.Initialize(index => { if (IsHost()) _matchSettings.MapMode = (EMapSelectionMode)index; });
            
            _roundsSelector.Initialize(index => {
                if (IsHost())
                {
                    if (index == 0) _matchSettings.Rounds = EBestOfRounds.Bo3;
                    else if (index == 1) _matchSettings.Rounds = EBestOfRounds.Bo5;
                    else if (index == 2) _matchSettings.Rounds = EBestOfRounds.Bo7;
                }
            });

            _modifiersToggle.onValueChanged.AddListener(val => { if (IsHost()) _matchSettings.ModifiersEnabled = val; });
            _powerUpsToggle.onValueChanged.AddListener(val => { if (IsHost()) _matchSettings.PowerUpsEnabled = val; });

            _joinTeamAButton.onClick.AddListener(() => TrySetTeam(MatchConstants.TEAM_A));
            _joinTeamBButton.onClick.AddListener(() => TrySetTeam(MatchConstants.TEAM_B));
        }

        private void Update()
        {
            // MatchSettings is spawned by LobbyManager after this view starts, so wait for it.
            _matchSettings = MatchSettings.Instance;
            if (_matchSettings == null) return;

            if (_runner == null) _runner = FindObjectOfType<NetworkRunner>();

            SyncUIWithNetworkState();
        }

        private void SyncUIWithNetworkState()
        {
            bool isHost = IsHost();

            _teamModeSelector.SyncVisuals((int)_matchSettings.TeamMode, isHost);
            _mapModeSelector.SyncVisuals((int)_matchSettings.MapMode, isHost);
            
            int roundsIndex = _matchSettings.Rounds == EBestOfRounds.Bo3 ? 0 : 
                              _matchSettings.Rounds == EBestOfRounds.Bo5 ? 1 : 2;
            _roundsSelector.SyncVisuals(roundsIndex, isHost);

            _modifiersToggle.interactable = isHost;
            _powerUpsToggle.interactable = isHost;
            
            if (!isHost)
            {
                _modifiersToggle.SetIsOnWithoutNotify(_matchSettings.ModifiersEnabled);
                _powerUpsToggle.SetIsOnWithoutNotify(_matchSettings.PowerUpsEnabled);
            }

            _teamSelectionPanel.SetActive(_matchSettings.TeamMode == ETeamSelectionMode.Manual);
            
            if (_matchSettings.TeamMode == ETeamSelectionMode.Manual)
            {
                string teamAString = "";
                string teamBString = "";

                foreach (var player in _runner.ActivePlayers)
                {
                    if (_runner.TryGetPlayerObject(player, out var playerObj))
                    {
                        var state = playerObj.GetComponent<NetworkPlayerState>();
                        
                        //TODO: Print player's name: state.PlayerName;
                        if (state.TeamId == MatchConstants.TEAM_A)
                            teamAString += $"Player {state.PlayerRef.PlayerId}\n";
                        else if (state.TeamId == MatchConstants.TEAM_B)
                            teamBString += $"Player {state.PlayerRef.PlayerId}\n";
                    }
                }

                _teamARosterText.text = teamAString;
                _teamBRosterText.text = teamBString;
            }
        }

        private void TrySetTeam(byte teamId)
        {
            if (_runner != null && _runner.TryGetPlayerObject(_runner.LocalPlayer, out var playerObj))
            {
                var playerState = playerObj.GetComponent<NetworkPlayerState>();
                playerState.RPC_SetTeam(teamId);
            }
        }

        private bool IsHost()
        {
            return _runner != null && _runner.IsServer;
        }
    }
}