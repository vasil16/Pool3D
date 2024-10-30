using System.Collections.Generic;
using UnityEngine;

public class MaterialPooling : MonoBehaviour
{

    [SerializeField] List<Renderer> poolObject;
    [SerializeField] List<Texture> poolTexture;
    [SerializeField] List<Color> poolColor;
    MaterialPropertyBlock _propBlock;

    void Start()
    {
        _propBlock = new MaterialPropertyBlock();
        SetBallTextures();
    }

    void SetBallTextures()
    {
        for (int i = 0; i < poolObject.Count; i++)
        {
            Renderer ballRenderer = poolObject[i];

            // Apply a unique color to each ball (for example purposes)
            Texture newTexture = poolTexture[i];
            Color newColor = poolColor[i];
            SetBallTexture(ballRenderer, newTexture, newColor);
        }
    }

    void SetBallTexture(Renderer renderer, Texture newTexture, Color newColor)
    {
        _propBlock.Clear();

        _propBlock.SetTexture("_BaseMap", newTexture);
        _propBlock.SetColor("_BaseColor", newColor);

        renderer.SetPropertyBlock(_propBlock);
    }

}
