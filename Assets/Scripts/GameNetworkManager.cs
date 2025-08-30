using UnityEngine;
using Fusion;
using System;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    [SerializeField] private NetworkRunner _networkRunnerPrefab;
    private NetworkRunner _networkRunner;

    // RENAME THIS to avoid conflict with Fusion.GameMode
    public enum NetworkGameMode
    {
        Offline1v1,
        OfflineVSCPU,
        Online1v1
    }

    private NetworkGameMode _currentGameMode = NetworkGameMode.Offline1v1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async void StartGame(NetworkGameMode mode, string sessionName = null)
    {
        _currentGameMode = mode;

        if (mode == NetworkGameMode.Online1v1)
        {
            // Start online game
            if (_networkRunner == null && _networkRunnerPrefab != null)
            {
                _networkRunner = Instantiate(_networkRunnerPrefab);
                _networkRunner.name = "NetworkRunner";
                DontDestroyOnLoad(_networkRunner.gameObject);

                // ADD THIS: Add callbacks to detect player connections
                var callbacks = gameObject.AddComponent<NetworkCallbacks>();
                _networkRunner.AddCallbacks(callbacks);
            }

            if (_networkRunner == null)
            {
                Debug.LogError("NetworkRunner prefab is not assigned!");
                return;
            }

            var sceneManager = _networkRunner.GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null)
            {
                sceneManager = _networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }

            var startGameArgs = new StartGameArgs()
            {
                GameMode = Fusion.GameMode.Shared,
                SessionName = sessionName ?? "PoolSession",
                SceneManager = sceneManager,
                PlayerCount = 2,
                Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex)
            };

            var result = await _networkRunner.StartGame(startGameArgs);

            if (result.Ok)
            {
                Debug.Log("Online game started successfully - waiting for players...");
            }
            else
            {
                Debug.LogError($"Failed to start game: {result.ErrorMessage}");
                // Optional: Show error in UI
            }
        }
        else
        {
            // Start offline game
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    public string GetSessionName()
    {
        if (_networkRunner != null)
        {
            return _networkRunner.SessionInfo.Name;
        }
        return "UnknownSession";
    }

    public NetworkGameMode GetCurrentGameMode() => _currentGameMode;
    public bool IsOnlineMode() => _currentGameMode == NetworkGameMode.Online1v1;
    public NetworkRunner GetNetworkRunner() => _networkRunner;
}