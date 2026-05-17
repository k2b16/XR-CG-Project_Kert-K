using UnityEngine;

public class WaterBlobChain : MonoBehaviour
{
    [Header("references")]
    public Transform source;
    public Transform attractor;
    public GameObject blobPrefab;

    [Header("chain")]
    [Range(8, 40)] public int pointCount = 18;
    public float segmentLength = 0.045f;

    [Header("motion")]
    public float sourceFollowSpeed = 18f;
    public float springStrength = 12f;
    public float damping = 10f;

    [Header("orb")]
    public float attractStrength = 4f;
    public float orbitRadius = 0.20f;
    public float swirlStrength = 0.7f;
    public Vector3 swirlAxis = Vector3.up;

    [Header("liquid")]
    public float baseBlobSize = 0.05f;
    public float endBlobSize = 0.018f;
    public float sizeWobble = 0.005f;
    public float positionWobble = 0.004f;
    public float wobbleSpeed = 1.2f;

    private Vector3[] points;
    private Vector3[] velocities;
    private Transform[] blobs;

    void Start()
    {
        if (blobPrefab == null)
        {
            Debug.LogError("blob prefab missing.");
            enabled = false;
            return;
        }

        points = new Vector3[pointCount];
        velocities = new Vector3[pointCount];
        blobs = new Transform[pointCount];

        Vector3 startPos = source != null ? source.position : transform.position;

        for (int i = 0; i < pointCount; i++)
        {
            points[i] = startPos;
            velocities[i] = Vector3.zero;

            GameObject blob = Instantiate(blobPrefab, startPos, Quaternion.identity, transform);
            blob.name = "Blob_" + i;
            blobs[i] = blob.transform;
        }
    }

    void LateUpdate()
    {
        if (source == null || attractor == null || blobs == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        points[0] = Vector3.Lerp(points[0], source.position, 1f - Mathf.Exp(-sourceFollowSpeed * dt));

        for (int i = 1; i < pointCount; i++)
        {
            Vector3 current = points[i];
            Vector3 prev = points[i - 1];

            Vector3 toPrev = prev - current;
            Vector3 dirToPrev = toPrev.sqrMagnitude > 0.00001f ? toPrev.normalized : Vector3.forward;
            Vector3 desiredPos = prev - dirToPrev * segmentLength;

            Vector3 force = Vector3.zero;
            force += (desiredPos - current) * springStrength;

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

            velocities[i] += force * dt;
            velocities[i] *= Mathf.Exp(-damping * dt);
            points[i] += velocities[i] * dt;
        }

        UpdateBlobs();
    }

    void UpdateBlobs()
    {
        for (int i = 0; i < pointCount; i++)
        {
            float t01 = (float)i / (pointCount - 1);

            float size = Mathf.Lerp(baseBlobSize, endBlobSize, t01);
            float wobble = Mathf.Sin(Time.time * wobbleSpeed + i * 0.45f) * sizeWobble;

            Vector3 wobbleOffset = new Vector3(
                Mathf.Sin(Time.time * wobbleSpeed + i * 0.31f),
                Mathf.Cos(Time.time * wobbleSpeed * 0.9f + i * 0.27f),
                Mathf.Sin(Time.time * wobbleSpeed * 1.1f + i * 0.19f)
            ) * positionWobble;

            blobs[i].position = points[i] + wobbleOffset;

            float finalSize = Mathf.Max(0.001f, size + wobble);
            if (i % 4 == 0) finalSize *= 1.18f;
            if (i % 5 == 0) finalSize *= 1.10f;

            blobs[i].localScale = Vector3.one * finalSize;
        }
    }
}