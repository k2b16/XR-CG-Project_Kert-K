using UnityEngine;
using Oculus.Interaction;

public class PaperAirplaneGlide : MonoBehaviour
{
    [Header("Glide Settings")]
    public float liftMultiplier = 0.5f;
    public float dragMultiplier = 0.3f;
    public float minGlideSpeed = 0.3f;
    public float throwForceMultiplier = 2f;
    public float rotationSpeed = 2f;

    [Header("Axis Settings")]
    public Vector3 modelForward = Vector3.forward; // change if nose points wrong way

    private Rigidbody _rb;
    private Grabbable _grabbable;
    private bool _wasGrabbed = false;
    private bool _hasBeenThrown = false;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _grabbable = GetComponent<Grabbable>();
        _rb.isKinematic = false;
        _rb.useGravity = true;
    }

    void Update()
    {
        bool isGrabbed = _grabbable.SelectingPointsCount > 0;

        if (!_wasGrabbed && isGrabbed)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _hasBeenThrown = false;
        }

        if (_wasGrabbed && !isGrabbed)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _hasBeenThrown = true;

            Vector3 localVel = OVRInput.GetLocalControllerVelocity(
                               OVRInput.Controller.RTouch);
            Vector3 worldVel = Camera.main.transform.TransformDirection(localVel);

            if (worldVel.magnitude < 0.1f)
            {
                localVel = OVRInput.GetLocalControllerVelocity(
                           OVRInput.Controller.LTouch);
                worldVel = Camera.main.transform.TransformDirection(localVel);
            }

            _rb.linearVelocity = worldVel * throwForceMultiplier;

            // Lock rotation to prevent spinning on release
            _rb.angularVelocity = Vector3.zero;

            Debug.Log("Throw velocity: " + worldVel);
        }

        _wasGrabbed = isGrabbed;
    }

    void FixedUpdate()
    {
        if (!_hasBeenThrown || _rb.isKinematic) return;

        float speed = _rb.linearVelocity.magnitude;
        if (speed < minGlideSpeed) return;

        // Align model forward to velocity direction
        Vector3 velocityDir = _rb.linearVelocity.normalized;
        Quaternion targetRot = Quaternion.FromToRotation(
            transform.TransformDirection(modelForward), velocityDir) * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRot, Time.fixedDeltaTime * rotationSpeed);

        // Lift perpendicular to velocity
        Vector3 liftDir = Vector3.Cross(_rb.linearVelocity, transform.right).normalized;
        _rb.AddForce(liftDir * speed * liftMultiplier);

        // Drag
        _rb.AddForce(-_rb.linearVelocity * dragMultiplier);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * 0.5f);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 0.5f);
    }
}