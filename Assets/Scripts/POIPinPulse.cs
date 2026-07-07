using UnityEngine;

public class POIPinPulse : MonoBehaviour
{
    [SerializeField] private Transform pinHead;
    public float baseScale = 0.6f;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.04f;

    void Update()
    {
        if (pinHead == null) return;
        float pulse = baseScale + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        pinHead.localScale = Vector3.one * pulse;
    }
}