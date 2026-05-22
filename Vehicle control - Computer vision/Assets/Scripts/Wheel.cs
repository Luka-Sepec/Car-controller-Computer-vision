using UnityEngine;

public class Wheel : MonoBehaviour
{
    public Car car;
    public Transform visuals;
    public float maxSteerAngle = 30f;
    public float currentSteerAngle = 0f;
    public float wheelRadius = 0.45f;
    public float direction;

    public bool front;
    public bool right;
    public bool grounded;

    [Header("Suspension")]
    public float compression;
    public Vector3 suspensionForce;


    private void Start()
    {
        car = GetComponentInParent<Car>();
        visuals = GetComponentsInChildren<Transform>()[1];
    }

    public void Rotate()
    {
        float forwardSpeed = Vector3.Dot(car.rb.linearVelocity, transform.forward);
        float rotation = (forwardSpeed / (2f * Mathf.PI * wheelRadius)) * 360f * Time.deltaTime;
        visuals.Rotate(Vector3.right, rotation, Space.Self);
    }

    public void Steer(float steerValue, float speed)
    {
        if (!front) return;

        float speedFactor = Mathf.InverseLerp(0f, 150f, speed);
        float dynamicMaxAngle = Mathf.Lerp(maxSteerAngle, maxSteerAngle * 0.125f, speedFactor);
        float targetAngle = steerValue * dynamicMaxAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, 0.125f);
        transform.localRotation = Quaternion.Euler(0f, currentSteerAngle, 0f);
    }

    public void UpdateSuspension()
    {
        Vector3 springOrigin = transform.position;
        if (Physics.Raycast(springOrigin, -transform.up, out RaycastHit hit, car.defaultSpringLength, LayerMask.GetMask("Ground")))
        {
            compression = car.defaultSpringLength - hit.distance;
            Vector3 pointVelocity = car.rb.GetPointVelocity(transform.position);
            float compressionVelocity = Vector3.Dot(transform.up, pointVelocity);
            float force = (compression * car.springForce - compressionVelocity * car.dampingForce);
            force = Mathf.Clamp(force, 0f, 30000f);
            suspensionForce = force * transform.up;
            grounded = true;
        }
        else
        {
            grounded = false;
            suspensionForce = Vector3.zero;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Vector3 start = transform.position;
        Vector3 end;

        end = start - transform.up * wheelRadius;
        Gizmos.DrawLine(start, end);

    }
}
