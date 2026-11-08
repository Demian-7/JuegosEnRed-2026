using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using Game.Foundation;
using UnityEngine;

namespace Game.Core
{
    public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        public IEventBus EventBus { get; private set; }
        public static GameLauncher Instance { get; private set; }

        [SerializeField] private NetworkPlayerState _playerStatePrefab;
        [SerializeField] private LocalInputProvider _localInputProvider;
        [SerializeField] private GameObject _runnerPrefab;

        private NetworkRunner _runner;
        private NetworkSceneManagerDefault _sceneManager;
        private FusionObjectPool _objectPool;

        // scene indexes, tbd if moved somewhere else
        [SerializeField] private int _menuSceneIndex = 0;
        [SerializeField] private int _lobbySceneIndex = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            EventBus = new EventBus();

            _objectPool = gameObject.AddComponent<FusionObjectPool>();
        }
        
        private void CreateRunner()
        {
            if (_runner != null)
            {
                Destroy(_runner.gameObject);
            }

            var runnerGO = Instantiate(_runnerPrefab, transform);
            
            _runner = runnerGO.GetComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            _sceneManager = runnerGO.GetComponent<NetworkSceneManagerDefault>();

            if (_localInputProvider != null)
            {
                _runner.AddCallbacks(_localInputProvider);
            }
    
            _runner.AddCallbacks(this);
        }

        public async Task<StartGameResult> StartHost(string roomCode)
        {
            CreateRunner();
            
            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = roomCode,
                PlayerCount = 4,
                IsVisible = false,
                Scene = SceneRef.FromIndex(_lobbySceneIndex),
                SceneManager = _sceneManager,
                ObjectProvider = _objectPool
            });

            if (result.Ok)
                Debug.Log($"Hosted room: {roomCode}. Waiting for players...");
            else
                Debug.LogError($"Failed to Start Host: {result.ShutdownReason}");

            return result;
        }

        public async Task<StartGameResult> StartClient(string roomCode)
        {
            CreateRunner();
            
            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = roomCode,
                SceneManager = _sceneManager,
                ObjectProvider = _objectPool
            });

            if (result.Ok)
                Debug.Log($"Joined room: {roomCode}");
            else
                Debug.LogError($"Failed to Join: {result.ShutdownReason}");

            return result;
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
                return;

            if (_playerStatePrefab == null)
                return;

            var playerState = runner.Spawn(_playerStatePrefab, Vector3.zero, Quaternion.identity, player,
                               flags: NetworkSpawnFlags.DontDestroyOnLoad);

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

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (_runner != null)
            {
                Destroy(_runner.gameObject);
            }
            
            _objectPool.ClearPools();
    
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != _menuSceneIndex)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(_menuSceneIndex);
            }
        }

        #region Unused Callbacks

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
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