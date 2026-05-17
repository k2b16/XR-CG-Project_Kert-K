using System.Collections;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public class ARWaterManager : MonoBehaviour
{
    [Header("refs")]
    public GameObject waterObject;
    public OVRHand leftHand;
    public OVRHand rightHand;

    [Header("placement")]
    public float mrukTimeoutSeconds = 5f;
    public float poolSize = 1.5f;

    [Header("pinch settings")]
    public float pinchDebounce = 0.3f;

    private float floorY = 0f;
    private bool floorFound = false;
    private bool placed = false;
    private float lastPinchEndTime = -999f;
    private bool wasPinchingLast = false;

    void Start()
    {
        if (waterObject == null)
        {
            Debug.LogError("waterObject not assigned.");
            enabled = false;
            return;
        }

        waterObject.SetActive(false);
        StartCoroutine(WaitForMRUKFloor());
    }

    IEnumerator WaitForMRUKFloor()
    {
        float deadline = Time.time + mrukTimeoutSeconds;
        while (Time.time < deadline)
        {
            if (MRUK.Instance != null)
            {
                var room = MRUK.Instance.GetCurrentRoom();
                if (room != null)
                {
                    var floor = room.GetFloorAnchor();
                    if (floor != null)
                    {
                        floorY = floor.transform.position.y;
                        floorFound = true;
                        Debug.Log($"MRUK floor found at Y={floorY}");
                        yield break;
                    }
                }
            }
            yield return null;
        }

        Debug.LogWarning("MRUK floor not found within timeout. Falling back to manual placement (will use pinch height as floor).");
    }

    void Update()
    {
        bool isPinching = IsAnyHandPinching(out Vector3 pinchPos);
        if (isPinching && !wasPinchingLast && Time.time - lastPinchEndTime > pinchDebounce) { PlaceWater(pinchPos); }

        if (!isPinching && wasPinchingLast) { lastPinchEndTime = Time.time; }
        wasPinchingLast = isPinching;
    }

    bool IsAnyHandPinching(out Vector3 pinchPos)
    {
        pinchPos = Vector3.zero;

        if (rightHand != null && rightHand.IsTracked &&
            rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
            rightHand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.High)
        {
            pinchPos = rightHand.transform.position;
            return true;
        }
        if (leftHand != null && leftHand.IsTracked &&
            leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
            leftHand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.High)
        {
            pinchPos = leftHand.transform.position;
            return true;
        }
        return false;
    }

    void PlaceWater(Vector3 pinchPos)
    {
        float useY = floorFound ? floorY : pinchPos.y - 0.05f;

        Vector3 placePos = new Vector3(pinchPos.x, useY, pinchPos.z);
        waterObject.transform.position = placePos;
        waterObject.SetActive(true);
        placed = true;

        var sim = waterObject.GetComponent<WaterSimulation>();
        var mesh = waterObject.GetComponent<WaterMeshGenerator>();
        if (sim != null) sim.planeSize = poolSize;
        if (mesh != null) mesh.size = poolSize;

        Debug.Log($"Water placed at {placePos}");
    }
}
