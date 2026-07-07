using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    public static RouteData SelectedRoute { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void SelectRoute(RouteData route)
    {
        SelectedRoute = route;
    }

    public static void GoToRouteList()
    {
        SceneManager.LoadScene("RouteList");
    }

    public static void GoToRoutePreview(RouteData route)
    {
        SelectedRoute = route;
        SceneManager.LoadScene("RoutePreview");
    }

    public static void GoToARNavigation()
    {
        SceneManager.LoadScene("ARNavigation");
    }

    public static void GoToSplash()
    {
        SceneManager.LoadScene("SplashScreen");
    }
}
