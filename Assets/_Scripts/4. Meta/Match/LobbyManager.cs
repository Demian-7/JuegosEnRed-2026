using System;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Meta
{
    public class LobbyManager : NetworkBehaviour
    {
        private const int RequiredPlayers = 4;

        [Tooltip("The build index of the gameplay arena scene")] [SerializeField]
        private int _gameplaySceneIndex = 1;

        private Button _startButton;
        private int _countPlayers => Runner.ActivePlayers.Count();

        private void Awake()
        {
            _startButton = GetComponent<Button>();
            _startButton.onClick.AddListener(StartMatch);
        }

        /// <summary>
        /// Called by the "Start Match" UI Button.
        /// Only the UI button on the Host's screen should be interactable.
        /// </summary>
        public void StartMatch()
        {
            // host only
            if (Object == null || !Object.HasStateAuthority)
                return;

            if (_countPlayers < RequiredPlayers)
            {
                Debug.Log($"Can't start: {_countPlayers}/{RequiredPlayers} players.");
                return;
            }

            // close room when starting
            Runner.SessionInfo.IsOpen = false;

            Runner.LoadScene(SceneRef.FromIndex(_gameplaySceneIndex));
            Debug.Log("Host is starting the match. Loading arena...");
        }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                Runner.SessionInfo.IsOpen = true;
            }
        }
    }
}