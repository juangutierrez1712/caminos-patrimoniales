using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class MinimapWebTile : MonoBehaviour
{
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private float updateIntervalSeconds = 3f;

    // Zoom 17 = nivel de calle, bueno para peatones
    private const int ZOOM = 17;
    private const int TILE_SIZE = 256;

    private double lastLat = 0;
    private double lastLon = 0;
    private float timer = 0;
    private bool isLoading = false;

    void Update()
    {
        if (LocationManager.Instance == null || !LocationManager.Instance.IsReady) return;

        timer += Time.deltaTime;
        if (timer >= updateIntervalSeconds && !isLoading)
        {
            double lat = LocationManager.Instance.Latitude;
            double lon = LocationManager.Instance.Longitude;

            // Solo actualizar si nos movimos más de 5 metros
            if (CalculateDistance(lastLat, lastLon, lat, lon) > 5.0 || lastLat == 0)
            {
                lastLat = lat;
                lastLon = lon;
                timer = 0;
                StartCoroutine(LoadMapTile(lat, lon));
            }
        }
    }

    IEnumerator LoadMapTile(double lat, double lon)
    {
        isLoading = true;

        // Usamos OpenStreetMap (gratuito, sin API key)
        // Calculamos el tile X,Y para el zoom dado
        int tileX = LonToTileX(lon, ZOOM);
        int tileY = LatToTileY(lat, ZOOM);

        string url = $"https://tile.openstreetmap.org/{ZOOM}/{tileX}/{tileY}.png";

        using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            // Header requerido por OSM
            req.SetRequestHeader("User-Agent", "CaminosPatrimoniales/0.1 Unity");
            yield return req.SendWebRequest();

            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
                minimapImage.texture = tex;

                // Rotar la imagen para que el norte quede arriba
                // (los tiles OSM ya vienen con norte arriba)
                minimapImage.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                Debug.LogWarning("Minimap tile error: " + req.error);
            }
        }

        isLoading = false;
    }

    // Convierte longitud → tile X de OSM
    int LonToTileX(double lon, int zoom)
    {
        return (int)((lon + 180.0) / 360.0 * (1 << zoom));
    }

    // Convierte latitud → tile Y de OSM
    int LatToTileY(double lat, int zoom)
    {
        double latR = lat * Mathf.PI / 180.0;
        return (int)((1.0 - Mathf.Log((float)(Mathf.Tan((float)latR) +
               1.0 / Mathf.Cos((float)latR))) / Mathf.PI) / 2.0 * (1 << zoom));
    }

    double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        if (lat1 == 0 && lon1 == 0) return 999;
        double R = 6371000;
        double dLat = (lat2 - lat1) * Mathf.PI / 180f;
        double dLon = (lon2 - lon1) * Mathf.PI / 180f;
        double a = Mathf.Sin((float)(dLat / 2)) * Mathf.Sin((float)(dLat / 2)) +
                   Mathf.Cos((float)(lat1 * Mathf.PI / 180f)) *
                   Mathf.Cos((float)(lat2 * Mathf.PI / 180f)) *
                   Mathf.Sin((float)(dLon / 2)) * Mathf.Sin((float)(dLon / 2));
        return R * 2 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt(1f - (float)a));
    }
}