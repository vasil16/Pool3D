using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class Popup : MonoBehaviour
{
    [SerializeField] private GameObject popupPrefab;

    public static Popup instance;

    private void Awake()
    {
        instance = this;
    }

    public void CreatePopup(string message)
    {
        GameObject pop = Instantiate(popupPrefab);
        pop.transform.SetParent(this.transform);
        RectTransform popTransform = pop.GetComponent<RectTransform>();
        popTransform.anchoredPosition = new Vector2(0, -770f);
        pop.GetComponent<Text>().text = message;
        popTransform.DOAnchorPos(new Vector2 (0,-192f), 1.2f).SetEase(Ease.InOutBack).OnComplete(() =>

        {
            popTransform.DOAnchorPos(new Vector2(0, -770f), .4f).SetDelay(2f).SetEase(Ease.OutCubic).OnComplete(()=>Destroy(pop)) ;
        });
    }
}
