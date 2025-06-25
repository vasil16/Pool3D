using Fusion;
using UnityEngine;

public enum PlayerRole { None, Player1, Player2 }

public class NetworkPlayer : NetworkBehaviour
{
    [Networked] public bool IsTurn { get; set; }
    [Networked] public string PlayerName { get; set; }

    public static NetworkPlayer Local;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            RPC_SendPlayerName(GameManager.instance.localPlayerName);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendPlayerName(string name)
    {
        PlayerName = name;
    }

    public bool IsMyTurn => HasInputAuthority && IsTurn;
}
