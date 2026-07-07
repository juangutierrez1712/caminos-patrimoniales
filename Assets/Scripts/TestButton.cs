using UnityEngine;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

public class TestButton : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("CLICK DETECTADO EN EXPLOREBUTTON");
    }
}