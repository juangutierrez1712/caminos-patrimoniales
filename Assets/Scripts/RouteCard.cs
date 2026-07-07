using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RouteCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI routeNameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private RawImage cardImage;
    [SerializeField] private Button selectButton;

    private RouteData routeData;

    public void Setup(RouteData data)
    {
        routeData = data;
        routeNameText.text = data.nombre;
        descText.text = data.descripcionCorta;
        durationText.text = $"{data.duracionMin / 60}h {data.duracionMin % 60}m";
        distanceText.text = $"{data.distanciaM / 1000f:F1} km";
        difficultyText.text = data.dificultad;

        selectButton.onClick.AddListener(OnSelectPressed);

        if (!string.IsNullOrEmpty(data.imagenUrl))
            StartCoroutine(LoadImage(data.imagenUrl));
    }

    void OnSelectPressed()
    {
        SceneLoader.GoToRoutePreview(routeData);
    }

    IEnumerator LoadImage(string url)
    {
        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            cardImage.texture = ((DownloadHandlerTexture)req.downloadHandler).texture;
    }
}