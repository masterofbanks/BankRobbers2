using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(CarManager))]
public class PlayerCarInput : MonoBehaviour
{
    public InputSystem_Actions ISAs;
    private InputAction _move;
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
    private void OnEnable()
    {
        _move = ISAs.Player.Move;
        _move.Enable();
    }
    private void OnDisable()
    {
        _move.Disable();
    }
}
