using Fusion;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner _runner;
    [SerializeField] private NetworkPrefabRef _playerPrefab;


    private void Awake()
    {
        _runner.AddCallbacks(this);
    }

    [ContextMenu("Start as Host")]
    public async void StartGameHost()
    {

        _runner.ProvideInput = true;

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = "Partida1"

        });
    }

    [ContextMenu("Start as Client")]
    public async void StartGameClient() 
    {

        _runner.ProvideInput = true;

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = "Partida1"

        });


    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
        {
            return;
        }

        runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
      
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
       
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
       
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
      
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
       
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
       
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Preparar inputs locales
        Vector2 Direction = Vector2.zero;

        if (Input.GetKey(KeyCode.W))
        {
            Direction.y += 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            Direction.y -= 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            Direction.x += 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            Direction.x -= 1;
        }


        // Crear network input data
        NetworkInputData data = new NetworkInputData();
        data.Direction = Direction;

        data.Buttons.Set((int)InputButton.Shoot, Input.GetKey(KeyCode.Space));
        // Enviar network input data
        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
       
    }

    public void OnConnectedToServer(NetworkRunner runner)
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

    public void OnSceneLoadDone(NetworkRunner runner)
    {
       
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        
    }
}
