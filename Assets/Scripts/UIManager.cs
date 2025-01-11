using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject homePanel, gameplayPanel, gameStartPanel, gameLogic;
    int index;

    public void PlayClick(int index)
    {
        foreach(Transform t in homePanel.transform)
        {
            this.index = index;            
            if(t.GetComponent<UIItemExit>())
                t.GetComponent<UIItemExit>().ExitWithCallBack(PlayButtonCallback);
        }
    }

    void PlayButtonCallback()
    {
        homePanel.SetActive(false);
        gameplayPanel.SetActive(true);
        gameStartPanel.SetActive(true);
        gameLogic.SetActive(true);
        GameManager.instance.gameMode = index == 0 ? GameManager.GameMode.players : GameManager.GameMode.cpu;
        GameObject.FindObjectOfType<PoolCamBehaviour>().SetInitialCameraAnim();
        if (index==1)
        {
            Debug.Log("vs cpu");
            GameManager.instance.SetCpu();
        }
    }
   
}
