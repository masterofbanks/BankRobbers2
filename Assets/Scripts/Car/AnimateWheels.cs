using UnityEngine;

public class AnimateWheels : MonoBehaviour
{
    [SerializeField] private Transform[] TurnableWheelMeshes;
    [SerializeField] private float SteeringSmoothingSpeed = 5f;
    private CarManager _carManagementScript;
    private float _currentSteeringAngle;
    
    private void Awake()
    {
        _carManagementScript = GetComponentInParent<CarManager>();
    }

    private void Update()
    {
        _currentSteeringAngle = Mathf.Lerp(_currentSteeringAngle, _carManagementScript.GetCurrentTurningConfiguration(), Time.deltaTime * SteeringSmoothingSpeed);
        foreach (Transform wheelMesh in TurnableWheelMeshes)
        {
            wheelMesh.localRotation = Quaternion.Euler(0f, _currentSteeringAngle, 0f);
        }
    }
}
