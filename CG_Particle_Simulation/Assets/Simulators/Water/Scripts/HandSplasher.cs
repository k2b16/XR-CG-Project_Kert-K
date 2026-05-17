using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OVRSkeleton))]
public class HandSplasher : MonoBehaviour
{
    [Header("water refs")]
    public WaterSimulation water;

    [Header("splash size")]
    [Range(0f, 2f)]   public float entrySplashStrength = 0.5f;
    [Range(0.005f, 0.1f)] public float entrySplashRadius = 0.035f;
    [Range(0f, 2f)]   public float trailSplashStrength  = 0.12f;
    [Range(0.005f, 0.1f)] public float trailSplashRadius  = 0.022f;

    [Header("trail throttling")]
    public float minTrailSpeed = 0.2f;
    public float trailInterval = 0.06f;

    [Header("Tracking")]
    public string[] tipNameSuffixes = { "ThumbTip", "IndexTip", "MiddleTip", "RingTip", "LittleTip", "PinkyTip" };

    OVRSkeleton skeleton;
    class TipState
    {
        public Transform t;
        public Vector3 lastPos;
        public bool wasUnder;
        public float lastTrailTime;
    }
    readonly List<TipState> tips = new List<TipState>();
    bool tipsInitialized = false;

    void Awake() {skeleton = GetComponent<OVRSkeleton>();}

    void Update()
    {
        if (water == null) return;
        if (!tipsInitialized)
        {
            if (skeleton.Bones == null || skeleton.Bones.Count == 0) return;
            InitializeTips();
            tipsInitialized = true;
        }

        foreach (var tip in tips)
        {
            if (tip.t == null) continue;

            Vector3 pos = tip.t.position;
            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            Vector3 vel = (pos - tip.lastPos) / dt;

            float surfY = water.GetWaterSurfaceY(pos);
            bool isUnder = pos.y < surfY;

            if (isUnder && !tip.wasUnder)
            {
                float downSpeed = Mathf.Clamp01(-vel.y / 2.5f);
                water.Splash(pos,
                             entrySplashStrength * (0.4f + 0.6f * downSpeed),
                             entrySplashRadius);
            }

            if (isUnder && vel.magnitude > minTrailSpeed &&
                Time.time - tip.lastTrailTime > trailInterval)
            {
                float speedFactor = Mathf.Clamp01(vel.magnitude / 2.5f);
                water.Splash(pos,
                             trailSplashStrength * speedFactor,
                             trailSplashRadius);
                tip.lastTrailTime = Time.time;
            }

            tip.wasUnder = isUnder;
            tip.lastPos = pos;
        }
    }

    void InitializeTips()
    {
        tips.Clear();
        foreach (var bone in skeleton.Bones)
        {
            if (bone == null || bone.Transform == null) continue;
            string name = bone.Transform.name;

            bool isTip = false;
            foreach (var suffix in tipNameSuffixes)
            {
                if (name.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
                {
                    isTip = true; break;
                }
            }
            if (!isTip) continue;

            tips.Add(new TipState
            {
                t = bone.Transform,
                lastPos = bone.Transform.position,
                wasUnder = false,
                lastTrailTime = 0f,
            });
        }

        if (tips.Count == 0)
        {
            Debug.LogWarning($"No fingertip bones found on {name}. Skeleton may not be set up correctly.");
        }
    }
}
