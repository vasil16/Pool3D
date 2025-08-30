using Fusion;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [Networked] public int PlayerId { get; set; }
    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkBool IsTurn { get; set; }
    [Networked] public NetworkString<_16> PlayerName { get; set; }

    public override void Spawned()
    {
        Debug.Log($"NetworkPlayer spawned for {Object.InputAuthority}");

        if (Object.HasInputAuthority)
        {
            RPC_SetPlayerName(GameManager.instance.localPlayerName);
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
                              BallBehaviour.BallType player1BallType, BallBehaviour.BallType player2BallType,
                              NetworkBool isBreakState)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.SyncUIFromNetwork(activePlayerIndex, player1Name.Value, player2Name.Value,
                                                 player1BallType, player2BallType, isBreakState);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncUIForToss(int activePlayerIndex, NetworkString<_16> player1Name, NetworkString<_16> player2Name)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdatePlayerNames(player1Name.Value, player2Name.Value);
            GameManager.instance.UpdatePlayerIndicator(activePlayerIndex);
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncPocketedBall(int playerIndex, int ballCode)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.DisableBallImage(ballCode);
        }
    }
}