using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace Game.Foundation
{
    public class LocalInputProvider : MonoBehaviour, INetworkRunnerCallbacks
    {
        private Vector2 _moveInput;
        private Vector2 _aimWorldPosition;
        private NetworkButtons _buttons;

        private void ReadInput()
        {
            _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

            if (Camera.main != null)
            {
                Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                _aimWorldPosition = new Vector2(worldPoint.x, worldPoint.y);
            }

            _buttons.Set(EAbilityButton.Shoot, Input.GetMouseButton(0));
            _buttons.Set(EAbilityButton.Dash, Input.GetKey(KeyCode.Space));
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            ReadInput();

            input.Set(new NetworkInputData
            {
                MovementInput = _moveInput,
                AimWorldPosition = _aimWorldPosition,
                Buttons = _buttons
            });
        }

        #region Interface Methods

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        #endregion
    }
}