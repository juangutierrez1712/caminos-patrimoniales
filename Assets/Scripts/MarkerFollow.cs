using UnityEngine;

public class MarkerFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = new Vector3(
            target.position.x,
            1f,
            target.position.z
        );
    }
}