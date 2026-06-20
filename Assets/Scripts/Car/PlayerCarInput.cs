using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(CarManager))]
public class PlayerCarInput : MonoBehaviour
{
    public InputSystem_Actions ISAs;
    private InputAction _move;
    private InputAction _jump;
    private Vector2 _directionalInput;
    private CarManager _carManagerScript;


    
    private void Awake()
    {
        ISAs = new InputSystem_Actions();
        _carManagerScript = GetComponent<CarManager>();
    }
    private void Update()
    {
        _directionalInput = _move.ReadValue<Vector2>();
        _carManagerScript.ThrottleBrakeValue = _directionalInput.y;
        _carManagerScript.SteeringDirection = _directionalInput.x;
    }

    private void PerformJump(InputAction.CallbackContext context)
    {
        Debug.Log("Trying to jump from within Player Car Input!!");
        _carManagerScript.Jump();
    }
    private void OnEnable()
    {
        _move = ISAs.Player.Move;
        _move.Enable();

        _jump = ISAs.Player.Jump;
        _jump.Enable();
        _jump.performed += PerformJump;
    }
    private void OnDisable()
    {
        _move.Disable();
        _jump.Disable();
    }
}
