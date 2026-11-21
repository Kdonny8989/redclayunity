using UnityEngine;

public class CarInput : MonoBehaviour
{
    public float Throttle { get; private set; }
    public float Steering { get; private set; }

    private void Update()
    {
        Throttle = Input.GetAxisRaw("Vertical");
        Steering = Input.GetAxisRaw("Horizontal");
    }
}