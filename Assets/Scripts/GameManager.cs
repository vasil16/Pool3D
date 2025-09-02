using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI References")]
    [SerializeField] public GameObject homePanel;
    [SerializeField] public GameObject placeBallPop, startPanel, restartPanel, messageObject;
    [SerializeField] Sprite[] solidBalls;
    [SerializeField] Sprite[] stripeBalls;
    [SerializeField] Image[] p1Balls, p2Balls;
    [SerializeField] GameObject[] playerIndicator;
    [SerializeField] Text messageText;
    [SerializeField] public TextMeshProUGUI player1Txt, player2Txt;
    [SerializeField] AudioClip uiFx;
    [SerializeField] public NetworkPrefabRef _playerPrefab;
    public string localPlayerName;
    public NetworkRunner runner;

    public int ballhitCount;

    [Space(10)] public Player player1, player2;
    [Space(10)] public List<GameObject> pocketedBalls;

    public Dictionary<Users, Player> players = new();
    public Users currentPlayer;
    public GameMode gameMode;

    public Action<Users> onGameComplete;

    private GamePlayController playerController;
    private PoolCamBehaviour poolCam;

    public enum GameMode { offline, cpu, online }
    public enum Users { player1, player2 }

    // NETWORK SYNC STATE
    private bool _isNetworkInitialized = false;
    private bool _isInitializing = false;

    // Player states
    [System.Serializable]
    public class PlayerState
    {
        public PoolCamBehaviour.GameState gameState;
        public bool isMyTurn;
    }

    public Dictionary<Users, PlayerState> playerStates = new Dictionary<Users, PlayerState>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        playerController = GamePlayController.instance;
        poolCam = FindObjectOfType<PoolCamBehaviour>();

        InitializePlayerStates();
    }

    private void InitializePlayerStates()
    {
        playerStates[Users.player1] = new PlayerState { gameState = PoolCamBehaviour.GameState.Waiting, isMyTurn = false };
        playerStates[Users.player2] = new PlayerState { gameState = PoolCamBehaviour.GameState.Waiting, isMyTurn = false };
    }

    private void OnEnable()
    {
        onGameComplete += GameCompleteEvent;
    }

    private void Start()
    {
        playerController.touchDisabled = false;

        if (gameMode == GameMode.online)
        {
            runner = GameNetworkManager.Instance.GetNetworkRunner();
            Debug.Log("Online mode detected, waiting for network setup...");
        }
        else
        {
            playerController.manager = this;
            SetupPlayers();
        }
    }

    public void PlayUIFx()
    {
        if (gameFx != null && uiFx != null)
        {
            gameFx.PlayOneShot(uiFx);
        }
    }

    [SerializeField] GameObject startMatchPop;

    // ONLINE MODE METHODS
    public IEnumerator InitializeNetworkGame()
    {
        startMatchPop.SetActive(true);
        if (_isNetworkInitialized || _isInitializing) yield break;

        _isInitializing = true;
        Debug.Log("Starting network game initialization...");

        // DISABLE HOME PANEL via UIManager
        if (UIManager.instance != null)
        {
            UIManager.instance.HideHomePanel();
        }

        // Setup players
        yield return StartCoroutine(SetupOnlinePlayers());

        if (players.Count < 2)
        {
            Debug.LogError("Failed to setup players");
            _isInitializing = false;
            yield break;
        }
        

        // Setup gameplay controller
        var gameplayController = FindObjectOfType<GamePlayController>();
        if (gameplayController != null)
        {
            gameplayController.manager = this;
            gameplayController.SetOnlineMode(true);
        }

        _isNetworkInitialized = true;
        _isInitializing = false;
        Debug.Log("Network game initialization complete!");

        // FIX: ONLY MASTER CLIENT INITIATES TOSS
        if (runner != null && runner.IsSharedModeMasterClient)
        {
            Debug.Log("Master client initiating toss...");
            StartCoroutine(TossOnline());
        }
        else
        {
            Debug.Log("Non-master client waiting for toss result...");
            // Non-master clients will receive the toss result via RPC sync
        }
    }

    public IEnumerator SetupOnlinePlayers()
    {
        Debug.Log("SetupOnlinePlayers called");

        // Wait for NetworkPlayer objects to be properly spawned
        float timeout = 10f;
        float timer = 0f;
        NetworkPlayer[] networkPlayers = new NetworkPlayer[0];

        while (networkPlayers.Length < 2 && timer < timeout)
        {
            networkPlayers = FindObjectsOfType<NetworkPlayer>();
            Debug.Log($"Looking for NetworkPlayers: {networkPlayers.Length}/2 found");

            if (networkPlayers.Length < 2)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        if (networkPlayers.Length == 2)
        {
            var sortedPlayers = networkPlayers.OrderBy(p => p.Object.InputAuthority.PlayerId).ToArray();

            player1 = new Player(sortedPlayers[0].PlayerName.Value, p1Balls, sortedPlayers[0]);
            player2 = new Player(sortedPlayers[1].PlayerName.Value, p2Balls, sortedPlayers[1]);

            players[Users.player1] = player1;
            players[Users.player2] = player2;

            Debug.Log($"Players assigned: {player1.name} vs {player2.name}");
        }
        else
        {
            Debug.LogError($"Failed to find NetworkPlayers: {networkPlayers.Length}/2");
        }
    }

    public IEnumerator TossOnline()
    {
        Debug.Log("Starting online toss...");
        yield return new WaitForSeconds(0.5f);

        // DOUBLE CHECK: ONLY MASTER CLIENT DETERMINES TOSS RESULT
        if (runner != null && runner.IsSharedModeMasterClient)
        {
            int rand = UnityEngine.Random.Range(0, 2);
            currentPlayer = (Users)rand;

            Debug.Log($"Toss result: {currentPlayer} will break (Master client decided)");

            // SET PROPER STATES: Breaking player gets Break, other gets Waiting
            playerStates[currentPlayer].gameState = PoolCamBehaviour.GameState.Break;
            playerStates[currentPlayer].isMyTurn = true;

            playerStates[GetOpponent(currentPlayer)].gameState = PoolCamBehaviour.GameState.Waiting;
            playerStates[GetOpponent(currentPlayer)].isMyTurn = false;

            SyncFullUIToAllPlayers();

            // SYNC THE TOSS RESULT TO ALL PLAYERS
            SyncTossResultToAllPlayers(currentPlayer);

            playerController.isWaiting = true;

            // Handle break UI based on turn
            UpdateBreakUIAfterToss();

            StartCoroutine(Popup($"{players[currentPlayer].name} will break"));            
        }
        else
        {
            Debug.Log("Not master client - skipping toss logic");
            // This shouldn't happen since only master client calls this method
            yield break;
        }
    }

    public void SyncTossResultToAllPlayers(Users winningPlayer)
    {
        var networkPlayers = FindObjectsOfType<NetworkPlayer>();
        foreach (var netPlayer in networkPlayers)
        {
            if (netPlayer.Object.HasStateAuthority)
            {
                netPlayer.RPC_SyncTossResult((int)winningPlayer);
                break;
            }
        }
    }

    public void UpdateBreakUIAfterToss()
    {
        startMatchPop.SetActive(false);
        UIManager.instance.HideMultiplayerPanel();
        UIManager.instance.ShowGameplayUI();
        if (IsLocalPlayersTurn())
        {
            Debug.Log("Local player won toss - enabling break UI");
            if (UIManager.instance != null) UIManager.instance.ShowPlaceBallPopup(true);
            if (startPanel != null) startPanel.SetActive(true);
        }
        else
        {
            Debug.Log("Other player won toss - disabling UI");
            if (UIManager.instance != null) UIManager.instance.ShowPlaceBallPopup(false);
            if (startPanel != null) startPanel.SetActive(false);
            playerController.SetAllUIEnabled(false);
        }
    }

    private IEnumerator TransitionToGameplayUI()
    {
        yield return new WaitForSeconds(1f);

        if (UIManager.instance != null)
        {
            UIManager.instance.HideMultiplayerPanel();
            UIManager.instance.ShowGameplayUI();
        }
    }

    // OFFLINE MODE METHODS
    private void SetupPlayers()
    {
        player1 = new Player("Player 1", p1Balls);
        player2 = new Player(gameMode == GameMode.cpu ? "CPU" : "Player 2", p2Balls);
        players[Users.player1] = player1;
        players[Users.player2] = player2;

        StartCoroutine(Toss());
    }

    private IEnumerator Toss()
    {
        Debug.Log("toss tt");
        yield return null;
        int rand = UnityEngine.Random.Range(0, 2);
        currentPlayer = (Users)rand;

        playerController.isWaiting = true;
        playerIndicator[rand].SetActive(true);

        StartCoroutine(Popup($"{players[currentPlayer].name} will break"));

        placeBallPop.SetActive(players[currentPlayer].name != "CPU");
        if (players[currentPlayer].name == "CPU")
        {
            playerController.StartCPUMode();
        }
    }

    // UI SYNC METHODS
    public void UpdatePlayerNames(string player1Name, string player2Name)
    {
        if (player1Txt != null) player1Txt.text = player1Name;
        if (player2Txt != null) player2Txt.text = player2Name;

        if (players.ContainsKey(Users.player1)) players[Users.player1].name = player1Name;
        if (players.ContainsKey(Users.player2)) players[Users.player2].name = player2Name;

        Debug.Log($"Player names updated: {player1Name} vs {player2Name}");
    }

    public void UpdateBallImages(BallBehaviour.BallType player1BallType, BallBehaviour.BallType player2BallType)
    {
        // ONLY update if balls are actually assigned (not white/unassigned)
        if (player1BallType == BallBehaviour.BallType.white ||
            player2BallType == BallBehaviour.BallType.white)
        {
            Debug.Log("Skipping ball image update - balls not assigned yet");
            return;
        }

        if (players.ContainsKey(Users.player1)) players[Users.player1].BallType = player1BallType;
        if (players.ContainsKey(Users.player2)) players[Users.player2].BallType = player2BallType;

        SetBallImages();
        Debug.Log("Ball images updated after assignment");
    }

    public void UpdatePlayerIndicator(int activePlayerIndex)
    {
        currentPlayer = (Users)activePlayerIndex;

        for (int i = 0; i < playerIndicator.Length; i++)
        {
            if (playerIndicator[i] != null)
            {
                playerIndicator[i].SetActive(i == activePlayerIndex);
            }
        }
        Debug.Log($"Player indicator updated: Player {activePlayerIndex + 1} active");
    }

    public void SyncUIFromNetwork(int activePlayerIndex, string player1Name, string player2Name,
                            BallBehaviour.BallType player1BallType, BallBehaviour.BallType player2BallType,
                            bool isBreakState)
    {
        // Update player names (always available)
        UpdatePlayerNames(player1Name, player2Name);

        // Update player indicator (always available)
        UpdatePlayerIndicator(activePlayerIndex);

        // ONLY update ball images if they're properly assigned (not white)
        if (player1BallType != BallBehaviour.BallType.white &&
            player2BallType != BallBehaviour.BallType.white)
        {
            UpdateBallImages(player1BallType, player2BallType);
        }

        // Handle break state UI
        if (isBreakState)
        {
            currentPlayer = (Users)activePlayerIndex;
            if (IsLocalPlayersTurn())
            {
                if (UIManager.instance != null) UIManager.instance.ShowPlaceBallPopup(true);
                if (startPanel != null) startPanel.SetActive(true);
            }
            else
            {
                if (UIManager.instance != null) UIManager.instance.ShowPlaceBallPopup(false);
                if (startPanel != null) startPanel.SetActive(false);
            }
        }

        Debug.Log("UI synchronized from network");
    }

    public void SyncFullUIToAllPlayers()
    {
        int activePlayerIndex = (int)currentPlayer;
        bool isBreakState = (poolCam != null && poolCam.gameState == PoolCamBehaviour.GameState.Break);

        var networkPlayers = FindObjectsOfType<NetworkPlayer>();
        foreach (var netPlayer in networkPlayers)
        {
            if (netPlayer.Object.HasStateAuthority)
            {
                // DON'T include ball types in full UI sync
                netPlayer.RPC_SyncFullUI(
                    activePlayerIndex,
                    players[Users.player1].name,
                    players[Users.player2].name,
                    isBreakState
                );
                break;
            }
        }
    }

    public void SyncPlayerGameStatesToAllPlayers()
    {
        PoolCamBehaviour.GameState currentPlayerState = playerStates[currentPlayer].gameState;
        PoolCamBehaviour.GameState opponentPlayerState = playerStates[GetOpponent(currentPlayer)].gameState;

        var networkPlayers = FindObjectsOfType<NetworkPlayer>();
        foreach (var netPlayer in networkPlayers)
        {
            if (netPlayer.Object.HasStateAuthority)
            {
                netPlayer.RPC_SyncPlayerGameState(
                    (int)currentPlayer,
                    currentPlayerState,
                    opponentPlayerState
                );
                break;
            }
        }
    }

    // TURN MANAGEMENT
    public void CompleteTurn(bool isFoul = false)
    {
        Debug.Log($"Completing turn. Foul: {isFoul}");
        SwitchTurn(isFoul);

        if (isFoul)
        {
            StartCoroutine(Popup("Foul! Opponent gets ball in hand"));
        }
    }

    public void SwitchTurn(bool isFoul = false)
    {
        Debug.Log($"Switching turn. Foul: {isFoul}, Current player: {currentPlayer}");

        Users previousPlayer = currentPlayer;
        currentPlayer = GetOpponent(currentPlayer);

        if (gameMode == GameMode.online && runner != null && runner.IsSharedModeMasterClient)
        {
            players[previousPlayer].netPlayer.IsTurn = false;
            players[currentPlayer].netPlayer.IsTurn = true;
        }

        // SET STATES BASED ON FOUL OR NORMAL TURN CHANGE
        if (isFoul)
        {
            playerStates[currentPlayer].gameState = PoolCamBehaviour.GameState.Break;
            playerStates[previousPlayer].gameState = PoolCamBehaviour.GameState.Waiting;
        }
        else
        {
            playerStates[currentPlayer].gameState = PoolCamBehaviour.GameState.Aim;
            playerStates[previousPlayer].gameState = PoolCamBehaviour.GameState.Waiting;
        }

        if (gameMode == GameMode.online)
        {
            SyncPlayerGameStatesToAllPlayers();
        }

        SetIndicator();
        UpdateUIFromPlayerStates();

        Debug.Log($"Turn switched to: {currentPlayer}");
    }

    public void UpdateUIFromPlayerStates()
    {
        if (playerController == null) return;

        if (playerStates[currentPlayer].isMyTurn)
        {
            switch (playerStates[currentPlayer].gameState)
            {
                case PoolCamBehaviour.GameState.Break:
                    playerController.SetBreakUIEnabled(true);
                    playerController.SetPlayerUIEnabled(false);
                    break;

                case PoolCamBehaviour.GameState.Aim:
                    playerController.SetBreakUIEnabled(false);
                    playerController.SetPlayerUIEnabled(true);
                    break;

                case PoolCamBehaviour.GameState.Hit:
                    playerController.SetAllUIEnabled(false);
                    break;
            }
        }
        else
        {
            playerController.SetAllUIEnabled(false);
        }
    }

    // COMMON METHODS
    public bool IsLocalPlayersTurn()
    {
        if (gameMode == GameMode.online && runner != null)
        {
            // Use the synchronized state from the dictionary
            return playerStates.ContainsKey(currentPlayer) &&
                   playerStates[currentPlayer].isMyTurn &&
                   IsLocalPlayer(currentPlayer);
        }
        return true; // For offline/CPU modes
    }

    private bool IsLocalPlayer(Users player)
    {
        if (players.ContainsKey(player) && players[player].netPlayer != null)
        {
            return players[player].netPlayer.Object.HasInputAuthority;
        }
        return false;
    }

    public void SetBallImages()
    {
        player1Txt.text = player1.name;
        player2Txt.text = player2.name;
        bool isCurrentPlayerStripe = players[currentPlayer].BallType == BallBehaviour.BallType.stripe;

        Sprite[] currentBalls = isCurrentPlayerStripe ? stripeBalls : solidBalls;
        Sprite[] opponentBalls = isCurrentPlayerStripe ? solidBalls : stripeBalls;

        for (int i = 0; i < 7; i++)
        {
            players[currentPlayer].playerBalls[i].sprite = currentBalls[i];
            players[GetOpponent(currentPlayer)].playerBalls[i].sprite = opponentBalls[i];
        }
    }

    public void DisableBallImage(int ballCode)
    {
        players[currentPlayer].DisableBallImage(ballCode);
    }

    public void ClosePlacePop()
    {
        if (!playerController.CueBallValid()) return;

        placeBallButton.SetActive(true);

        foreach (GameObject ball in playerController.balls)
        {
            ball.GetComponent<Rigidbody>().isKinematic = false;
        }

        playerController.isWaiting = false;

        // TRANSITION FROM BREAK TO AIM STATE
        if (playerStates[currentPlayer].gameState == PoolCamBehaviour.GameState.Break)
        {
            playerStates[currentPlayer].gameState = PoolCamBehaviour.GameState.Aim;

            if (poolCam != null) poolCam.gameState = PoolCamBehaviour.GameState.Aim;

            if (gameMode == GameMode.online) SyncPlayerGameStatesToAllPlayers();
        }

        poolCam.gameState = PoolCamBehaviour.GameState.Aim;
        placeBallPop.SetActive(false);
        startPanel.SetActive(false);
        playerController.StartGame();
    }

    // UI AND GAME FLOW METHODS
    public void SetIndicator()
    {
        playerIndicator[(int)currentPlayer].SetActive(true);
        playerIndicator[(int)GetOpponent(currentPlayer)].SetActive(false);
    }

    public void GameCompleteEvent(Users winner)
    {
        restartPanel.SetActive(true);
        restartPanel.transform.GetChild(0).GetComponent<Text>().text = $"{winner} WINS";
    }

    public IEnumerator Popup(string message)
    {
        messageText.text = message;
        messageObject.SetActive(true);
        yield return new WaitForSeconds(2.2f);
        messageObject.SetActive(false);
    }

    public void PlaySound(AudioClip clip)
    {
        gameFx.PlayOneShot(clip);
    }

    public void PlayBallSound(AudioClip clip)
    {
        if (ballhitCount % 2 == 0 && !playerController.isFoul && playerController.isWaiting)
        {
            gameFx.PlayOneShot(clip);
        }
    }

    public bool CorrectBallPlayed(BallBehaviour.BallType ballType)
    {
        if (ballType == BallBehaviour.BallType.black)
        {
            if (players[currentPlayer].pocketedBalls.Count == 7) return true;
        }
        return ballType == players[currentPlayer].BallType;
    }

    public Users GetOpponent(Users player) => player == Users.player1 ? Users.player2 : Users.player1;

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    [SerializeField] AudioSource gameFx;
    public GameObject placeBallButton;
}

#region helperClass
[System.Serializable]
public class Player
{
    public string name;
    public BallBehaviour.BallType BallType;
    public List<GameObject> pocketedBalls = new();
    public Image[] playerBalls;
    public NetworkPlayer netPlayer;

    public bool IsMyTurn => netPlayer != null && netPlayer.IsTurn && netPlayer.Object.HasInputAuthority;

    public Player(string name, Image[] playerBalls, NetworkPlayer netPlayer=null)
    {
        this.name = name;
        this.playerBalls = playerBalls;
        this.netPlayer = netPlayer;
    }

    public void DisableBallImage(int ballCode) => playerBalls[ballCode].enabled = false;
}
#endregion