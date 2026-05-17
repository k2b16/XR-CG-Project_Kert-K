using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshRenderer))]
public class WaterSimulation : MonoBehaviour
{
    [Header("compute")]
    public ComputeShader waveCompute;

    [Range(64, 512)] public int resolution = 256;
    [Range(0.95f, 0.999f)] public float damping = 0.996f;
    [Range(1, 8)] public int stepsPerFrame = 2;

    [Header("world")]
    public float planeSize = 10f;
    public float heightScale = 1f;

    [Header("splash")]
    [Range(0.005f, 0.2f)] public float defaultSplashRadius = 0.025f;
    [Range(0.0f, 2.0f)] public float defaultSplashStrength = 0.4f;

    [Header("cpu readback")]
    public bool enableReadback = true;
    [Range(1, 8)] public int readbackInterval = 1;

    private RenderTexture[] heightTex = new RenderTexture[2];
    private int writeIdx = 0;
    private int CurrentIdx => 1 - writeIdx;

    private int kStep, kSplash, kClear;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock mpb;

    private Vector2[] heightCache;
    private bool readbackInFlight;
    private int framesSinceReadback;

    private static readonly int P_Input = Shader.PropertyToID("_Input");
    private static readonly int P_Output = Shader.PropertyToID("_Output");
    private static readonly int P_SplashTarget = Shader.PropertyToID("_SplashTarget");
    private static readonly int P_Resolution = Shader.PropertyToID("_Resolution");
    private static readonly int P_Damping = Shader.PropertyToID("_Damping");
    private static readonly int P_SplashPos = Shader.PropertyToID("_SplashPos");
    private static readonly int P_SplashRadius = Shader.PropertyToID("_SplashRadius");
    private static readonly int P_SplashStrength = Shader.PropertyToID("_SplashStrength");
    private static readonly int P_HeightTex = Shader.PropertyToID("_HeightTex");

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        mpb = new MaterialPropertyBlock();

        if (waveCompute == null)
        {
            Debug.LogError("[WaterSimulation] No compute shader assigned.");
            enabled = false;
            return;
        }

        kStep = waveCompute.FindKernel("KStep");
        kSplash = waveCompute.FindKernel("KSplash");
        kClear = waveCompute.FindKernel("KClear");

        AllocateTextures();
        ClearTextures();
        BindTextureToMaterial();

        heightCache = new Vector2[resolution * resolution];
    }

    void OnDestroy() { ReleaseTextures(); }

    void AllocateTextures()
    {
        for (int i = 0; i < 2; i++)
        {
            heightTex[i] = new RenderTexture(resolution, resolution, 0, GraphicsFormat.R16G16_SFloat)
            {
                name = $"WaterHeight_{i}",
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                autoGenerateMips = false,
            };
            heightTex[i].Create();
        }
    }

    void ReleaseTextures()
    {
        for (int i = 0; i < 2; i++)
        {
            if (heightTex[i] != null)
            {
                heightTex[i].Release();
                Destroy(heightTex[i]);
                heightTex[i] = null;
            }
        }
    }

    void ClearTextures()
    {
        int groups = Mathf.CeilToInt(resolution / 8f);
        for (int i = 0; i < 2; i++)
        {
            waveCompute.SetTexture(kClear, P_Output, heightTex[i]);
            waveCompute.SetInt(P_Resolution, resolution);
            waveCompute.Dispatch(kClear, groups, groups, 1);
        }
        writeIdx = 0;
    }

    void Update()
    {
        for (int i = 0; i < stepsPerFrame; i++) Step();
        BindTextureToMaterial();
        if (enableReadback) RequestReadback();
    }

    void Step()
    {
        int readIdx = CurrentIdx;
        int outIdx = writeIdx;

        waveCompute.SetTexture(kStep, P_Input, heightTex[readIdx]);
        waveCompute.SetTexture(kStep, P_Output, heightTex[outIdx]);
        waveCompute.SetInt(P_Resolution, resolution);
        waveCompute.SetFloat(P_Damping, damping);

        int groups = Mathf.CeilToInt(resolution / 8f);
        waveCompute.Dispatch(kStep, groups, groups, 1);

        writeIdx = readIdx;
    }

    void BindTextureToMaterial()
    {
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetTexture(P_HeightTex, heightTex[CurrentIdx]);
        meshRenderer.SetPropertyBlock(mpb);
    }

    void RequestReadback()
    {
        if (readbackInFlight) return;
        if (++framesSinceReadback < readbackInterval) return;
        framesSinceReadback = 0;

        readbackInFlight = true;
        AsyncGPUReadback.Request(heightTex[CurrentIdx], 0, TextureFormat.RGFloat, OnReadbackComplete);
    }

    void OnReadbackComplete(AsyncGPUReadbackRequest req)
    {
        readbackInFlight = false;
        if (req.hasError) return;
        var data = req.GetData<Vector2>();
        if (data.Length != heightCache.Length) return;
        data.CopyTo(heightCache);
    }
    public void Splash(Vector3 worldPos, float strength = -1f, float radius = -1f)
    {
        if (strength < 0f) strength = defaultSplashStrength;
        if (radius < 0f) radius = defaultSplashRadius;
        DispatchGaussian(worldPos, strength, radius);
    }

    public void Depress(Vector3 worldPos, float strength, float radius){ DispatchGaussian(worldPos, -Mathf.Abs(strength), radius); }

    void DispatchGaussian(Vector3 worldPos, float signedStrength, float radius)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector2 uv = new Vector2(
            localPos.x / planeSize + 0.5f,
            localPos.z / planeSize + 0.5f);

        if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f) return;

        waveCompute.SetTexture(kSplash, P_SplashTarget, heightTex[CurrentIdx]);
        waveCompute.SetInt(P_Resolution, resolution);
        waveCompute.SetVector(P_SplashPos, uv);
        waveCompute.SetFloat(P_SplashRadius, radius);
        waveCompute.SetFloat(P_SplashStrength, signedStrength);

        int groups = Mathf.CeilToInt(resolution / 8f);
        waveCompute.Dispatch(kSplash, groups, groups, 1);
    }
    public float WaterSurfaceY => transform.position.y;
    public float GetWaterSurfaceY(Vector3 worldPos)
    {
        float baseY = transform.position.y;
        if (heightCache == null || !enableReadback) return baseY;

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        float u = localPos.x / planeSize + 0.5f;
        float v = localPos.z / planeSize + 0.5f;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return baseY;

        float fx = u * (resolution - 1);
        float fy = v * (resolution - 1);
        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, resolution - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(fy), 0, resolution - 1);
        int x1 = Mathf.Min(x0 + 1, resolution - 1);
        int y1 = Mathf.Min(y0 + 1, resolution - 1);
        float tx = fx - x0;
        float ty = fy - y0;

        float h00 = heightCache[y0 * resolution + x0].y;
        float h10 = heightCache[y0 * resolution + x1].y;
        float h01 = heightCache[y1 * resolution + x0].y;
        float h11 = heightCache[y1 * resolution + x1].y;

        float h0 = Mathf.Lerp(h00, h10, tx);
        float h1 = Mathf.Lerp(h01, h11, tx);
        return baseY + Mathf.Lerp(h0, h1, ty) * heightScale;
    }
}