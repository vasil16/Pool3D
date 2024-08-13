using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject homePanel, gameplayPanel, gameStartPanel;

    public void PlayClick()
    {
        foreach(Transform t in homePanel.transform)
        {
            if(t.GetComponent<UIItemExit>())
                t.GetComponent<UIItemExit>().ExitWithCallBack(PlayButtonCallback);
        }
    }

    void PlayButtonCallback()
    {
        homePanel.SetActive(false);
        gameplayPanel.SetActive(true);
        gameStartPanel.SetActive(true);
    }
   
}
