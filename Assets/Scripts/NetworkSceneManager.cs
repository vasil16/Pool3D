//using Fusion;
//using UnityEngine.SceneManagement;
//using System.Threading.Tasks;

//public class NetworkSceneManager : INetworkSceneManager
//{
//    private readonly NetworkRunner _runner;

//    public NetworkSceneManager(NetworkRunner runner)
//    {
//        _runner = runner;
//    }

//    public async Task<bool> SwitchScene(SceneRef sceneRef, LoadSceneMode mode)
//    {
//        if (!_runner.IsSceneAuthority)
//            return false;

//        var scene = sceneRef.AsIndex;
//        if (scene < 0 || scene >= SceneManager.sceneCountInBuildSettings)
//            return false;

//        var sceneName = SceneUtility.GetScenePathByBuildIndex(scene);
//        var loadOp = SceneManager.LoadSceneAsync(scene, mode);

//        while (!loadOp.isDone)
//            await Task.Delay(16);

//        // Scene loaded successfully, initialize game
//        if (_runner.IsServer)
//        {
//            var gameManager = FindObjectOfType<GameManager>();
//            if (gameManager != null)
//            {
//                gameManager.SetupOnlinePlayers();
//            }
//        }

//        return true;
//    }

//    // Required interface methods
//    public Task<bool> UnloadScene(SceneRef sceneRef) => Task.FromResult(true);
//    public Task Initialize(NetworkRunner runner) => Task.CompletedTask;
//    public void Shutdown() { }
//}
using Fusion;

public class NetworkGameState : NetworkBehaviour
{
    [Networked] public int CurrentPlayerTurn { get; set; }
    [Networked] public bool GameStarted { get; set; }

    public static NetworkGameState Instance { get; private set; }

    public override void Spawned()
    {
        if (Instance == null) Instance = this;
    }
}