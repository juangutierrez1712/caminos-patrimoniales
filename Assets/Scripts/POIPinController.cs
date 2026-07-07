using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class POIPinController : MonoBehaviour
{
    private POIPanel poiPanel;

    [SerializeField] private GameObject poiPinPrefab;
    [SerializeField] private float activationRadius = 500000f;
    [SerializeField] private float pinHeight = 1.5f;  

    private GameObject currentPin;
    private bool pinVisible = false;

    private bool ManagerReady =>
        POIManager.Instance != null && POIManager.Instance.IsLoaded;

    void Start()
    {
        // Suscribirse al cambio de POI para resetear el pin
        if (POIManager.Instance != null)
            POIManager.Instance.OnPOIChanged += OnPOIChanged;

        poiPanel = FindFirstObjectByType<POIPanel>(FindObjectsInactive.Include);
    }

    void OnDestroy()
    {
        if (POIManager.Instance != null)
            POIManager.Instance.OnPOIChanged -= OnPOIChanged;
    }

    void OnPOIChanged(POIData poi)
    {
        // Al cambiar de POI, ocultar el pin anterior
        HidePin();
    }

    void Update()
    {
        if (!ManagerReady) return;
        if (LocationManager.Instance == null || !LocationManager.Instance.IsReady) return;

        POIData poi = POIManager.Instance.CurrentPOI;
        if (poi == null) return;

        float dist = (float)CalculateDistance(
            LocationManager.Instance.Latitude,
            LocationManager.Instance.Longitude,
            poi.latitude, poi.longitude
        );

        if (dist <= activationRadius && !pinVisible)
            ShowPin();
        else if (dist > activationRadius && pinVisible)
            HidePin();

        if (pinVisible && currentPin != null)
            UpdatePinPosition(poi);

        // Detección de tap sobre el pin
        DetectTap();
    }


    void UpdatePinPosition(POIData poi)
    {
        float bearing = (float)CalculateBearing(
            LocationManager.Instance.Latitude,
            LocationManager.Instance.Longitude,
            poi.latitude, poi.longitude
        );

        float relativeAngle = bearing - LocationManager.Instance.Heading;
        Vector3 direction = Quaternion.Euler(0, relativeAngle, 0) * Vector3.forward;
        direction.Normalize();

        Transform cam = Camera.main.transform;
        Vector3 pos = cam.position + direction * 2f;  // más cerca
        pos.y = cam.position.y + 0.5f;  // ligeramente sobre el nivel de los ojos

        currentPin.transform.position = pos;

        // Log para verificar que se está actualizando
        Debug.Log($"Pin pos: {pos}, cam pos: {cam.position}");
    }


    void ShowPin()
    {
        if (currentPin != null) Destroy(currentPin);
        currentPin = Instantiate(poiPinPrefab, Vector3.zero, Quaternion.identity);
        pinVisible = true;

        string nombre = POIManager.Instance.CurrentPOI?.nombre ?? "POI";
        Debug.Log($"Pin mostrado: {nombre}");
    }

    void HidePin()
    {
        if (currentPin != null) { Destroy(currentPin); currentPin = null; }
        pinVisible = false;
    }

    void DetectTap()
    {
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        Ray ray = Camera.main.ScreenPointToRay(touch.position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider != null && hit.transform.root == currentPin?.transform)
            {
                Debug.Log("Pin tocado — abriendo panel");
                poiPanel?.ShowPanel(POIManager.Instance.CurrentPOI);
            }
        }
    }

    double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double lat1R = lat1 * Math.PI / 180.0;
        double lat2R = lat2 * Math.PI / 180.0;
        double y = Math.Sin(dLon) * Math.Cos(lat2R);
        double x = Math.Cos(lat1R) * Math.Sin(lat2R) -
                   Math.Sin(lat1R) * Math.Cos(lat2R) * Math.Cos(dLon);
        return (Math.Atan2(y, x) * 180.0 / Math.PI + 360.0) % 360.0;
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
}