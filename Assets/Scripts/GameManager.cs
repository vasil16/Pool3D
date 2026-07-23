using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] CameraController poolCam;
    [SerializeField] AnimationCurve lerpCurve;
    [SerializeField] public GameObject placeBallPop, startPanel, restartPanel, messageObject;
    [SerializeField] Sprite[] solidBalls;
    [SerializeField] Sprite[] stripeBalls;
    [SerializeField] Image[] p1Balls, p2Balls;
    [SerializeField] GameObject[] playerIndicator;
    [SerializeField] Text messageText;
    [SerializeField] public TextMeshProUGUI player1Txt, player2Txt;
    public string localPlayerName;
    public NetworkRunner runner;

    public int ballhitCount;

    [Space(10)] public Player player1, player2;
    [Space(10)] public List<GameObject> pocketedBalls;

    public Dictionary<Users, Player> players = new();
    public Users currentPlayer;
    public GameMode gameMode;
    [SerializeField] public GameState gameState;

    public Action<Users> onGameComplete;

    private GameController playerController;

    public enum GameMode { offline, cpu, online }
    public enum Users { player1, player2 }
    public enum GameState
    {
        Break,
        Hit,
        Waiting,
        Aim,
        Reset
    };

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
        playerController = GameController.instance;
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


    private IEnumerator Toss()
    {
        Debug.Log("toss tt");
        yield return null;
        // int rand = UnityEngine.Random.Range(0, 2);
        int rand = 1;
        currentPlayer = (Users)rand;

        playerController.isWaiting = true;
        playerIndicator[rand].SetActive(true);

        StartCoroutine(Popup($"{players[currentPlayer].name} will break", poolCam.SetInitialCameraAnim));

        yield return new WaitForSeconds(3f);
        
        //yield return LerpTextAlpha(tossTxt, 0, 1, 2);

        placeBallPop.SetActive(players[currentPlayer].name != "CPU");
        //tossTxt.gameObject.SetActive(false);
        if (players[currentPlayer].name == "CPU")
        {
            playerController.StartCPUMode();
        }
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
        player1Txt.text = player1.BallType + "";
        player2Txt.text = player2.BallType + "";
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

    public GameObject placeBallButton;

    public void ClosePlacePop()
    {
        if (!playerController.CueBallValid()) return;
        
        
        placeBallButton.SetActive(true);
        

        foreach (GameObject ball in playerController.balls)
        {
            ball.GetComponent<Rigidbody>().isKinematic = false;
        }

        playerController.isWaiting = false;
        gameState = GameState.Aim;
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
            gameFx.PlayOneShot(clip);
        }
    }

    [SerializeField] AudioSource gameFx;

    public void PlaySound(AudioClip clip)
    {
        gameFx.PlayOneShot(clip);
    }

    public IEnumerator Popup(string message, Action callBack = null)
    {
        yield return null;

        messageText.text = message;
        messageObject.SetActive(true);

        RectTransform rect = messageObject.GetComponent<RectTransform>();

        rect.DOAnchorPosY(79, 0.6f).SetEase(Ease.InOutCubic).OnComplete(() =>
        {
            rect.DOAnchorPosY(-93, 0.6f)
                .SetEase(Ease.InOutCubic)
                .SetDelay(1.3f)
                .OnComplete(() =>
                {
                    messageObject.SetActive(false);
                    callBack?.Invoke();
                });
        });
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

