using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using Debug = UnityEngine.Debug;

public class LocationManager : MonoBehaviour
{
    public static LocationManager Instance { get; private set; }

    [Header("Suavizado de brújula")]
    [SerializeField] private float headingSmoothingSpeed = 4f; // más alto = responde más rápido, menos suave

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public float Accuracy { get; private set; }
    public bool IsReady { get; private set; }
    public float Heading => smoothedHeading;

    private float smoothedHeading = 0f;
    private bool headingInitialized = false;

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
        Input.compass.enabled = true;

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

        // ── Suavizado de la brújula ──
        float rawHeading = Input.compass.trueHeading;

        if (!headingInitialized)
        {
            smoothedHeading = rawHeading;
            headingInitialized = true;
        }
        else
        {
            smoothedHeading = Mathf.LerpAngle(smoothedHeading, rawHeading, headingSmoothingSpeed * Time.deltaTime);
        }
    }

    private void OnDestroy()
    {
        if (Input.location.isEnabledByUser) Input.location.Stop();
    }
}