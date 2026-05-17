using UnityEngine;
using UnityEngine.VFX;

public class WaterVFXDriver : MonoBehaviour
{
    public VisualEffect vfx;
    public Transform source;
    public Transform orb;
    public float pullStrength = 1.8f;

    void Reset()
    {
        if (vfx == null) vfx = GetComponent<VisualEffect>();
    }

    void LateUpdate()
    {
        if (vfx == null || source == null) return;
        transform.position = source.position;
        vfx.SetVector3("SourcePosLocal", source.position);

        if (orb != null)
            vfx.SetVector3("OrbPosLocal", orb.position);

        vfx.SetFloat("PullStrength", pullStrength);

        Debug.Log("Source pos: " + source.position);
    }
}