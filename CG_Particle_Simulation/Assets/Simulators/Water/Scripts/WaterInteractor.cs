using UnityEngine;

public class WaterInteractor : MonoBehaviour
{
    public WaterSimulation water;

    [Header("splash siz")]
    [Range(0.0f, 2.0f)] public float entrySplashStrength = 0.6f;
    [Range(0.0f, 2.0f)] public float trailSplashStrength = 0.15f;
    [Range(0.005f, 0.15f)] public float entrySplashRadius = 0.045f;
    [Range(0.005f, 0.15f)] public float trailSplashRadius = 0.025f;

    [Header("Trail behaviour")]
    public float minVelocityForSplash = 0.15f;
    public float trailSplashInterval = 0.05f;

    private Vector3 lastPos;
    private bool wasUnderwater;
    private float lastTrailSplashTime;

    void Start()
    {
        lastPos = transform.position;
        wasUnderwater = water != null && transform.position.y < water.WaterSurfaceY;
    }

    void Update()
    {
        if (water == null) return;

        Vector3 pos = transform.position;
        Vector3 vel = (pos - lastPos) / Mathf.Max(Time.deltaTime, 1e-5f);

        bool isUnderwater = pos.y < water.WaterSurfaceY;

        if (isUnderwater && !wasUnderwater)
        {
            float speed = Mathf.Clamp01(Mathf.Abs(vel.y) / 3.0f);
            water.Splash(pos, entrySplashStrength * (0.3f + 0.7f * speed), entrySplashRadius);
        }

        if (isUnderwater && vel.magnitude > minVelocityForSplash)
        {
            if (Time.time - lastTrailSplashTime >= trailSplashInterval)
            {
                float speedFactor = Mathf.Clamp01(vel.magnitude / 3.0f);
                water.Splash(pos, trailSplashStrength * speedFactor, trailSplashRadius);
                lastTrailSplashTime = Time.time;
            }
        }

        wasUnderwater = isUnderwater;
        lastPos = pos;
    }
}
