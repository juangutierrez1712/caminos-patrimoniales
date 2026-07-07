using UnityEngine;

public class MinimapRouteLine : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float lineHeight = 2f;

    void Awake()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
    }

    void OnEnable()
    {
        if (RouteService.Instance != null)
            RouteService.Instance.OnRouteUpdated += Redraw;
    }

    void OnDisable()
    {
        if (RouteService.Instance != null)
            RouteService.Instance.OnRouteUpdated -= Redraw;
    }

    void LateUpdate()
    {
        Redraw();
    }

    void Redraw()
    {
        if (RouteService.Instance == null || !RouteService.Instance.HasRoute) return;
        if (LocationManager.Instance == null || !LocationManager.Instance.IsReady) return;
        if (Camera.main == null) return;

        var points = RouteService.Instance.RoutePoints;
        lineRenderer.positionCount = points.Count;

        double baseLat = LocationManager.Instance.Latitude;
        double baseLon = LocationManager.Instance.Longitude;
        Transform cam = Camera.main.transform;

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            double dLat = p.lat - baseLat;
            double dLon = p.lon - baseLon;

            float offsetZ = (float)(dLat * 111139.0);
            float offsetX = (float)(dLon * 111139.0 * System.Math.Cos(baseLat * System.Math.PI / 180.0));

            lineRenderer.SetPosition(i, new Vector3(cam.position.x + offsetX, lineHeight, cam.position.z + offsetZ));
        }
    }
}
