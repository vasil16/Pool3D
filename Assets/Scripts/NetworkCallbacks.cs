using Fusion;
using UnityEngine;
using System.Collections;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;

public class NetworkCallbacks : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkPrefabRef _playerPrefab;
    private GameManager _gameManager;

    private void Start()
    {
        _gameManager = GameManager.instance;
        _playerPrefab = _gameManager._playerPrefab;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} joined! Total players: {runner.ActivePlayers.Count()}");

        // Each client spawns their own NetworkPlayer
        if (player == runner.LocalPlayer)
        {
            Vector3 spawnPosition = Vector3.zero;
            NetworkObject playerObj = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            Debug.Log($"Spawned NetworkPlayer for local player {player.PlayerId}");
        }

        if (runner.ActivePlayers.Count() == 2)
        {
            Debug.Log("Both players connected! Initializing game...");
            StartCoroutine(DelayedInitialize(runner));
        }
    }

    private IEnumerator DelayedInitialize(NetworkRunner runner)
    {
        yield return new WaitForSeconds(1f); // Wait for players to be ready
        InitializeNetworkGame(runner);
    }

    private void InitializeNetworkGame(NetworkRunner runner)
    {
        if (_gameManager != null)
        {
            _gameManager.gameMode = GameManager.GameMode.online;
            _gameManager.runner = runner;
            _gameManager.StartCoroutine(_gameManager.InitializeNetworkGame());
        }
    }

    // Required empty interface methods
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        //throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        //throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        //throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        //throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        //throw new NotImplementedException();
    }
}