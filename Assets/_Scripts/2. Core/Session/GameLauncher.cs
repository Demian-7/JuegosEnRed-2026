using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace Game.Core
{
    [RequireComponent(typeof(NetworkRunner))]
    public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkPlayerState _playerStatePrefab;

        private NetworkRunner _runner;
        private NetworkSceneManagerDefault _sceneManager;

        private void Awake()
        {
            _runner = GetComponent<NetworkRunner>();

            // Added under guard to avoid duplicating.
            _sceneManager = GetComponent<NetworkSceneManagerDefault>();
            if (_sceneManager == null)
            {
                _sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            }
        }

        public async void StartHost(string roomCode)
        {
            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = roomCode,
                PlayerCount = 4,
                IsVisible = false,
                SceneManager = _sceneManager
            });

            if (result.Ok)
                Debug.Log($"Hosted room: {roomCode}. Waiting for players...");
            else
                Debug.LogError($"Failed to Start Host: {result.ShutdownReason}");
        }

        public async void StartClient(string roomCode)
        {
            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = roomCode,
                SceneManager = _sceneManager
            });

            if (result.Ok)
                Debug.Log($"Joined room: {roomCode}");
            else
                Debug.LogError($"Failed to Join: {result.ShutdownReason}");
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
                return;

            if (_playerStatePrefab == null)
                return;

            var playerState = runner.Spawn(_playerStatePrefab, Vector3.zero, Quaternion.identity, player);

            runner.SetPlayerObject(player, playerState.Object);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer) return;

            if (runner.TryGetPlayerObject(player, out var playerObj))
            {
                runner.Despawn(playerObj);
                Debug.Log($"Player {player} left. Roster updated.");
            }
        }

        #region Unused Callbacks

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
        {
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
            ArraySegment<byte> data)
        {
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        #endregion
    }
}