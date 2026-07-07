using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class POIPanel : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI nombreText;
    [SerializeField] private TextMeshProUGUI historiaText;

    [Header("Imagen")]
    [SerializeField] private RawImage fotoImage;

    [Header("Botones")]
    [SerializeField] private Button btnAnterior;
    [SerializeField] private Button btnSiguiente;
    [SerializeField] private Button btnCerrar;

    private void Start()
    {
        // Asignar listeners de botones
        btnAnterior.onClick.AddListener(OnAnterior);
        btnSiguiente.onClick.AddListener(OnSiguiente);
        btnCerrar.onClick.AddListener(HidePanel);

        // Panel empieza oculto
        gameObject.SetActive(false);
    }

    // ── Interfaz pública ──────────────────────────────────────────────────────

    public void ShowPanel(POIData poi)
    {
        if (poi == null) return;

        gameObject.SetActive(true);
        PopulatePanel(poi);
        Debug.Log($"POIPanel: mostrando '{poi.nombre}'");
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }

    // ── Lógica interna ────────────────────────────────────────────────────────

    private void PopulatePanel(POIData poi)
    {
        nombreText.text = poi.nombre;

        // Mostrar historia o descripción como fallback
        historiaText.text = !string.IsNullOrEmpty(poi.historia)
            ? poi.historia
            : poi.descripcion;

        // Actualizar estado de botones
        btnAnterior.interactable = !POIManager.Instance.IsFirstPOI();
        btnSiguiente.interactable = !POIManager.Instance.IsLastPOI();

        // Cambiar texto del último POI
        TextMeshProUGUI txtSiguiente = btnSiguiente.GetComponentInChildren<TextMeshProUGUI>();
        if (txtSiguiente != null)
            txtSiguiente.text = POIManager.Instance.IsLastPOI()
                ? "Fin del recorrido"
                : "Siguiente →";

        // Cargar imagen desde URL
        if (!string.IsNullOrEmpty(poi.fotoUrl))
            StartCoroutine(LoadImage(poi.fotoUrl));
        else
            fotoImage.color = new Color(0.1f, 0.2f, 0.3f); // placeholder oscuro
    }

    private IEnumerator LoadImage(string url)
    {
        fotoImage.color = new Color(0.1f, 0.2f, 0.3f); // placeholder mientras carga

        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            fotoImage.texture = DownloadHandlerTexture.GetContent(req);
            fotoImage.color = Color.white;
        }
        else
        {
            Debug.LogWarning("POIPanel: no se pudo cargar la imagen — " + req.error);
        }
    }

    // ── Navegación ────────────────────────────────────────────────────────────

    private void OnAnterior()
    {
        POIManager.Instance.PreviousPOI();
        PopulatePanel(POIManager.Instance.CurrentPOI);
        HidePanel();
    }

    private void OnSiguiente()
    {
        if (POIManager.Instance.IsLastPOI())
        {
            Debug.Log("POIPanel: recorrido completado.");
            // Aquí puedes mostrar una pantalla de fin de recorrido en Semana 6
            HidePanel();
            return;
        }

        POIManager.Instance.NextPOI();
        HidePanel();
        // El PathProjector y POIPinController se actualizarán automáticamente
        // gracias al evento OnPOIChanged de POIManager
    }
}