using UnityEngine;

public class CarCamera : MonoBehaviour
{
    [SerializeField] private InputHandler input;
    public Transform target;

    public float distance = 6f;
    public float height = 2f;

    public float mouseSensitivity = 3f;
    public float followSmoothTime = 0.1f;

    private float yaw;
    private float pitch = 10f;

    Vector3 currentVelocity;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        Vector2 look = input.lookValue;
        yaw += look.x * mouseSensitivity;
        pitch -= look.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -5f, 45f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = target.position - rotation * Vector3.forward * distance + Vector3.up * height;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, followSmoothTime);
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}
