using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterTubeMesh : MonoBehaviour
{
    [Header("References")]
    public Transform source;
    public Transform attractor;

    [Header("Chain Motion")]
    [Range(8, 40)] public int pointCount = 20;
    public float segmentLength = 0.02f;

    public float sourceFollowSpeed = 18f;
    public float springStrength = 14f;
    public float damping = 12f;

    [Header("Orb Behaviour")]
    public float attractStrength = 4f;
    public float orbitRadius = 0.18f;
    public float swirlStrength = 0.45f;
    public Vector3 swirlAxis = Vector3.up;

    [Header("Tube Shape")]
    [Range(3, 12)] public int radialSegments = 6;
    public float startRadius = 0.032f;
    public float endRadius = 0.010f;
    public float radiusWobble = 0.12f;
    public float centerWobble = 0.002f;
    public float wobbleSpeed = 1.2f;

    private Vector3[] points;
    private Vector3[] velocities;

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] normals;
    private Vector2[] uvs;
    private int[] triangles;

    private int lastPointCount = -1;
    private int lastRadialSegments = -1;

    void Awake()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.name = "WaterTubeMesh";
        mesh.MarkDynamic();
        mf.sharedMesh = mesh;
    }

    void Start()
    {
        InitializeMotion();
        EnsureMeshData();
    }

    void OnValidate()
    {
        pointCount = Mathf.Max(8, pointCount);
        radialSegments = Mathf.Max(3, radialSegments);
        segmentLength = Mathf.Max(0.001f, segmentLength);
        startRadius = Mathf.Max(0.001f, startRadius);
        endRadius = Mathf.Max(0.001f, endRadius);
    }

    void InitializeMotion()
    {
        points = new Vector3[pointCount];
        velocities = new Vector3[pointCount];

        Vector3 startPos = source != null ? source.position : transform.position;

        for (int i = 0; i < pointCount; i++)
        {
            points[i] = startPos;
            velocities[i] = Vector3.zero;
        }
    }

    void EnsureMeshData()
    {
        if (mesh == null) return;

        if (lastPointCount == pointCount && lastRadialSegments == radialSegments && vertices != null)
            return;

        lastPointCount = pointCount;
        lastRadialSegments = radialSegments;

        int ringVertexCount = radialSegments + 1;
        int vertexCount = pointCount * ringVertexCount;
        int triangleIndexCount = (pointCount - 1) * radialSegments * 6;

        vertices = new Vector3[vertexCount];
        normals = new Vector3[vertexCount];
        uvs = new Vector2[vertexCount];
        triangles = new int[triangleIndexCount];

        int tri = 0;
        for (int ring = 0; ring < pointCount - 1; ring++)
        {
            int ringStart = ring * ringVertexCount;
            int nextRingStart = (ring + 1) * ringVertexCount;

            for (int seg = 0; seg < radialSegments; seg++)
            {
                int a = ringStart + seg;
                int b = ringStart + seg + 1;
                int c = nextRingStart + seg;
                int d = nextRingStart + seg + 1;

                triangles[tri++] = a;
                triangles[tri++] = c;
                triangles[tri++] = b;

                triangles[tri++] = b;
                triangles[tri++] = c;
                triangles[tri++] = d;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
    }

    void LateUpdate()
    {
        if (source == null || attractor == null) return;

        if (points == null || points.Length != pointCount)
            InitializeMotion();

        EnsureMeshData();
        SimulatePoints();
        UpdateTubeMesh();
    }

    void SimulatePoints()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        points[0] = Vector3.Lerp(points[0], source.position, 1f - Mathf.Exp(-sourceFollowSpeed * dt));

        for (int i = 1; i < pointCount; i++)
        {
            Vector3 current = points[i];
            Vector3 prev = points[i - 1];

            Vector3 toPrev = prev - current;
            Vector3 dirToPrev = toPrev.sqrMagnitude > 0.000001f ? toPrev.normalized : Vector3.forward;
            Vector3 desiredPos = prev - dirToPrev * segmentLength;

            Vector3 force = Vector3.zero;

            // Keep chain together
            force += (desiredPos - current) * springStrength;

            // Pull toward orb shell
            Vector3 toCenter = attractor.position - current;
            float dist = toCenter.magnitude;

            if (dist > 0.000001f)
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
    }

    void UpdateTubeMesh()
    {
        int ringVertexCount = radialSegments + 1;

        for (int i = 0; i < pointCount; i++)
        {
            float t01 = (float)i / (pointCount - 1);

            Vector3 prev = points[Mathf.Max(i - 1, 0)];
            Vector3 next = points[Mathf.Min(i + 1, pointCount - 1)];
            Vector3 tangent = (next - prev).normalized;

            if (tangent.sqrMagnitude < 0.000001f)
                tangent = Vector3.forward;

            Vector3 refUp = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(tangent, refUp)) > 0.95f)
                refUp = Vector3.right;

            Vector3 normal = Vector3.Cross(refUp, tangent).normalized;
            Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

            float radius = Mathf.Lerp(startRadius, endRadius, t01);

            float noise = Mathf.Sin(Time.time * wobbleSpeed + i * 0.55f) * radiusWobble;
            radius *= 1f + noise;
            radius = Mathf.Max(0.001f, radius);

            Vector3 ringCenter = points[i]
                + normal * (Mathf.Sin(Time.time * wobbleSpeed + i * 0.37f) * centerWobble)
                + binormal * (Mathf.Cos(Time.time * wobbleSpeed * 0.9f + i * 0.29f) * centerWobble);

            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = (j / (float)radialSegments) * Mathf.PI * 2f;
                Vector3 radialDir = Mathf.Cos(angle) * normal + Mathf.Sin(angle) * binormal;
                int index = i * ringVertexCount + j;

                vertices[index] = transform.InverseTransformPoint(ringCenter + radialDir * radius);
                normals[index] = transform.InverseTransformDirection(radialDir).normalized;
                uvs[index] = new Vector2(j / (float)radialSegments, t01);
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}