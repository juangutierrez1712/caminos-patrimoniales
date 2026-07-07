using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class RoutePreviewController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI routeNameText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Transform poiListContent;
    [SerializeField] private GameObject poiListItemPrefab;
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    void Start()
    {
        RouteData route = SceneLoader.SelectedRoute;

        if (route == null)
        {
            Debug.LogWarning("RoutePreview: no hay recorrido seleccionado.");
            SceneLoader.GoToRouteList();
            return;
        }

        routeNameText.text = route.nombre;
        statsText.text = $"\u23f1 {route.duracionMin / 60}h {route.duracionMin % 60}m  " +
                         $"\ud83d\udccd {route.distanciaM / 1000f:F1} km  " +
                         $"\ud83d\udccc {route.numPOIs} puntos";

        startButton.onClick.AddListener(OnStartPressed);
        backButton.onClick.AddListener(() => SceneLoader.GoToRouteList());

        if (!string.IsNullOrEmpty(route.poiServiceUrl))
            StartCoroutine(LoadPOIList(route.poiServiceUrl));
    }

    IEnumerator LoadPOIList(string serviceUrl)
    {
        string url = serviceUrl + "/0/query?where=1%3D1&outFields=orden,nombre&orderByFields=orden&f=json&outSR=4326";
        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("RoutePreview: error cargando lista de POIs — " + req.error);
            yield break;
        }

        BuildPOIList(req.downloadHandler.text);
    }

    void BuildPOIList(string json)
    {
        try
        {
            POIListResponse resp = JsonUtility.FromJson<POIListResponse>(json);
            if (resp?.features == null) return;

            foreach (var f in resp.features)
            {
                GameObject item = Instantiate(poiListItemPrefab, poiListContent);
                var tmp = item.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = $"{f.attributes.orden}. {f.attributes.nombre}";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("RoutePreview parse error: " + e.Message);
        }
    }

    void OnStartPressed()
    {
        POIManager.Instance?.ResetToFirstPOI();
        SceneLoader.GoToARNavigation();
    }

    [System.Serializable] private class POIListResponse { public List<POIFeature> features; }
    [System.Serializable] private class POIFeature { public POIAttribs attributes; }
    [System.Serializable] private class POIAttribs { public int orden; public string nombre; }
}