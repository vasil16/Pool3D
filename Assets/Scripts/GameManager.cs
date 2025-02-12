using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] PoolCamBehaviour poolCam;
    [SerializeField] public Text tossTxt;
    [SerializeField] AnimationCurve lerpCurve;
    [SerializeField] public GameObject placeBallPop, startPanel, restartPanel;
    [SerializeField] Sprite[] solidBalls;
    [SerializeField] Sprite[] stripeBalls;
    [SerializeField] Image[] p1Balls, p2Balls;

    public int ballhitCount;

    [Space(10)] public Player player1, player2;
    [Space(10)] public List<GameObject> pocketedBalls;

    public Dictionary<CurrentPlayer, Player> players = new();
    public CurrentPlayer currentPlayer;
    public GameMode gameMode;

    public Action<CurrentPlayer> onGameComplete;

    private GamePlayController playerController;

    public enum GameMode { players, cpu }
    public enum CurrentPlayer { player1, player2 }

    private void Awake()
    {
        instance = this;
        playerController = GamePlayController.instance;
        playerController.manager = this;
    }

    private void OnEnable()
    {
        onGameComplete += GameCompleteEvent;
    }

    private void Start()
    {
        SetupPlayers();        
    }

    private void SetupPlayers()
    {
        player1 = new Player("Player 1", p1Balls);
        player2 = new Player(gameMode == GameMode.cpu ? "CPU" : "Player 2", p2Balls);
        players[CurrentPlayer.player1] = player1;
        players[CurrentPlayer.player2] = player2;
        StartCoroutine(Toss());
    }

    private IEnumerator Toss()
    {
        Debug.Log("toss tt");
        yield return null;
        int rand = UnityEngine.Random.Range(0, 2);
        currentPlayer = (CurrentPlayer)rand;
        playerController.isWaiting = true;
        playerController.playerIndicator[rand].SetActive(true);

        tossTxt.text = $"{players[currentPlayer].name} will break";
        yield return LerpTextAlpha(tossTxt, 0, 1, 2);

        placeBallPop.SetActive(players[currentPlayer].name != "CPU");
        tossTxt.gameObject.SetActive(false);
        if (players[currentPlayer].name=="CPU")
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

    public void GameCompleteEvent(CurrentPlayer winner)
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

    public void SetCpu()
    {
        //foreach(GameObject ball in PoolMain.instance.balls)
        //{
        //    if(ball.GetComponent<BallBehaviour>().ballType == player2.BallType)
        //    {
        //        PoolMain.instance.cpuBalls.Add(ball);
        //    }
        //}
        //playerController.StartGame();
    }

    public void PlayBallSound(AudioClip clip)
    {
        if(ballhitCount%2==0)
        {
            playerController.gameAudio.PlayOneShot(clip);
        }
    }

    public CurrentPlayer GetOpponent(CurrentPlayer player) => player == CurrentPlayer.player1 ? CurrentPlayer.player2 : CurrentPlayer.player1;

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex == 0 ? 0 : 1);

    [System.Serializable]
    public class Player
    {
        public string name;
        public BallBehaviour.BallType BallType;
        public List<GameObject> pocketedBalls = new();
        public Image[] playerBalls;

        public Player(string name, Image[] playerBalls)
        {
            this.name = name;
            this.playerBalls = playerBalls;
        }

        public void DisableBallImage(int ballCode) => playerBalls[ballCode].enabled = false;
    }
}
