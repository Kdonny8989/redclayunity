using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CarInput))]
public class CarController2D : MonoBehaviour
{
    [Header("Engine")]
    public float acceleration = 20f;
    public float maxSpeed = 25f;

    [Header("Steering")]
    public float turnSpeed = 160f;

    [Header("Grip")]
    [Range(0f, 1f)]
    public float tireGrip = 0.88f;

    public float dragAmount = 0.5f;

    private Rigidbody2D rb;
    private CarInput input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<CarInput>();
    }

    private void FixedUpdate()
    {
        ApplyEngine();
        ApplySteering();
        ApplyLateralGrip();
        ApplyDrag();
    }

    private void ApplyEngine()
    {
        Vector2 force = (Vector2)transform.up *
                        input.Throttle *
                        acceleration;

        rb.AddForce(force);

        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity =
                rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private void ApplySteering()
    {
        float speedFactor =
            rb.linearVelocity.magnitude / maxSpeed;

        float rotationAmount =
            -input.Steering *
            turnSpeed *
            speedFactor *
            Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation + rotationAmount);
    }

    private void ApplyLateralGrip()
    {
        Vector2 forward = transform.up;
        Vector2 right = transform.right;

        float forwardVelocity =
            Vector2.Dot(rb.linearVelocity, forward);

        float sideVelocity =
            Vector2.Dot(rb.linearVelocity, right);

        Vector2 newVelocity =
            forward * forwardVelocity +
            right * sideVelocity * (1f - tireGrip);

        rb.linearVelocity = newVelocity;
    }

    private void ApplyDrag()
    {
        rb.linearVelocity *= (1f - dragAmount * Time.fixedDeltaTime);
    }
}