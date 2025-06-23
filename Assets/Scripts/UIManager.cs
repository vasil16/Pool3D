using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    
    [SerializeField] RectTransform homePanel,playPanel , gameplayPanel, gameStartPanel;
    [SerializeField] GameObject gameLogic;
    int index;

    [Header("MainMenu")]
    [SerializeField] RectTransform title, button1, button2;
    [SerializeField] Vector2 titleActivePos, titlehiddenPos, button1ActivePos, button1HiddenPos, button2ActivePos, button2HiddenPos;

    private void Start()
    {
        //AnimateUIEntry();
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
        foreach(Transform t in homePanel.transform)
        {
            this.index = index;
            //AnimateUIExit(PlayButtonCallback);
            PlayButtonCallback();
        }
    }

    void PlayButtonCallback()
    {
        homePanel.gameObject.SetActive(false);
        gameplayPanel.gameObject.SetActive(true);
        gameStartPanel.gameObject.SetActive(true);
        gameLogic.gameObject.SetActive(true);
        GameManager.instance.gameMode = index == 0 ? GameManager.GameMode.players : GameManager.GameMode.cpu;
        //GameObject.FindObjectOfType<PoolCamBehaviour>().SetInitialCameraAnim();
        if (index==1)
        {
            Debug.Log("vs cpu");
            GameManager.instance.SetCpu();
        }
    }

    public void OpenPlayPanel()
    {
        homePanel.DOAnchorPos(new Vector2(-2000, 0), 0.7f).SetEase(Ease.InBack).OnComplete(()=>homePanel.gameObject.SetActive(false));
        playPanel.gameObject.SetActive(true);
        playPanel.DOAnchorPos(new Vector2(0, 0), 0.7f).SetEase(Ease.InBack);
    }
   
}
