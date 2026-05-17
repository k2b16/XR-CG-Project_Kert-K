using UnityEngine;
public class WaterBlob : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Final visual radius once fully lifted.")]
    public float fullRadius = 0.08f;

    [Header("Wobble")]
    [Tooltip("How strongly hand acceleration squashes/stretches the blob.")]
    public float wobbleStrength = 0.05f;
    [Tooltip("How fast the wobble springs back to spherical.")]
    public float wobbleDamping = 8f;

    private float growth = 0f;
    private Vector3 lastTrackedPos;
    private Vector3 lastVelocity;
    private Vector3 currentSquash = Vector3.one;

    void Start()
    {
        lastTrackedPos = transform.position;
        ApplyScale();
    }

    public void SetGrowth(float t) { growth = Mathf.Clamp01(t); ApplyScale(); }

    public void SetMotion(Vector3 trackedPos)
    {
        Vector3 vel = (trackedPos - lastTrackedPos) / Mathf.Max(Time.deltaTime, 1e-5f);
        Vector3 accel = (vel - lastVelocity) / Mathf.Max(Time.deltaTime, 1e-5f);
        Vector3 dir = accel.sqrMagnitude > 1e-4f ? accel.normalized : Vector3.zero;
        float mag = Mathf.Clamp01(accel.magnitude * wobbleStrength * 0.05f);
        Vector3 target = Vector3.one + dir * mag - new Vector3(Mathf.Abs(dir.y) + Mathf.Abs(dir.z),
                                                                Mathf.Abs(dir.x) + Mathf.Abs(dir.z),
                                                                Mathf.Abs(dir.x) + Mathf.Abs(dir.y)) * mag * 0.5f;

        currentSquash = Vector3.Lerp(currentSquash, target, 1f - Mathf.Exp(-wobbleDamping * Time.deltaTime));
        lastVelocity = vel;
        lastTrackedPos = trackedPos;

        ApplyScale();
    }

    void Update()
    {
        currentSquash = Vector3.Lerp(currentSquash, Vector3.one, 1f - Mathf.Exp(-wobbleDamping * 0.5f * Time.deltaTime));
        ApplyScale();
    }

    void ApplyScale()
    {
        float baseScale = fullRadius * 2f * growth;
        transform.localScale = new Vector3(
            baseScale * currentSquash.x,
            baseScale * currentSquash.y,
            baseScale * currentSquash.z);
    }
}
