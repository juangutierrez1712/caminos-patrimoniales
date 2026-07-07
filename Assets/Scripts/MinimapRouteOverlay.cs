using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapRouteOverlay : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform segmentContainer;
    [SerializeField] private GameObject segmentPrefab;

    [Header("Ajustes visuales")]
    [SerializeField] private Color lineColor = new Color(0f, 0.706f, 0.847f); // #00B4D8
    [SerializeField] private float lineThickness = 4f;
    [SerializeField] private float maxDrawRadiusPixels = 140f;
    [SerializeField] private int maxSegments = 60;

    // Calibración: si la línea se ve desfasada respecto al PlayerDot/POIMarker,
    // ajusta este valor (normalmente cerca de 1.0). Compensa que el RawImage
    // puede no medir exactamente 256x256 px como el tile fuente.
    [SerializeField] private float mapPixelScale = 1.0f;

    // Deben coincidir con las constantes de MinimapWebTile.cs
    private const int ZOOM = 17;
    private const int TILE_SIZE = 256;

    private List<RectTransform> pool = new List<RectTransform>();
    private List<GeoPoint> downsampledPoints = new List<GeoPoint>();

    void OnEnable()
    {
        if (RouteService.Instance != null)
            RouteService.Instance.OnRouteUpdated += RebuildDownsampledRoute;

        RebuildDownsampledRoute();
    }

    void OnDisable()
    {
        if (RouteService.Instance != null)
            RouteService.Instance.OnRouteUpdated -= RebuildDownsampledRoute;
    }

    void RebuildDownsampledRoute()
    {
        downsampledPoints.Clear();
        if (RouteService.Instance == null || !RouteService.Instance.HasRoute) return;

        var full = RouteService.Instance.RoutePoints;
        int step = Mathf.Max(1, full.Count / maxSegments);

        for (int i = 0; i < full.Count; i += step)
            downsampledPoints.Add(full[i]);

        var last = full[full.Count - 1];
        var lastAdded = downsampledPoints[downsampledPoints.Count - 1];
        if (lastAdded.lat != last.lat || lastAdded.lon != last.lon)
            downsampledPoints.Add(last);
    }

    void LateUpdate()
    {
        if (LocationManager.Instance == null || !LocationManager.Instance.IsReady) return;
        if (downsampledPoints.Count < 2) { HideAllSegments(); return; }

        double userLat = LocationManager.Instance.Latitude;
        double userLon = LocationManager.Instance.Longitude;

        int usedSegments = 0;
        for (int i = 0; i < downsampledPoints.Count - 1; i++)
        {
            Vector2 pA = LatLonToPixelOffset(downsampledPoints[i].lat, downsampledPoints[i].lon, userLat, userLon);
            Vector2 pB = LatLonToPixelOffset(downsampledPoints[i + 1].lat, downsampledPoints[i + 1].lon, userLat, userLon);

            // Saltar segmentos completamente fuera del área visible del minimapa
            if (pA.magnitude > maxDrawRadiusPixels && pB.magnitude > maxDrawRadiusPixels)
                continue;

            DrawSegment(usedSegments, pA, pB);
            usedSegments++;
        }

        for (int i = usedSegments; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }

    void DrawSegment(int index, Vector2 a, Vector2 b)
    {
        RectTransform seg = GetOrCreateSegment(index);
        seg.gameObject.SetActive(true);

        Vector2 diff = b - a;
        float length = Mathf.Max(diff.magnitude, 0.01f);
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        seg.anchoredPosition = a;
        seg.sizeDelta = new Vector2(length, lineThickness);
        seg.localRotation = Quaternion.Euler(0, 0, angle);
    }

    RectTransform GetOrCreateSegment(int index)
    {
        if (index < pool.Count) return pool[index];

        GameObject go = Instantiate(segmentPrefab, segmentContainer);
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = lineColor;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);

        pool.Add(rt);
        return rt;
    }

    void HideAllSegments()
    {
        foreach (var seg in pool)
            seg.gameObject.SetActive(false);
    }

    // Diferencia lat/lon → píxeles de pantalla (mapa north-up, mismo zoom que MinimapWebTile)
    Vector2 LatLonToPixelOffset(double lat, double lon, double baseLat, double baseLon)
    {
        double worldX = LonToWorldPixelX(lon);
        double worldY = LatToWorldPixelY(lat);
        double baseWorldX = LonToWorldPixelX(baseLon);
        double baseWorldY = LatToWorldPixelY(baseLat);

        float dx = (float)(worldX - baseWorldX) * mapPixelScale;
        float dy = (float)(worldY - baseWorldY) * mapPixelScale;

        // Invertir Y: en tiles +Y es hacia el sur (abajo); en UI, +Y es hacia arriba
        return new Vector2(dx, -dy);
    }

    double LonToWorldPixelX(double lon)
    {
        return (lon + 180.0) / 360.0 * TILE_SIZE * (1 << ZOOM);
    }

    double LatToWorldPixelY(double lat)
    {
        double latRad = lat * System.Math.PI / 180.0;
        return (1.0 - System.Math.Log(System.Math.Tan(latRad) + 1.0 / System.Math.Cos(latRad)) / System.Math.PI)
               / 2.0 * TILE_SIZE * (1 << ZOOM);
    }
}