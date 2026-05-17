using UnityEngine;
using UnityEngine.Rendering;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterMeshGenerator : MonoBehaviour
{
    public float size = 10f;
    [Range(16, 256)] public int subdivisions = 128;
    public float boundsHeightPadding = 5f;

    void Awake(){ GetComponent<MeshFilter>().sharedMesh = BuildPlaneMesh(); }

    Mesh BuildPlaneMesh()
    {
        var mesh = new Mesh
        {
            name = "WaterPlane",
            indexFormat = IndexFormat.UInt32,
        };

        int side = subdivisions + 1;
        int vCount = side * side;
        int qCount = subdivisions * subdivisions;

        var vertices = new Vector3[vCount];
        var uvs = new Vector2[vCount];
        var indices = new int[qCount * 6];
        for (int y = 0; y < side; y++)
        {
            for (int x = 0; x < side; x++)
            {
                int i = y * side + x;
                float fx = (float)x / subdivisions;
                float fz = (float)y / subdivisions;
                vertices[i] = new Vector3((fx - 0.5f) * size, 0f, (fz - 0.5f) * size);
                uvs[i] = new Vector2(fx, fz);
            }
        }

        int t = 0;
        for (int y = 0; y < subdivisions; y++)
        {
            for (int x = 0; x < subdivisions; x++)
            {
                int v = y * side + x;
                indices[t++] = v;
                indices[t++] = v + side;
                indices[t++] = v + 1;

                indices[t++] = v + 1;
                indices[t++] = v + side;
                indices[t++] = v + side + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.bounds = new Bounds(
            Vector3.zero,
            new Vector3(size * 1.1f, boundsHeightPadding * 2f, size * 1.1f));

        return mesh;
    }
}
