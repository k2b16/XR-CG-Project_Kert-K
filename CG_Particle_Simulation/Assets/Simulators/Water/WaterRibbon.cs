using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaterRibbon : MonoBehaviour
{
    [Header("References")]
    public Transform source;       // Controller transform
    public Transform attractor;    // Center orb

    [Header("Ribbon Shape")]
    [Range(8, 64)] public int pointCount = 24;
    public float segmentLength = 0.04f;

    [Header("Motion")]
    public float sourceFollowSpeed = 25f;
    public float springStrength = 20f;
    public float damping = 8f;

    [Header("Orb Behaviour")]
    public float attractStrength = 6f;
    public float orbitRadius = 0.18f;
    public float swirlStrength = 1.8f;
    public Vector3 swirlAxis = Vector3.up;

    [Header("Noise")]
    public float noiseStrength = 0.015f;
    public float noiseSpeed = 1.5f;

    private LineRenderer lr;
    private Vector3[] points;
    private Vector3[] velocities;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        points = new Vector3[pointCount];
        velocities = new Vector3[pointCount];
        lr.positionCount = pointCount;
    }

    void Start()
    {
        Vector3 startPos = source != null ? source.position : transform.position;

        for (int i = 0; i < pointCount; i++)
        {
            points[i] = startPos;
            velocities[i] = Vector3.zero;
        }

        UpdateRenderer();
    }

    void LateUpdate()
    {
        if (source == null || attractor == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // First point smoothly follows controller
        points[0] = Vector3.Lerp(points[0], source.position, 1f - Mathf.Exp(-sourceFollowSpeed * dt));

        for (int i = 1; i < pointCount; i++)
        {
            Vector3 current = points[i];
            Vector3 prev = points[i - 1];

            // Keep chain spacing
            Vector3 toPrev = prev - current;
            Vector3 dirToPrev = toPrev.sqrMagnitude > 0.00001f ? toPrev.normalized : Vector3.forward;
            Vector3 desiredPos = prev - dirToPrev * segmentLength;

            Vector3 force = Vector3.zero;

            // Spring toward previous point
            force += (desiredPos - current) * springStrength;

            // Orb attraction + orbit shell
            Vector3 toCenter = attractor.position - current;
            float dist = toCenter.magnitude;

            if (dist > 0.00001f)
            {
                Vector3 centerDir = toCenter / dist;

                float radiusError = dist - orbitRadius;
                force += centerDir * radiusError * attractStrength;

                Vector3 tangent = Vector3.Cross(centerDir, swirlAxis).normalized;
                force += tangent * swirlStrength;
            }

            // Small animated wobble
            float t = Time.time * noiseSpeed + i * 0.21f;
            Vector3 noise = new Vector3(
                Mathf.PerlinNoise(t, 0.13f) - 0.5f,
                Mathf.PerlinNoise(0.31f, t) - 0.5f,
                Mathf.PerlinNoise(t, t * 0.67f) - 0.5f
            ) * 2f * noiseStrength;

            force += noise;

            // Integrate
            velocities[i] += force * dt;
            velocities[i] *= Mathf.Exp(-damping * dt);
            points[i] += velocities[i] * dt;
        }

        UpdateRenderer();
    }

    void UpdateRenderer()
    {
        lr.positionCount = pointCount;
        lr.SetPositions(points);
    }
}