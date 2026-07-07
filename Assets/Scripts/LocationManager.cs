using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Android;
using Debug = UnityEngine.Debug;

public class LocationManager : MonoBehaviour
{
    public static LocationManager Instance { get; private set; }

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public float Accuracy { get; private set; }
    public bool IsReady { get; private set; }
    public float Heading => Input.compass.trueHeading;  // ← NUEVO

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(InitializeGPS());
    }

    private IEnumerator InitializeGPS()
    {
#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1f);
        }
#endif
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("GPS desactivado por el usuario.");
            IsReady = false;
            yield break;
        }

        Input.location.Start(5f, 2f);
        Input.compass.enabled = true;  // ← NUEVO

        int maxWait = 30;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait <= 0) { Debug.LogError("Timeout GPS"); IsReady = false; yield break; }
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("No se pudo obtener ubicación."); IsReady = false; yield break;
        }

        IsReady = true;
        Debug.Log("GPS listo. Lat: " + Input.location.lastData.latitude +
                  ", Lon: " + Input.location.lastData.longitude);
    }

    private void Update()
    {
        if (!IsReady) return;
        var data = Input.location.lastData;
        Latitude = data.latitude;
        Longitude = data.longitude;
        Accuracy = data.horizontalAccuracy;
    }

    private void OnDestroy()
    {
        if (Input.location.isEnabledByUser) Input.location.Stop();
    }
}