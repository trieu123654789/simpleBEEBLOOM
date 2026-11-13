using UnityEngine;

public sealed class BeeControllerWings : MonoBehaviour
{
    [SerializeField] Transform leftWing;
    [SerializeField] Transform rightWing;
    [SerializeField, Range(0f, 45f)] float strokeAngle = 26f;
    [SerializeField, Min(0.02f)] float strokeDuration = 0.10f;
    [SerializeField, Min(0.02f)] float settleDuration = 0.16f;

    Quaternion leftRest, rightRest;
    Vector3 leftAxis, rightAxis;
    float angle, startAngle, targetAngle, elapsed;
    bool moving;

    void Awake()
    {
        if (!leftWing || !rightWing) return;
        leftRest = leftWing.localRotation;
        rightRest = rightWing.localRotation;
        leftAxis = leftWing.parent.InverseTransformDirection(transform.forward);
        rightAxis = rightWing.parent.InverseTransformDirection(transform.forward);
    }

    public void Stroke(bool upstroke)
    {
        if (!leftWing || !rightWing) return;
        startAngle = angle;
        targetAngle = upstroke ? strokeAngle : -strokeAngle;
        elapsed = 0f;
        moving = true;
    }

    void LateUpdate()
    {
        if (!moving) return;
        elapsed += Time.deltaTime;
        angle = elapsed < strokeDuration
            ? Mathf.SmoothStep(startAngle, targetAngle, elapsed / strokeDuration)
            : Mathf.SmoothStep(targetAngle, 0f, (elapsed - strokeDuration) / settleDuration);
        leftWing.localRotation = Quaternion.AngleAxis(-angle, leftAxis) * leftRest;
        rightWing.localRotation = Quaternion.AngleAxis(angle, rightAxis) * rightRest;
        moving = elapsed < strokeDuration + settleDuration;
    }

    void OnDisable()
    {
        if (!moving) return;
        leftWing.localRotation = leftRest;
        rightWing.localRotation = rightRest;
        moving = false;
        angle = 0f;
    }
}
