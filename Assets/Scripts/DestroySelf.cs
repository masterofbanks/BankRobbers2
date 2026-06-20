using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    public float TimeAlive;

    private float _t;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _t = 0;
    }

    // Update is called once per frame
    void Update()
    {
        _t += Time.deltaTime;
        if (_t > TimeAlive)
        {
            Destroy(this.gameObject);
        }
    }
}