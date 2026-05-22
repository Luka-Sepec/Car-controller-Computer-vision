using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public PlayerInput input;

    public float accelerationValue;
    public float steerValue;
    public bool brake;

    public Vector2 lookValue;

    private void OnEnable()
    {
        input.actions["Accelerate"].performed += context => accelerationValue = context.ReadValue<float>();
        input.actions["Accelerate"].canceled += context => accelerationValue = 0f;

        input.actions["Steer"].performed += context => steerValue = context.ReadValue<float>();
        input.actions["Steer"].canceled += context => steerValue = 0f;

        input.actions["Brake"].performed += context => brake = true;
        input.actions["Brake"].canceled += context => brake = false;

        input.actions["Look"].performed += context => lookValue = context.ReadValue<Vector2>();
        input.actions["Look"].canceled += context => lookValue = Vector2.zero;
    }

    private void OnDisable()
    {
        input.actions["Accelerate"].performed -= context => accelerationValue = context.ReadValue<float>();
        input.actions["Accelerate"].canceled -= context => accelerationValue = 0f;

        input.actions["Steer"].performed -= context => steerValue = context.ReadValue<float>();
        input.actions["Steer"].canceled -= context => steerValue = 0f;

        input.actions["Brake"].performed -= context => brake = true;
        input.actions["Brake"].canceled -= context => brake = false;

        input.actions["Look"].performed -= context => lookValue = context.ReadValue<Vector2>();
        input.actions["Look"].canceled -= context => lookValue = Vector2.zero;
    }
}
