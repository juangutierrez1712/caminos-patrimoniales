using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public struct GeoPoint
{
    public double lat;
    public double lon;
}

public class Maneuver
{
    public string text;
    public double lengthMeters;       // convertido desde millas
    public double cumulativeMeters;   // distancia acumulada de ruta hasta el FIN de esta maniobra
}

public class RouteService : MonoBehaviour
{
    public static RouteService Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private string apiKey = "PEGA_AQUI_TU_API_KEY_ARCGIS";
    [SerializeField] private float deviationThresholdMeters = 18f;

    private const string ROUTE_URL =
        "https://route-api.arcgis.com/arcgis/rest/services/World/Route/NAServer/Route_World/solve";

    // Travel mode "Walking Time" estándar del servicio World Route.
    // Si el servicio devuelve error de travelMode inválido, revisa el
    // JSON de error (lo logueamos abajo) y ajusta este bloque.
    private const string WALKING_TRAVEL_MODE_JSON =
        "{\"attributeParameterValues\":[{\"attributeName\":\"Walking\",\"parameterName\":\"Restriction Usage\",\"value\":\"PROHIBITED\"}],\"description\":\"Follows paths and roads that allow pedestrian traffic.\",\"distanceAttributeName\":\"Kilometers\",\"id\":\"caFAgDGii0IuNodF\",\"impedanceAttributeName\":\"WalkTime\",\"name\":\"Walking Time\",\"restrictionAttributeNames\":[\"Avoid Roads Unsuitable for Pedestrians\",\"Preferred for Pedestrians\",\"Walking\"],\"simplificationTolerance\":2,\"simplificationToleranceUnits\":\"esriMeters\",\"timeAttributeName\":\"WalkTime\",\"type\":\"WALK\",\"useHierarchy\":false,\"uturnAtJunctions\":\"esriNFSBAllowBacktrack\"}";

    public List<double> PathCumulativeMeters { get; private set; } = new List<double>();
    public List<GeoPoint> RoutePoints { get; private set; } = new List<GeoPoint>();
    public List<Maneuver> Maneuvers { get; private set; } = new List<Maneuver>();
    public bool HasRoute => RoutePoints.Count > 0;

    public event Action OnRouteUpdated;

    private bool isLoading = false;
    private double lastDestLat = double.NaN;
    private double lastDestLon = double.NaN;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Decisión de recálculo ──────────────────────────────────────────────

    public bool NeedsRecalculation(double curLat, double curLon, double destLat, double destLon)
    {
        if (isLoading) return false;

        bool destinationChanged =
            double.IsNaN(lastDestLat) ||
            CalculateDistance(destLat, destLon, lastDestLat, lastDestLon) > 5.0;

        if (destinationChanged) return true;
        if (!HasRoute) return true;

        float deviation = DistanceToRouteMeters(curLat, curLon);
        return deviation > deviationThresholdMeters;
    }

    public void RequestRoute(double startLat, double startLon, double destLat, double destLon)
    {
        if (isLoading) return;
        StartCoroutine(SolveRouteCoroutine(startLat, startLon, destLat, destLon));
    }

    // ── Llamada al servicio ──────────────────────────────────────────────────

    private IEnumerator SolveRouteCoroutine(double startLat, double startLon, double destLat, double destLon)
    {
        isLoading = true;

        string stops = string.Format(CultureInfo.InvariantCulture,
            "{0},{1};{2},{3}", startLon, startLat, destLon, destLat);

        WWWForm form = new WWWForm();
        form.AddField("f", "json");
        form.AddField("token", apiKey);
        form.AddField("stops", stops);
        form.AddField("returnDirections", "true");
        form.AddField("returnRoutes", "true");
        form.AddField("outputLines", "esriNAOutputLineTrueShape");
        form.AddField("directionsLanguage", "es");
        form.AddField("outSR", "4326");
        form.AddField("travelMode", WALKING_TRAVEL_MODE_JSON);

        using UnityWebRequest req = UnityWebRequest.Post(ROUTE_URL, form);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("RouteService: error de red — " + req.error);
            isLoading = false;
            yield break;
        }

        ParseRouteResponse(req.downloadHandler.text, destLat, destLon);
        isLoading = false;
    }

    private void ParseRouteResponse(string json, double destLat, double destLon)
    {
        try
        {
            var root = SimpleJson.Parse(json) as Dictionary<string, object>;
            if (root == null)
            {
                Debug.LogError("RouteService: respuesta JSON no es un objeto válido.");
                return;
            }

            if (root.ContainsKey("error"))
            {
                Debug.LogError("RouteService: el servicio devolvió error — " + json);
                return;
            }

            // ── Polilínea real ──
            var routes = root["routes"] as Dictionary<string, object>;
            var features = routes?["features"] as List<object>;
            var feature0 = features?[0] as Dictionary<string, object>;
            var geometry = feature0?["geometry"] as Dictionary<string, object>;
            var paths = geometry?["paths"] as List<object>;
            var path0 = paths?[0] as List<object>;

            if (path0 == null)
            {
                Debug.LogError("RouteService: respuesta sin geometry.paths");
                return;
            }

            var newRoutePoints = new List<GeoPoint>();
            foreach (var coordObj in path0)
            {
                var coord = coordObj as List<object>;
                double lon = Convert.ToDouble(coord[0]);
                double lat = Convert.ToDouble(coord[1]);
                newRoutePoints.Add(new GeoPoint { lat = lat, lon = lon });
            }

            // ── Distancias acumuladas a lo largo de la polilínea (para ubicar al usuario) ──
            var newCumulative = new List<double> { 0.0 };
            for (int i = 1; i < newRoutePoints.Count; i++)
            {
                double segDist = CalculateDistance(
                    newRoutePoints[i - 1].lat, newRoutePoints[i - 1].lon,
                    newRoutePoints[i].lat, newRoutePoints[i].lon);
                newCumulative.Add(newCumulative[i - 1] + segDist);
            }

            // ── Maniobras (turn-by-turn) — solo texto + longitud, sin coordenadas ──
            var newManeuvers = new List<Maneuver>();
            double runningMeters = 0;
            if (root.ContainsKey("directions"))
            {
                var directions = root["directions"] as List<object>;
                var dir0 = directions?.Count > 0 ? directions[0] as Dictionary<string, object> : null;
                var dirFeatures = dir0?["features"] as List<object>;

                if (dirFeatures != null)
                {
                    foreach (var fObj in dirFeatures)
                    {
                        var f = fObj as Dictionary<string, object>;
                        var attrs = f?["attributes"] as Dictionary<string, object>;
                        if (attrs == null) continue;

                        double lengthMiles = attrs.ContainsKey("length") ? Convert.ToDouble(attrs["length"]) : 0;
                        double lengthMeters = lengthMiles * 1609.344; // el servicio devuelve "length" en millas

                        runningMeters += lengthMeters;

                        newManeuvers.Add(new Maneuver
                        {
                            text = attrs.ContainsKey("text") ? attrs["text"]?.ToString() ?? "" : "",
                            lengthMeters = lengthMeters,
                            cumulativeMeters = runningMeters
                        });
                    }
                }
            }

            RoutePoints = newRoutePoints;
            PathCumulativeMeters = newCumulative;
            Maneuvers = newManeuvers;
            lastDestLat = destLat;
            lastDestLon = destLon;

            Debug.Log($"RouteService: ruta cargada — {RoutePoints.Count} vértices, {Maneuvers.Count} maniobras");
            OnRouteUpdated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("RouteService: error al parsear JSON — " + e.Message);
        }
    }

    // ── Navegación por posición a lo largo de la ruta ──────────────────────────

    // Distancia del usuario proyectada sobre la polilínea (en metros desde el inicio)
    private double GetDistanceAlongPath(double curLat, double curLon, out int segmentIndex, out Vector2 projectedLocal)
    {
        segmentIndex = 0;
        projectedLocal = Vector2.zero;

        if (RoutePoints.Count < 2) return 0;

        double bestDist = double.MaxValue;
        double bestAlong = 0;

        for (int i = 0; i < RoutePoints.Count - 1; i++)
        {
            Vector2 a = LatLonToLocalMeters(RoutePoints[i].lat, RoutePoints[i].lon, curLat, curLon);
            Vector2 b = LatLonToLocalMeters(RoutePoints[i + 1].lat, RoutePoints[i + 1].lon, curLat, curLon);

            Vector2 ab = b - a;
            float t = Vector2.Dot(Vector2.zero - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f);
            t = Mathf.Clamp01(t);
            Vector2 closest = a + ab * t;
            float d = Vector2.Distance(Vector2.zero, closest);

            if (d < bestDist)
            {
                bestDist = d;
                segmentIndex = i;
                projectedLocal = closest;
                double segLen = PathCumulativeMeters[i + 1] - PathCumulativeMeters[i];
                bestAlong = PathCumulativeMeters[i] + segLen * t;
            }
        }

        return bestAlong;
    }

    // Punto un poco más adelante del usuario, siguiendo la polilínea real (para la flecha AR)
    public GeoPoint? GetLookaheadTarget(double curLat, double curLon, float lookaheadMeters = 10f)
    {
        if (RoutePoints.Count < 2) return null;

        double alongDist = GetDistanceAlongPath(curLat, curLon, out _, out _);
        double targetDist = alongDist + lookaheadMeters;

        for (int i = 0; i < PathCumulativeMeters.Count - 1; i++)
        {
            if (PathCumulativeMeters[i + 1] >= targetDist)
            {
                double segLen = PathCumulativeMeters[i + 1] - PathCumulativeMeters[i];
                float t = segLen > 0.001 ? (float)((targetDist - PathCumulativeMeters[i]) / segLen) : 0f;

                double lat = Mathf.Lerp((float)RoutePoints[i].lat, (float)RoutePoints[i + 1].lat, t);
                double lon = Mathf.Lerp((float)RoutePoints[i].lon, (float)RoutePoints[i + 1].lon, t);
                return new GeoPoint { lat = lat, lon = lon };
            }
        }

        // Ya pasamos el último vértice — apunta al último punto de la ruta
        var last = RoutePoints[RoutePoints.Count - 1];
        return new GeoPoint { lat = last.lat, lon = last.lon };
    }

    // Texto de instrucción correspondiente a dónde va el usuario ahora mismo
    public string GetCurrentInstructionText(double curLat, double curLon)
    {
        if (Maneuvers.Count == 0) return "";

        double alongDist = GetDistanceAlongPath(curLat, curLon, out _, out _);

        foreach (var m in Maneuvers)
        {
            if (alongDist <= m.cumulativeMeters) return m.text;
        }
        return Maneuvers[Maneuvers.Count - 1].text;
    }

    // ── Utilidades geométricas ──────────────────────────────────────────────

    private float DistanceToRouteMeters(double lat, double lon)
    {
        if (RoutePoints.Count < 2) return float.MaxValue;

        float minDist = float.MaxValue;
        for (int i = 0; i < RoutePoints.Count - 1; i++)
        {
            Vector2 a = LatLonToLocalMeters(RoutePoints[i].lat, RoutePoints[i].lon, lat, lon);
            Vector2 b = LatLonToLocalMeters(RoutePoints[i + 1].lat, RoutePoints[i + 1].lon, lat, lon);
            float d = DistancePointToSegment(Vector2.zero, a, b);
            if (d < minDist) minDist = d;
        }
        return minDist;
    }

    // Convierte un punto lat/lon a metros relativos a un origen (baseLat/baseLon)
    private Vector2 LatLonToLocalMeters(double lat, double lon, double baseLat, double baseLon)
    {
        double dLat = lat - baseLat;
        double dLon = lon - baseLon;
        float z = (float)(dLat * 111139.0);
        float x = (float)(dLon * 111139.0 * Math.Cos(baseLat * Math.PI / 180.0));
        return new Vector2(x, z);
    }

    private float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f);
        t = Mathf.Clamp01(t);
        Vector2 closest = a + ab * t;
        return Vector2.Distance(p, closest);
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
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