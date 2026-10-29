using Fusion;
using Game.Core;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Meta
{
    public class LobbyManager : NetworkBehaviour
    {
        private const int RequiredPlayers = 2;

        [Tooltip("The build index of the gameplay arena scene")] [SerializeField]
        private int _gameplaySceneIndex = 1;
        [SerializeField] private Button _startButton;
        [SerializeField] private NetworkPrefabRef _matchSettingsPrefab;
        
        private int _countPlayers => Runner.ActivePlayers.Count();

        private void Awake()
        {
            _startButton.onClick.AddListener(StartMatch);
        }

        /// <summary>
        /// Called by the "Start Match" UI Button.
        /// Only the UI button on the Host's screen should be interactable.
        /// </summary>
        public void StartMatch()
        {
            if (Object == null || !Object.HasStateAuthority)
                return;

            if (_countPlayers < RequiredPlayers)
            {
                Debug.Log($"Can't start: {_countPlayers}/{RequiredPlayers} players.");
                return;
            }

            if (MatchSettings.Instance.TeamMode == ETeamSelectionMode.Manual && !ValidateManualTeams())
            {
                Debug.Log("Can't start: Manual teams are not balanced 2v2.");
            }

            Runner.SessionInfo.IsOpen = false;
            var map = MatchSettings.Instance.FixedMapId;

            LoadSelectedLevel(map);
        }

        private void LoadSelectedLevel(byte mapName)
        {
            Runner.LoadScene(SceneRef.FromIndex(mapName +2));
        }

        private bool ValidateManualTeams()
        {
            int teamA = 0;
            int teamB = 0;

            foreach (var player in Runner.ActivePlayers)
            {
                if (Runner.TryGetPlayerObject(player, out var playerObj))
                {
                    var state = playerObj.GetComponent<NetworkPlayerState>();
                    if (state.TeamId == MatchConstants.TEAM_A)
                    {
                        teamA++;
                    }
                    else if (state.TeamId == MatchConstants.TEAM_B)
                    {
                        teamB++;
                    }
                }
            }
            
            return teamA == 2 && teamB == 2;
        }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                Runner.SessionInfo.IsOpen = true;

                if (MatchSettings.Instance == null)
                {
                    Runner.Spawn(_matchSettingsPrefab, flags: NetworkSpawnFlags.DontDestroyOnLoad);
                }
            }
        }
    }
}