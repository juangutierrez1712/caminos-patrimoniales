using UnityEngine;
using Debug = UnityEngine.Debug;

public class POIMarkerFollow : MonoBehaviour
{
    // Este script mueve el POIMarker del minimapa a la posición
    // del POI actual en coordenadas de Unity (relativo al jugador)

    void LateUpdate()
    {
        if (POIManager.Instance == null || !POIManager.Instance.IsLoaded) return;
        if (LocationManager.Instance == null || !LocationManager.Instance.IsReady) return;
        if (Camera.main == null) return;

        POIData poi = POIManager.Instance.CurrentPOI;
        if (poi == null) return;

        // Calcular desplazamiento en metros
        double dLat = poi.latitude - LocationManager.Instance.Latitude;
        double dLon = poi.longitude - LocationManager.Instance.Longitude;

        // 1 grado latitud ≈ 111139 metros
        float offsetZ = (float)(dLat * 111139.0);
        float offsetX = (float)(dLon * 111139.0 *
            System.Math.Cos(LocationManager.Instance.Latitude * System.Math.PI / 180.0));

        Transform cam = Camera.main.transform;
        transform.position = new Vector3(
            cam.position.x + offsetX,
            1f,
            cam.position.z + offsetZ
        );
    }
}