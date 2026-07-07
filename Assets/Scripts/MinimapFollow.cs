using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    [SerializeField] private Transform target; // arrastra XR Origin aquí
    private float height = 100f;

    void LateUpdate()
    {
        if (target == null) return;

        // La cámara sigue al jugador en XZ, siempre a la misma altura
        transform.position = new Vector3(
            target.position.x,
            height,
            target.position.z
        );

        // Rota para que el Norte siempre quede arriba en el minimapa
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}