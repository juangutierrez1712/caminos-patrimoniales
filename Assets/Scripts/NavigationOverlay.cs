using TMPro;
using UnityEngine;

public class NavigationOverlay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private PathProjector pathProjector;

    void Update()
    {
        if (pathProjector == null)
            pathProjector = FindFirstObjectByType<PathProjector>();
        if (pathProjector == null) return;

        // Ocultar overlay si el panel de POI está abierto
        POIPanel panel = FindFirstObjectByType<POIPanel>();
        if (panel != null && panel.gameObject.activeSelf)
        {
            distanceText.text = "";
            instructionText.text = "";
            return;
        }

        float dist = pathProjector.GetDistanceToTarget();

        // Distancia
        if (dist >= 1000f)
            distanceText.text = $"{dist / 1000f:F1} km";
        else
            distanceText.text = $"{dist:F0} m";

        // Nombre dinámico del POI actual
        string nombrePOI = POIManager.Instance?.CurrentPOI?.nombre ?? "el siguiente punto";

        // Instrucción
        if (!LocationManager.Instance.IsReady)
            instructionText.text = "Esperando GPS...";
        else if (!POIManager.Instance?.IsLoaded ?? true)
            instructionText.text = "Cargando recorrido...";
        else if (dist <= 20f)
        {
            instructionText.text = $"¡Llegaste! Toca el pin rojo";
            distanceText.color = Color.green;
        }
        else if (dist <= 50f)
            instructionText.text = $"{nombrePOI} está muy cerca";
        else
        {
            distanceText.color = new Color(0f, 0.706f, 0.847f); // #00B4D8
            instructionText.text = $"Sigue hacia {nombrePOI}";
        }
    }
}