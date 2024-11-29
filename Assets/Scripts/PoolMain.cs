using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PoolMain : MonoBehaviour
{

    public List<GameObject> balls;
    public List<Texture> ballTexture;

    MaterialPropertyBlock _propBlock;


    void Start()
    {
        _propBlock = new MaterialPropertyBlock();
        SetBallTextures();
        PlayerPrefs.DeleteAll();
    }

    void SetBallTextures()
    {
        for (int i=0; i< balls.Count;i++)
        {
            Renderer ballRenderer = balls[i].GetComponent<Renderer>();

            // Apply a unique color to each ball (for example purposes)
            Texture newTexture = ballTexture[i];
            SetBallTexture(ballRenderer, newTexture);
        }
    }

    void SetBallTexture(Renderer renderer, Texture newTexture)
    {
        _propBlock.Clear();

        _propBlock.SetTexture("_BaseMap", newTexture);

        renderer.SetPropertyBlock(_propBlock);
    }
}
