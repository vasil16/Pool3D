using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    
    [SerializeField] GameObject homePanel, gameplayPanel, gameStartPanel, gameLogic;
    int index;

    [Header("MainMenu")]
    [SerializeField] RectTransform title, button1, button2;
    [SerializeField] Vector2 titleActivePos, titlehiddenPos, button1ActivePos, button1HiddenPos, button2ActivePos, button2HiddenPos;

    private void Start()
    {
        AnimateUIEntry();
    }

    void AnimateUIEntry()
    {
        title.DOAnchorPos(titleActivePos,1f).SetEase(Ease.Linear);
        button1.DOAnchorPos(button1ActivePos, .8f).SetDelay(0.4f);
        button2.DOAnchorPos(button2ActivePos, .8f).SetDelay(0.2f);
    }

    void AnimateUIExit(Action callBack=null)
    {
        title.DOAnchorPos(titlehiddenPos, 1f).SetEase(Ease.Linear).SetDelay(0.4f);
        button1.DOAnchorPos(button1HiddenPos, 1.2f);
        button2.DOAnchorPos(button2HiddenPos, 1.3f).SetDelay(0.2f).OnComplete(()=>
        {
            callBack.Invoke();
        });
    }

    public void PlayClick(int index)
    {
        foreach(Transform t in homePanel.transform)
        {
            this.index = index;
            AnimateUIExit(PlayButtonCallback);            
        }
    }

    void PlayButtonCallback()
    {
        homePanel.SetActive(false);
        gameplayPanel.SetActive(true);
        gameStartPanel.SetActive(true);
        gameLogic.SetActive(true);
        GameManager.instance.gameMode = index == 0 ? GameManager.GameMode.players : GameManager.GameMode.cpu;
        //GameObject.FindObjectOfType<PoolCamBehaviour>().SetInitialCameraAnim();
        if (index==1)
        {
            Debug.Log("vs cpu");
            GameManager.instance.SetCpu();
        }
    }
   
}
