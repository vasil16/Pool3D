using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetworkPlayersHandler : MonoBehaviour, INetworkRunnerCallbacks
{

    public struct NetworkInputData : INetworkInput
    {
        public int dummy; // placeholder
    }

    [SerializeField] private NetworkPrefabRef playerPrefab;

    private NetworkPlayer netPlayer1, netPlayer2;
    private string player1Name = "Player 1";
    private string player2Name = "Player 2";
    public int playerCount = 0;
    [SerializeField] UIManager uiManager;


    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} joined.");

        if (!runner.IsServer) return;
        Debug.Log("1 join");
        Vector3 spawnPos = Vector3.zero;
        NetworkObject netObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        NetworkPlayer netPlayer = netObj.GetComponent<NetworkPlayer>();

        playerCount++;

        if (netPlayer1 == null)
            netPlayer1 = netPlayer;
        else if (netPlayer2 == null)
            netPlayer2 = netPlayer;

        // ✅ Only start game and show gameplay UI when both players are here
        if (playerCount == 2 && netPlayer1 != null && netPlayer2 != null)
        {
            Debug.Log("2 joins");
            uiManager.StartOnline(); // ✅ Moved here
            StartCoroutine(WaitAndStartGame());
        }
    }

    IEnumerator WaitAndStartGame()
    {
        // Wait until both names are assigned via RPC
        yield return new WaitUntil(() =>
            !string.IsNullOrEmpty(netPlayer1.PlayerName) &&
            !string.IsNullOrEmpty(netPlayer2.PlayerName)
        );

        Debug.Log($"Names received: {netPlayer1.PlayerName} & {netPlayer2.PlayerName}");

        GameManager.instance.SetupOnlinePlayers(netPlayer1.PlayerName,netPlayer2.PlayerName,netPlayer1,netPlayer2);

        Debug.Log("Both players spawned and assigned. Game starting...");
    }


    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} left.");
    }

    // Optional: implement others if needed
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        Debug.Log("hjsbvdkfhjvbdi");
        if (GameManager.instance == null || GameManager.instance.runner != runner)
        {
            return;
        }

        var data = new NetworkInputData(); // your struct
        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }
}
