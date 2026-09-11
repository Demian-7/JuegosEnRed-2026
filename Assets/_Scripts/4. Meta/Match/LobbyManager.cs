using System;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Meta
{
    public class LobbyManager : MonoBehaviour
    {
        [Tooltip("The build index of your actual gameplay arena scene")] [SerializeField]
        private int _gameplaySceneIndex = 1;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(StartMatch);
        }

        /// <summary>
        /// Called by the "Start Match" UI Button.
        /// Only the UI button on the Host's screen should be interactable.
        /// </summary>
        public void StartMatch()
        {
            var runner = FindAnyObjectByType<NetworkRunner>();

            runner.LoadScene(SceneRef.FromIndex(_gameplaySceneIndex));

            Debug.Log("Host is starting the match. Loading arena...");
        }
    }
}