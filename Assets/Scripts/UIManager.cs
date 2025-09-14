using System;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using Fusion;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    
    [SerializeField] RectTransform homePanel,playPanel , gameplayPanel, gameStartPanel;
    [SerializeField] GameObject gameManager, multiplayerPanel, networkObject;
    [SerializeField] CustomButton multiplierMatchButton;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] TextMeshProUGUI statusText;    
    int index;

    [Header("MainMenu")]
    [SerializeField] RectTransform title, button1, button2;
    [SerializeField] Vector2 titleActivePos, titlehiddenPos, button1ActivePos, button1HiddenPos, button2ActivePos, button2HiddenPos;

    private void Awake()
    {
        if(instance==null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        //AnimateUIEntry();
    }

    public void test()
    {
        Debug.Log("hii");
    }

    void AnimateUIEntry()
    {
        title.DOAnchorPos(titleActivePos,1f).SetEase(Ease.InOutBack);
        button1.DOAnchorPos(button1ActivePos, .8f).SetDelay(0.4f);
        button2.DOAnchorPos(button2ActivePos, .8f).SetDelay(0.2f);
    }

    void AnimateUIExit(Action callBack=null)
    {
        title.DOAnchorPos(titlehiddenPos, 1f).SetEase(Ease.InOutBack).SetDelay(0.4f);
        button1.DOAnchorPos(button1HiddenPos, .8f).SetDelay(0.1f);
        button2.DOAnchorPos(button2HiddenPos, .8f).SetDelay(0.2f).OnComplete(()=>
        {
            callBack.Invoke();
        });
    }

    public void PlayClick(int index)
    {        
        this.index = index;
        //AnimateUIExit(PlayButtonCallback);        
        PlayButtonCallback();
    }

    [SerializeField] GameObject blurOverlay;
    [SerializeField] Material blurMaterial;

    void PlayButtonCallback()
    {
        homePanel.gameObject.SetActive(false);
        gameplayPanel.gameObject.SetActive(true);
        gameStartPanel.gameObject.SetActive(true);
        gameManager.gameObject.SetActive(true);
        GameManager.instance.gameMode = index == 0 ? GameManager.GameMode.offline : GameManager.GameMode.cpu;
    }

    public void PlayOnlineCallback()
    {
        gameManager.SetActive(true);
        GameManager.instance.gameMode = GameManager.GameMode.online;
        multiplayerPanel.gameObject.SetActive(true);
        networkObject.gameObject.SetActive(true);             
    }

    public void FindMatch()
    {
        string playerName = nameInput.text.Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            statusText.text = "Please enter a name.";
            return;
        }

        GameManager.instance.localPlayerName = playerName;
        statusText.text = "Searching for opponent...";
        nameInput.interactable = false;
        multiplierMatchButton.interactable = false;
        StartCoroutine(InitOnlineGame()); // Starts Fusion networking
    }

    IEnumerator InitOnlineGame()
    {
        yield return new WaitForSeconds(0.3f); // Optional delay
    }


    public void StartOnline()
    {
        statusText.text = "match found, starting game";
        homePanel.gameObject.SetActive(false);
        multiplayerPanel.gameObject.SetActive(false);
        gameplayPanel.gameObject.SetActive(true);
        gameStartPanel.gameObject.SetActive(true);        
    }

    [SerializeField] AudioSource gameFx;
    [SerializeField] AudioClip uiFx;

    public void PlayUIFx()
    {
        gameFx.PlayOneShot(uiFx);
    }


    public void OpenPlayPanel()
    {
        homePanel.DOAnchorPos(new Vector2(-2000, 0), 0.7f).SetEase(Ease.InBack).OnComplete(()=>homePanel.gameObject.SetActive(false));
        playPanel.gameObject.SetActive(true);
        playPanel.DOAnchorPos(new Vector2(0, 0), 0.7f).SetEase(Ease.InBack);
    }

    public void Pause()
    {
        Time.timeScale = 0;
    }

    public void Resume()
    {
        Time.timeScale = 1;
    }

}
