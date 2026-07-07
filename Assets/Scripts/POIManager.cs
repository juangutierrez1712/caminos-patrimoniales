using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class POIManager : MonoBehaviour
{
    public static POIManager Instance { get; private set; }

    // URL del Feature Service — capa 0, todos los campos, ordenados por 'orden'
    private const string FEATURE_SERVICE_URL =
    "https://services.arcgis.com/8DAUcrpQcpyLMznu/arcgis/rest/services/" + "POIs_CaminosPatrimoniales/FeatureServer/0/query" + "?where=1%3D1&outFields=*&orderByFields=orden&f=json&outSR=4326";

    public List<POIData> POIs { get; private set; } = new List<POIData>();
    public bool IsLoaded { get; private set; } = false;
    public int CurrentIndex { get; private set; } = 0;

    // POI actual según CurrentIndex
    public POIData CurrentPOI => (POIs.Count > 0) ? POIs[CurrentIndex] : null;

    // Evento que dispara cuando los POIs están cargados
    public event Action OnPOIsLoaded;

    // Evento que dispara cuando el POI activo cambia
    public event Action<POIData> OnPOIChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(LoadPOIsFromAGOL());
    }

    // ── Carga desde AGOL ─────────────────────────────────────────────────────

    private IEnumerator LoadPOIsFromAGOL()
    {
        Debug.Log("POIManager: iniciando carga desde AGOL...");

        using UnityWebRequest req = UnityWebRequest.Get(FEATURE_SERVICE_URL);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("POIManager: error de red — " + req.error);
            yield break;
        }

        ParseResponse(req.downloadHandler.text);
    }

    private void ParseResponse(string json)
    {
        try
        {
            // Unity no parsea listas raíz directamente; usamos wrapper
            AGOLResponse response = JsonUtility.FromJson<AGOLResponse>(json);

            if (response == null || response.features == null)
            {
                Debug.LogError("POIManager: respuesta AGOL nula o sin features.");
                return;
            }

            POIs.Clear();

            foreach (var feature in response.features)
            {
                POIData poi = new POIData
                {
                    orden = feature.attributes.orden,
                    nombre = feature.attributes.nombre,
                    descripcion = feature.attributes.descripcion,
                    historia = feature.attributes.historia,
                    fotoUrl = feature.attributes.foto_url,
                    // AGOL devuelve geometría en WGS84: x = lon, y = lat
                    longitude = feature.geometry.x,
                    latitude = feature.geometry.y
                };
                POIs.Add(poi);
            }

            // Ordenar por campo 'orden' por seguridad
            POIs.Sort((a, b) => a.orden.CompareTo(b.orden));

            IsLoaded = true;
            Debug.Log($"POIManager: {POIs.Count} POIs cargados correctamente.");

            OnPOIsLoaded?.Invoke();
            OnPOIChanged?.Invoke(CurrentPOI);
        }
        catch (Exception e)
        {
            Debug.LogError("POIManager: error al parsear JSON — " + e.Message);
        }
    }

    // ── Navegación secuencial ─────────────────────────────────────────────────

    public void NextPOI()
    {
        if (CurrentIndex < POIs.Count - 1)
        {
            CurrentIndex++;
            OnPOIChanged?.Invoke(CurrentPOI);
            Debug.Log($"POIManager: avanzando a POI {CurrentIndex + 1} — {CurrentPOI.nombre}");
        }
        else
        {
            Debug.Log("POIManager: ya estás en el último POI del recorrido.");
        }
    }

    public void PreviousPOI()
    {
        if (CurrentIndex > 0)
        {
            CurrentIndex--;
            OnPOIChanged?.Invoke(CurrentPOI);
            Debug.Log($"POIManager: regresando a POI {CurrentIndex + 1} — {CurrentPOI.nombre}");
        }
    }

    public bool IsLastPOI() => CurrentIndex == POIs.Count - 1;
    public bool IsFirstPOI() => CurrentIndex == 0;

    public void ResetToFirstPOI()
    {
        CurrentIndex = 0;
        if (IsLoaded) OnPOIChanged?.Invoke(CurrentPOI);
    }

    // ── Clases de deserialización JSON (JsonUtility) ──────────────────────────

    [Serializable] private class AGOLResponse { public List<Feature> features; }
    [Serializable] private class Feature { public Attributes attributes; public Geometry geometry; }
    [Serializable] private class Geometry { public double x; public double y; }

    [Serializable]
    private class Attributes
    {
        public int orden;
        public string nombre;
        public string descripcion;
        public string historia;
        public string foto_url;
    }



}