using UnityEngine;

public class WaterBender : MonoBehaviour
{
    [Header("References")]
    public WaterSimulation water;
    public GameObject waterBlobPrefab;
    public Transform handTransform;

    [Header("Input")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    [Tooltip("Grip threshold to start/keep bending.")]
    [Range(0.05f, 0.95f)] public float gripThreshold = 0.5f;

    [Header("Lift behavior")]
    public float maxLiftDistance = 1.5f;
    public float liftSpeed = 4f;
    [Range(0f, 2f)] public float depressionStrength = 0.05f;
    [Range(0.005f, 0.2f)] public float depressionRadius = 0.06f;
    public float depressionInterval = 0.05f;

    [Header("Release / splash")]
    public float releaseGravity = 9.81f;
    [Range(0f, 2f)] public float impactSplashStrength = 0.8f;
    [Range(0.005f, 0.15f)] public float impactSplashRadius = 0.06f;

    enum State { Idle, Reaching, Held, Falling }
    State state = State.Idle;

    GameObject blobInstance;
    WaterBlob blobScript;
    Vector3 liftSourceWorldXZ;
    Vector3 fallVelocity;
    float lastDepressionTime;

    Transform Hand => handTransform != null ? handTransform : transform;

    void Update()
    {
        switch (state)
        {
            case State.Idle: UpdateIdle();break;
            case State.Reaching: UpdateReaching();break;
            case State.Held: UpdateHeld();break;
            case State.Falling: UpdateFalling();break;
        }
    }

    bool IsGripping()
    {
        return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller) > gripThreshold;
    }

    void UpdateIdle()
    {
        if (!IsGripping()) return;
        if (water == null) return;

        Vector3 handPos = Hand.position;
        Vector3 surfaceXZ = new Vector3(handPos.x, water.WaterSurfaceY, handPos.z);

        float horizDist = Vector3.Distance(
            new Vector3(handPos.x, 0, handPos.z),
            new Vector3(surfaceXZ.x, 0, surfaceXZ.z));
        if (horizDist > maxLiftDistance) return;
        if (handPos.y < water.WaterSurfaceY) return;

        liftSourceWorldXZ = surfaceXZ;
        SpawnBlob(surfaceXZ);
        state = State.Reaching;
    }
    void UpdateReaching()
    {
        if (!IsGripping()) { ReleaseBlob(); return; }

        StampDepression();
        Vector3 target = Hand.position;
        blobInstance.transform.position = Vector3.MoveTowards(
            blobInstance.transform.position, target, liftSpeed * Time.deltaTime);

        float distToSurface = blobInstance.transform.position.y - water.WaterSurfaceY;
        float maxDist = Mathf.Max(0.01f, target.y - water.WaterSurfaceY);
        float t = Mathf.Clamp01(distToSurface / maxDist);
        blobScript.SetGrowth(t);

        if (Vector3.Distance(blobInstance.transform.position, target) < 0.02f)
        {
            blobScript.SetGrowth(1f);
            state = State.Held;
        }
    }

    void UpdateHeld()
    {
        if (!IsGripping()) { ReleaseBlob(); return; }

        StampDepression();

        Vector3 handPos = Hand.position;
        blobInstance.transform.position = Vector3.Lerp(
            blobInstance.transform.position, handPos, 1f - Mathf.Exp(-20f * Time.deltaTime));

        blobScript.SetMotion(handPos);
    }

    void UpdateFalling()
    {
        if (blobInstance == null) { state = State.Idle; return; }

        fallVelocity.y -= releaseGravity * Time.deltaTime;
        blobInstance.transform.position += fallVelocity * Time.deltaTime;

        Vector3 pos = blobInstance.transform.position;
        float surfY = water.GetWaterSurfaceY(pos);
        if (pos.y <= surfY)
        {
            float speed = Mathf.Clamp01(-fallVelocity.y / 5f);
            water.Splash(pos, impactSplashStrength * (0.5f + 0.5f * speed), impactSplashRadius);
            Destroy(blobInstance);
            blobInstance = null;
            blobScript = null;
            state = State.Idle;
        }
    }

    void StampDepression()
    {
        if (Time.time - lastDepressionTime < depressionInterval) return;
        lastDepressionTime = Time.time;
        water.Depress(liftSourceWorldXZ, depressionStrength, depressionRadius);
    }

    void SpawnBlob(Vector3 atSurface)
    {
        if (waterBlobPrefab == null)
        {
            Debug.LogError("[WaterBender] No waterBlobPrefab assigned."); return;
        }

        blobInstance = Instantiate(waterBlobPrefab, atSurface, Quaternion.identity);
        blobScript = blobInstance.GetComponent<WaterBlob>();
        if (blobScript == null) blobScript = blobInstance.AddComponent<WaterBlob>();
        blobScript.SetGrowth(0f);
    }

    void ReleaseBlob()
    {
        if (blobInstance == null) { state = State.Idle; return; }
        fallVelocity = Vector3.zero;
        state = State.Falling;
    }
}
