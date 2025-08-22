using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] PoolCamBehaviour poolCam;
    [SerializeField] AnimationCurve lerpCurve;
    [SerializeField] public GameObject placeBallPop, startPanel, restartPanel;
    [SerializeField] Sprite[] solidBalls;
    [SerializeField] Sprite[] stripeBalls;
    [SerializeField] Image[] p1Balls, p2Balls;
    [SerializeField] GameObject[] playerIndicator;
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

    public enum GameMode { offline, cpu, online }
    public enum Users { player1, player2 }

    private void Awake()
    {
        instance = this;
        //if (gameMode == GameMode.online) return;
        playerController = GamePlayController.instance;
        //playerController.manager = this;
    }

    private void OnEnable()
    {
        onGameComplete += GameCompleteEvent;
    }

    private void Start()
    {
        playerController.touchDisabled = false;
        if(gameMode == GameMode.online)
        {
            //runner = FindObjectOfType<NetworkRunner>();
        }
        else
        {
            playerController = GamePlayController.instance;
            playerController.manager = this;
            SetupPlayers();
        }
    }

    private void SetupPlayers()
    {
        player1 = new Player("Player 1", p1Balls);
        player2 = new Player(gameMode == GameMode.cpu ? "CPU" : "Player 2", p2Balls);
        players[Users.player1] = player1;
        players[Users.player2] = player2;
        StartCoroutine(Toss());
    }

    public void SetupOnlinePlayers(string p1, string p2, NetworkPlayer netPlayer1, NetworkPlayer netPlayer2)
    {
        player1 = new Player(p1, p1Balls, netPlayer1);
        player2 = new Player(p2, p2Balls, netPlayer2);
        players[Users.player1] = player1;
        players[Users.player2] = player2;
        StartCoroutine(TossOnline());
    }

    private IEnumerator Toss()
    {
        Debug.Log("toss tt");
        yield return null;
        int rand = UnityEngine.Random.Range(0, 2);
        currentPlayer = (Users)rand;

        playerController.isWaiting = true;
        playerIndicator[rand].SetActive(true);

        Popup.instance.CreatePopup($"{players[currentPlayer].name} will break");
        //yield return LerpTextAlpha(tossTxt, 0, 1, 2);

        placeBallPop.SetActive(players[currentPlayer].name != "CPU");
        //tossTxt.gameObject.SetActive(false);
        if (players[currentPlayer].name == "CPU")
        {
            playerController.StartCPUMode();
        }
    }

    private IEnumerator TossOnline()
    {
        Debug.Log("toss tt");
        yield return null;
        int rand = UnityEngine.Random.Range(0, 2);
        currentPlayer = (Users)rand;

        playerController.manager = this;
        players[currentPlayer].netPlayer.IsTurn = true;
        players[GetOpponent(currentPlayer)].netPlayer.IsTurn = false;

        playerController.isWaiting = true;
        playerIndicator[rand].SetActive(true);

        Popup.instance.CreatePopup($"{players[currentPlayer].name} will break");
        //yield return LerpTextAlpha(tossTxt, 0, 1, 2);

        if(IsLocalPlayersTurn())
        {
            placeBallPop.SetActive(players[currentPlayer].name != "CPU");
        }
        //tossTxt.gameObject.SetActive(false);
    }

    private IEnumerator LerpTextAlpha(Text text, float startAlpha, float endAlpha, float duration)
    {
        float time = 0;
        Color color = text.color;
        while (time < duration)
        {
            time += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, lerpCurve.Evaluate(time / duration));
            text.color = color;
            yield return null;
        }
    }

    public void GameCompleteEvent(Users winner)
    {
        restartPanel.SetActive(true);
        restartPanel.transform.GetChild(0).GetComponent<Text>().text = $"{winner} WINS";
    }

    public void SetBallImages()
    {
        bool isCurrentPlayerStripe = players[currentPlayer].BallType == BallBehaviour.BallType.stripe;
        UpdatePlayerBalls(isCurrentPlayerStripe);
    }

    private void UpdatePlayerBalls(bool isCurrentPlayerStripe)
    {
        Sprite[] currentBalls = isCurrentPlayerStripe ? stripeBalls : solidBalls;
        Sprite[] opponentBalls = isCurrentPlayerStripe ? solidBalls : stripeBalls;

        for (int i = 0; i < 7; i++)
        {
            players[currentPlayer].playerBalls[i].sprite = currentBalls[i];
            players[GetOpponent(currentPlayer)].playerBalls[i].sprite = opponentBalls[i];
        }
    }

    public void ClosePlacePop()
    {
        if (!playerController.CueBallValid()) return;

        foreach (GameObject ball in playerController.balls)
        {
            ball.GetComponent<Rigidbody>().isKinematic = false;
        }

        playerController.isWaiting = false;
        poolCam.gameState = PoolCamBehaviour.GameState.Aim;
        placeBallPop.SetActive(false);
        startPanel.SetActive(false);
        playerController.StartGame();
    }

    public bool IsLocalPlayersTurn()
    {
        return players[currentPlayer].netPlayer.IsMyTurn;
    }

    public void SwitchTurn()
    {
        if (gameMode == GameMode.online && runner.IsServer)
        {
            players[currentPlayer].netPlayer.IsTurn = false;
            currentPlayer = GetOpponent(currentPlayer);
            players[currentPlayer].netPlayer.IsTurn = true;
        }
        else
        {
            currentPlayer = GetOpponent(currentPlayer);
        }
    }

    public void SetIndicator()
    {
        playerIndicator[(int)currentPlayer].SetActive(true);
        playerIndicator[(int)GetOpponent(currentPlayer)].SetActive(false);
    }

    public void PlayBallSound(AudioClip clip)
    {
        if (ballhitCount % 2 == 0 && !playerController.isFoul && playerController.isWaiting)
        {
            playerController.gameAudio.PlayOneShot(clip);
        }
    }

    public bool CorrectBallPlayed(BallBehaviour.BallType ballType)
    {
        if (ballType == BallBehaviour.BallType.black)
        {
            if (players[currentPlayer].pocketedBalls.Count==7)
            {
                return true;
            }
        }
        if (ballType == players[currentPlayer].BallType)
        {
            return true;
        }
        return false;
    }

    public Users GetOpponent(Users player) => player == Users.player1 ? Users.player2 : Users.player1;

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex == 0 ? 0 : 1);

}

#region helperClass
public enum PlayerType { Local, Remote, CPU }
[System.Serializable]
public class Player
{
    public string name;
    public BallBehaviour.BallType BallType;
    public List<GameObject> pocketedBalls = new();
    public Image[] playerBalls;
    public NetworkPlayer netPlayer;
    public bool IsMyTurn => netPlayer != null && netPlayer.IsMyTurn;

    public Player(string name, Image[] playerBalls)
    {
        this.name = name;
        this.playerBalls = playerBalls;
    }

    public Player(string name, Image[] playerBalls, NetworkPlayer netPlayer)
    {
        this.name = name;
        this.playerBalls = playerBalls;
        this.netPlayer = netPlayer;
    }

    public void DisableBallImage(int ballCode) => playerBalls[ballCode].enabled = false;
}
#endregion

