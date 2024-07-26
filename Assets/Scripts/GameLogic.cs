using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour
{
    [SerializeField] PoolCamBehaviour poolCam;
    [SerializeField] public Text tossTxt;
    [SerializeField] AnimationCurve lerpCurve;
    [SerializeField] public GameObject placeBallPop, startPanel, restartPanel;
    [SerializeField] Sprite[] solidBalls;
    [SerializeField] Sprite[] stripeBalls;
    [SerializeField] Image[] p1Balls, p2Balls;

    [Space(10)] public Player player1, player2;
    [Space(10)] public List<GameObject> pocketedBalls;
    [Space(10)] public Dictionary<CurrentPlayer, Player> players;

    public CurrentPlayer currentPlayer;

    public Action <CurrentPlayer> onGameComplete;

    private void OnEnable()
    {
        onGameComplete += GameCompleteEvent;
    }


    public static GameLogic instance;

    public enum CurrentPlayer
    {
        player1,
        player2
    }

    private void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        StartCoroutine(Toss());
        player1 = new Player();
        player1.playerBalls = p1Balls;
        player2 = new Player();
        player2.playerBalls = p2Balls;
        players = new Dictionary<CurrentPlayer, Player>();
        players[CurrentPlayer.player1] = player1;
        players[CurrentPlayer.player2] = player2;
    }

    public void GameCompleteEvent(CurrentPlayer player)
    {
        restartPanel.SetActive(true);
        restartPanel.transform.GetChild(0).GetComponent<Text>().text = player.ToString() + " WINS";
    }

    public void SetBallImages()
    {
        if (players[currentPlayer].BallType == BallBehaviour.BallType.stripe)
        {
            for(int i = 0; i < 7; i++)
            {
                players[currentPlayer].playerBalls[i].sprite = stripeBalls[i];
                players[GetOpponent(currentPlayer)].playerBalls[i].sprite = solidBalls[i];
            }
        }
        else
        {
            for (int i = 0; i < 7; i++)
            {
                players[currentPlayer].playerBalls[i].sprite = solidBalls[i];
                players[GetOpponent(currentPlayer)].playerBalls[i].sprite = stripeBalls[i];
            }
        }
    }

    IEnumerator Toss()
    {
        float time = 0;
        float duration = 1;
        Debug.Log("duration " + duration);
        PoolMain.instance.isWaiting = true;
        yield return new WaitForSecondsRealtime(2f);
        int rand = UnityEngine.Random.Range(0, 2); ;
        currentPlayer = (CurrentPlayer)rand;
        PoolMain.instance.playerIndicator[rand].SetActive(true);
        tossTxt.text = currentPlayer.ToString() + " will break";

        while(time<=duration)
        {
            time += Time.smoothDeltaTime;
            float t = lerpCurve.Evaluate(time);
            Color newColor = tossTxt.color;
            newColor.a = t;
            tossTxt.color = newColor;
        }
        
        Debug.Log("currPla " + currentPlayer);
        tossTxt.gameObject.SetActive(false);
        placeBallPop.SetActive(true);
    }

    public void ClosePlacePop()
    {
        PoolMain.instance.isWaiting = false;
        poolCam.gameState = PoolCamBehaviour.GameState.Aim;
        placeBallPop.SetActive(false);
        startPanel.SetActive(false);
        PoolMain.instance.StartGame();
    }

    public CurrentPlayer GetOpponent(CurrentPlayer player)
    {
        return player == CurrentPlayer.player1 ? CurrentPlayer.player2 : CurrentPlayer.player1;
    }

    public void Restart()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
            SceneManager.LoadScene(0);
        else
            SceneManager.LoadScene(1);
    }

    [System.Serializable]
    public class Player
    {
        public BallBehaviour.BallType BallType;
        public bool isLast;
        public List<GameObject> pocketedBalls = new();
        public Image[] playerBalls = new Image[7];
        public void DisableBallImage(int ballCode)
        {
            playerBalls[ballCode].enabled = false;
        }
    }
}
