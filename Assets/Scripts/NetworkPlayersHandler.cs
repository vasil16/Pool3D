using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetworkPlayersHandler : NetworkBehaviour, INetworkRunnerCallbacks
{

    public struct NetworkInputData : INetworkInput
    {
        public int dummy; // placeholder
    }

    private Dictionary<PlayerRef, NetworkPlayer> players = new Dictionary<PlayerRef, NetworkPlayer>();


    [SerializeField] private NetworkPrefabRef playerPrefab;

    private NetworkPlayer netPlayer1, netPlayer2;
    private string player1Name = "Player 1";
    private string player2Name = "Player 2";
    public int playerCount = 0;
    [SerializeField] UIManager uiManager;


    //public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    //{
    //    Debug.Log($"Player {player.PlayerId} joined.");

    //    if (!runner.IsServer) return;
    //    Debug.Log("1 join");
    //    Vector3 spawnPos = Vector3.zero;
    //    NetworkObject netObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
    //    NetworkPlayer netPlayer = netObj.GetComponent<NetworkPlayer>();

    //    playerCount++;

    //    if (netPlayer1 == null)
    //        netPlayer1 = netPlayer;
    //    else if (netPlayer2 == null)
    //        netPlayer2 = netPlayer;

    //    // ✅ Only start game and show gameplay UI when both players are here
    //    if (playerCount == 2 && netPlayer1 != null && netPlayer2 != null)
    //    {
    //        Debug.Log("2 joins");
    //        uiManager.StartOnline(); // ✅ Moved here
    //        StartCoroutine(WaitAndStartGame());
    //    }
    //}

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} joined.");

        if (!runner.IsServer) return; // ✅ Only server spawns players

        Vector3 spawnPos = Vector3.zero;
        NetworkObject netObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        NetworkPlayer netPlayer = netObj.GetComponent<NetworkPlayer>();

        players[player] = netPlayer; // ✅ Always safe, dictionary key is unique

        if (players.Count == 2)
        {
            Debug.Log("Both players joined, scheduling game start...");
            StartCoroutine(WaitAndCallRPC());
        }
    }

    private IEnumerator WaitAndCallRPC()
    {
        yield return null; // wait one frame
        RPC_StartGame();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartGame()
    {
        Debug.Log("RPC_StartGame called on all clients");

        uiManager.StartOnline();
        StartCoroutine(WaitAndStartGame());
    }

    private IEnumerator WaitAndStartGame()
    {
        // ✅ First wait until 2 players exist in dictionary
        yield return new WaitUntil(() => players.Count == 2);

        // ✅ Then wait until both have valid names
        yield return new WaitUntil(() => players.Values.All(p => !string.IsNullOrEmpty(p.PlayerName)));

        Debug.Log("Both players ready. Names: " + string.Join(", ", players.Values.Select(p => p.PlayerName)));

        // Get ordered players (so P1 and P2 are consistent across clients)
        var orderedPlayers = players.OrderBy(kv => kv.Key.RawEncoded).ToList();

        NetworkPlayer p1 = orderedPlayers[0].Value;
        NetworkPlayer p2 = orderedPlayers[1].Value;

        GameManager.instance.SetupOnlinePlayers(p1.PlayerName, p2.PlayerName, p1, p2);

        Debug.Log("Game starting!");
    }



    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} left.");
    }

    // Optional: implement others if needed
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        //Debug.Log("hjsbvdkfhjvbdi");
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
