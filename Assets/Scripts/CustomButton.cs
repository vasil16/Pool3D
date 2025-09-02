using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButton : Button, IPointerClickHandler
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        UIManager.instance.PlayUIFx();
    }
}
