using UnityEngine;

public class CarManager : MonoBehaviour
{
    [Header("Wheel Information")]
    [SerializeField] private Transform[] CylinderComponents;
    [SerializeField] [Range(1,10)] private float WheelRadius;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnValidate()
    {
        UpdateWheelScale();
    }

    private void UpdateWheelScale()
    {
        foreach(Transform wheel in CylinderComponents)
        {
            if(wheel != null)
            {
                wheel.localScale = new Vector3(WheelRadius, wheel.localScale.y, WheelRadius);
            }
        }
    }
}
