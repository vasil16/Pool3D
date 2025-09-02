using Fusion;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [Networked] public int PlayerId { get; set; }
    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkBool IsTurn { get; set; }
    [Networked] public NetworkString<_16> PlayerName { get; set; }
    [Networked] public bool balllsAssigned { get; set; }

    public override void Spawned()
    {
        Debug.Log($"NetworkPlayer spawned for {Object.InputAuthority}");

        if (Object.HasInputAuthority)
        {
            RPC_SetPlayerName(GameManager.instance.localPlayerName);
        }
    }

    // Add this RPC method to NetworkPlayer.cs
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncTossResult(int winningPlayerIndex)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.currentPlayer = (GameManager.Users)winningPlayerIndex;

            // Update player states based on toss result
            GameManager.instance.playerStates[GameManager.instance.currentPlayer].gameState =
                PoolCamBehaviour.GameState.Break;
            GameManager.instance.playerStates[GameManager.instance.currentPlayer].isMyTurn = true;

            GameManager.instance.playerStates[GameManager.instance.GetOpponent(GameManager.instance.currentPlayer)].gameState =
                PoolCamBehaviour.GameState.Waiting;
            GameManager.instance.playerStates[GameManager.instance.GetOpponent(GameManager.instance.currentPlayer)].isMyTurn = false;

            // Update UI based on synchronized state
            GameManager.instance.UpdateBreakUIAfterToss();

            Debug.Log($"Toss result synchronized: Player {(winningPlayerIndex + 1)} breaks");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetPlayerName(NetworkString<_16> name)
    {
        PlayerName = name;
    }

    // UI SYNC RPCs
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncFullUI(int activePlayerIndex, NetworkString<_16> player1Name, NetworkString<_16> player2Name,
                              NetworkBool isBreakState)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.SyncUIFromNetwork(activePlayerIndex, player1Name.Value, player2Name.Value,
                                                 BallBehaviour.BallType.white, BallBehaviour.BallType.white,
                                                 isBreakState);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncPlayerGameState(int currentPlayerIndex, PoolCamBehaviour.GameState currentPlayerState,
                                       PoolCamBehaviour.GameState opponentPlayerState)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.currentPlayer = (GameManager.Users)currentPlayerIndex;

            GameManager.instance.playerStates[GameManager.Users.player1].gameState =
                (GameManager.Users.player1 == GameManager.instance.currentPlayer) ? currentPlayerState : opponentPlayerState;

            GameManager.instance.playerStates[GameManager.Users.player2].gameState =
                (GameManager.Users.player2 == GameManager.instance.currentPlayer) ? currentPlayerState : opponentPlayerState;

            GameManager.instance.playerStates[GameManager.Users.player1].isMyTurn =
                (GameManager.Users.player1 == GameManager.instance.currentPlayer);

            GameManager.instance.playerStates[GameManager.Users.player2].isMyTurn =
                (GameManager.Users.player2 == GameManager.instance.currentPlayer);

            GameManager.instance.UpdateUIFromPlayerStates();
        }
    }
    

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncBallAssignment(NetworkString<_16> player1Name, NetworkString<_16> player2Name,
                                     BallBehaviour.BallType player1BallType, BallBehaviour.BallType player2BallType)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdatePlayerNames(player1Name.Value, player2Name.Value);
            GameManager.instance.UpdateBallImages(player1BallType, player2BallType);
        }
    }

    // NEW: BALL ASSIGNMENT ONLY RPC
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncBallAssignmentOnly(NetworkString<_16> player1Name, NetworkString<_16> player2Name,
                                         BallBehaviour.BallType player1BallType, BallBehaviour.BallType player2BallType)
    {
        if (GameManager.instance != null)
        {
            // Only update ball images, not the entire UI
            GameManager.instance.UpdatePlayerNames(player1Name.Value, player2Name.Value);
            GameManager.instance.UpdateBallImages(player1BallType, player2BallType);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncPocketedBall(int playerIndex, int ballCode)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.DisableBallImage(ballCode);
        }
    }
}