using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PathProjector : MonoBehaviour
{
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private int dotCount = 8;
    [SerializeField] private float dotSpacing = 0.6f;

    private GameObject[] dots;
    private bool dotsActive = false;
    private float distanceToTarget = 0;

    void Start()
    {
        dots = new GameObject[dotCount];
        for (int i = 0; i < dotCount; i++)
        {
            dots[i] = Instantiate(dotPrefab, Vector3.zero, Quaternion.identity);
            dots[i].SetActive(false);
        }
    }

    void Update()
    {
        if (POIManager.Instance == null || !POIManager.Instance.IsLoaded) return;
        if (LocationManager.Instance == null || !LocationManager.Instance.IsReady) return;

        var poi = POIManager.Instance.CurrentPOI;

        distanceToTarget = (float)CalculateDistance(
            LocationManager.Instance.Latitude,
            LocationManager.Instance.Longitude,
            poi.latitude, poi.longitude
        );

        // Pedir/actualizar la ruta real caminable si hace falta
        if (RouteService.Instance != null)
        {
            if (RouteService.Instance.NeedsRecalculation(
                    LocationManager.Instance.Latitude, LocationManager.Instance.Longitude,
                    poi.latitude, poi.longitude))
            {
                RouteService.Instance.RequestRoute(
                    LocationManager.Instance.Latitude, LocationManager.Instance.Longitude,
                    poi.latitude, poi.longitude);
            }
        }

        if (distanceToTarget > 20f)
            ShowDots(poi.latitude, poi.longitude);
        else
            HideDots();
    }

    void ShowDots(double fallbackLat, double fallbackLon)
    {
        double targetLat = fallbackLat;
        double targetLon = fallbackLon;

        // Si ya hay ruta real calculada, apuntamos a un punto un poco
        // adelante siguiendo la polilínea real, no directo al POI en línea recta
        if (RouteService.Instance != null)
        {
            GeoPoint? lookahead = RouteService.Instance.GetLookaheadTarget(
                LocationManager.Instance.Latitude,
                LocationManager.Instance.Longitude,
                10f
            );

            if (lookahead.HasValue)
            {
                targetLat = lookahead.Value.lat;
                targetLon = lookahead.Value.lon;
            }
        }

        float bearing = (float)CalculateBearing(
            LocationManager.Instance.Latitude,
            LocationManager.Instance.Longitude,
            targetLat, targetLon
        );

        float compassHeading = LocationManager.Instance.Heading;
        float relativeAngle = bearing - compassHeading;

        Vector3 direction = Quaternion.Euler(0, relativeAngle, 0) * Vector3.forward;
        direction.Normalize();

        Transform cam = Camera.main.transform;
        float groundY = cam.position.y - 1.4f;

        for (int i = 0; i < dotCount; i++)
        {
            float distance = (i + 1) * dotSpacing;
            float scale = Mathf.Lerp(0.35f, 0.15f, (float)i / dotCount);

            Vector3 pos = cam.position + direction * distance;
            pos.y = groundY;

            dots[i].transform.position = pos;
            dots[i].transform.localScale = new Vector3(scale, scale, scale);
            dots[i].SetActive(true);
        }

        dotsActive = true;
    }

    void HideDots()
    {
        foreach (var dot in dots)
            if (dot != null) dot.SetActive(false);
        dotsActive = false;
    }

    void OnDestroy()
    {
        foreach (var dot in dots)
            if (dot != null) Destroy(dot);
    }

    double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double lat1R = lat1 * Math.PI / 180.0;
        double lat2R = lat2 * Math.PI / 180.0;
        double y = Math.Sin(dLon) * Math.Cos(lat2R);
        double x = Math.Cos(lat1R) * Math.Sin(lat2R) -
                   Math.Sin(lat1R) * Math.Cos(lat2R) * Math.Cos(dLon);
        double bearing = Math.Atan2(y, x) * 180.0 / Math.PI;
        return (bearing + 360.0) % 360.0;
    }

    double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371000;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public float GetDistanceToTarget() => distanceToTarget;
    public bool IsWaypointActive() => dotsActive;
}