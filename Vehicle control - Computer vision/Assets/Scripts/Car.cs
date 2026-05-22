using UnityEngine;

public class Car : MonoBehaviour
{
    public InputHandler input;
    public Rigidbody rb;
    public Wheel[] wheels;
    public float currentSpeed;
    public float maxSpeed = 220f;
    public float engineForce = 4000f;
    public float sideGrip = 1000f;

    [Header("Suspension")]
    public float defaultSpringLength = 0.45f;
    public float springForce = 10000f;
    public float dampingForce = 2000f;
    public float antiFlipStrength = 5000f;

    private void Start()
    {
        rb.centerOfMass += new Vector3(0, -0.5f, 0);
    }
    private void Update()
    {
        currentSpeed = rb.linearVelocity.magnitude * 3.6f;
        foreach(Wheel wheel in wheels)
        {
            wheel.Rotate();
            wheel.Steer(input.steerValue, currentSpeed);
        }
    }

    private void FixedUpdate()
    {
        float accel = input.accelerationValue;
        if (currentSpeed >= maxSpeed)
        {
            accel = 0f;
        }
        foreach(Wheel wheel in wheels)
        {
            wheel.UpdateSuspension();
            if (!wheel.grounded) continue;
            rb.AddForceAtPosition(wheel.suspensionForce, wheel.transform.position);
            //Velocities
            Vector3 wheelVelocity = rb.GetPointVelocity(wheel.transform.position);
            Vector3 forwardVelocity = Vector3.Dot(wheelVelocity, wheel.transform.forward) * wheel.transform.forward;
            Vector3 sidewaysVelocity = Vector3.Dot(wheelVelocity, wheel.transform.right) * wheel.transform.right;

            if (!wheel.front)
            {
                Vector3 driveForce = accel * engineForce * wheel.transform.forward;
                rb.AddForceAtPosition(driveForce, wheel.transform.position);
            }
            Vector3 frictionForce = rb.mass * 0.07f * -forwardVelocity;
            Vector3 sideGripForce = Vector3.ClampMagnitude(sideGrip * -sidewaysVelocity, 8000f);

            Vector3 totalForce = frictionForce + sideGripForce;

            rb.AddForceAtPosition(totalForce, wheel.transform.position);

            Wheel backLeft = wheels[0];
            Wheel backRight = wheels[1];
            Wheel frontLeft = wheels[2];
            Wheel frontRight = wheels[3];

            float rollForce = (frontLeft.compression - frontRight.compression) * antiFlipStrength;

            rb.AddForceAtPosition(rollForce * -frontLeft.transform.up, frontLeft.transform.position);
            rb.AddForceAtPosition(rollForce * frontRight.transform.up, frontRight.transform.position);

            rollForce = (backLeft.compression - backRight.compression) * antiFlipStrength;

            rb.AddForceAtPosition(rollForce * -backLeft.transform.up, backLeft.transform.position);
            rb.AddForceAtPosition(rollForce * backRight.transform.up, backRight.transform.position);

        }
    }
}
