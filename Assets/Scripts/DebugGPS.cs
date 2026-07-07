using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DebugGPS : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gpsText;
    [SerializeField] private PathProjector pathProjector;

    void Update()
    {
        if (gpsText == null) return;

        if (LocationManager.Instance == null)
        {
            gpsText.text = "ERROR: No LocationManager";
            return;
        }

        if (!LocationManager.Instance.IsReady)
        {
            gpsText.text = "Esperando GPS...\n" + Input.location.status;
            return;
        }

        // Buscar PathProjector si no está asignado
        if (pathProjector == null)
        {
            pathProjector = FindFirstObjectByType<PathProjector>();
        }

        string waypointInfo = "";
        if (pathProjector != null)
        {
            waypointInfo = $"\nDist POI: {pathProjector.GetDistanceToTarget():F1}m\nWaypoint: {(pathProjector.IsWaypointActive() ? "ACTIVO" : "INACTIVO")}";
        }

        gpsText.text = $"Lat: {LocationManager.Instance.Latitude:F6}\n" +
                       $"Lon: {LocationManager.Instance.Longitude:F6}\n" +
                       $"Acc: {LocationManager.Instance.Accuracy:F1}m\n" +
                       $"Compás: {LocationManager.Instance.Heading:F1}°" +
                       waypointInfo;

        string poiInfo = POIManager.Instance != null && POIManager.Instance.IsLoaded ? $"\nPOIs cargados: {POIManager.Instance.POIs.Count}\nActual: {POIManager.Instance.CurrentPOI?.nombre} ({POIManager.Instance.CurrentIndex + 1}/{POIManager.Instance.POIs.Count})": "\nPOIs: cargando...";
        gpsText.text += poiInfo;

    }
}