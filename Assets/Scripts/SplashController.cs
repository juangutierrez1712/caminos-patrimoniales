using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class SplashController : MonoBehaviour
{
    void Start()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        Debug.Log("Botones encontrados en escena: " + buttons.Length);

        foreach (Button btn in buttons)
        {
            if (btn.name == "ExploreButton")
            {
                btn.onClick.AddListener(OnExplorePressed);
                Debug.Log("Listener registrado en: " + btn.name);
                return;
            }
        }

        Debug.LogError("No se encontró ExploreButton en la escena");
    }

    void OnExplorePressed()
    {
        Debug.Log("Botón presionado — navegando a RouteList");
        SceneLoader.GoToRouteList();
    }
}