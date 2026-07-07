using UnityEngine;

public class PlayerDotRotator : MonoBehaviour
{
    void Update()
    {
        if (LocationManager.Instance == null || !LocationManager.Instance.IsReady) return;

        // Rota el punto según el heading del compás
        // En UI, la rotación Z negativa = giro horario = Norte arriba
        float heading = LocationManager.Instance.Heading;
        transform.rotation = Quaternion.Euler(0, 0, -heading);
    }
}