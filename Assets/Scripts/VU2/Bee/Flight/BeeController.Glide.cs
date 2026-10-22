using UnityEngine;

// Balanced flight uses world units for lift/descent and the authored miniature cruise speeds.
public partial class BeeController
{
    [Header("Glide Transition")]
    [Range(0.05f, 0.8f)] public float glideTransitionTime = 0.3f;
    [Range(0f, 0.5f), Tooltip("Extra lift relative to two accepted hand strokes. Does not alter ray detection.")]
    public float pairedLiftBonus = 0.25f;
    [Min(0f), Tooltip("World-space velocity gain spread over a stroke when explicitly leaving a petal.")]
    public float petalTakeoffLift = 0.45f;
    Vector3 worldHorizontalVelocity;
    float glideReleaseElapsed = 10f;
    public float GlideAmount => useWorldGravity && enableRhythmFlight && !IsFlapping && !IsFlowerPerched
        && !IsPositionHolding && !ExternalMovementLocked
        ? Mathf.SmoothStep(0f, 1f, glideReleaseElapsed / Mathf.Max(0.05f, glideTransitionTime)) : 0f;

    float StepBalancedVertical(float deltaTime, bool continuousFlap)
    {
        float remaining = Mathf.Max(0f, deltaTime);
        float velocity = _verticalVelocity;
        // Bound the integration step so a slow frame does not turn a stroke into an impulse.
        while (remaining > 0.000001f) {
            float dt = Mathf.Min(remaining, 1f / 120f);
            bool pulse = _strokeLift.Active;
            float lift = _strokeLift.Step(dt);
            if (pulse || continuousFlap) glideReleaseElapsed = 0f;
            else glideReleaseElapsed += dt;
            float transition = Mathf.SmoothStep(0f, 1f, glideReleaseElapsed / Mathf.Max(0.05f, glideTransitionTime));
            float descent = gravityAcceleration * Mathf.Lerp(0.25f, 1f, transition)
                * Mathf.Max(0f, ExternalGravityScale);
            // Residual wing lift fades after the stroke. Velocity is never reset at this boundary.
            float buttonLift = continuousFlap
                ? (descent + flapRecoveryAcceleration) * Mathf.Max(0f, ExternalFlapThrustScale) * dt : 0f;
            velocity += Mathf.Max(lift, buttonLift) - descent * dt;
            velocity *= Mathf.Exp(-Mathf.Max(0f, fallAirDrag) * dt);
            velocity = Mathf.Clamp(velocity, -FallSpeedLimit, maxAscendSpeed);
            remaining -= dt;
        }
        _restGlideRemaining = Mathf.Max(0f, glideTransitionTime - glideReleaseElapsed);
        return velocity;
    }

    Vector3 StepGlideHorizontal(Vector2 stick, Quaternion heading, float deltaTime)
    {
        stick = Vector2.ClampMagnitude(stick, 1f);
        Vector3 target = heading * new Vector3(stick.x * strafeSpeed, 0f,
            stick.y * (stick.y >= 0f ? moveSpeed : reverseSpeed));
        float acceleration;
        if (stick.sqrMagnitude < 0.001f)
            acceleration = Mathf.Max(0.01f, glideDrag);
        else {
            bool braking = Vector3.Dot(target, worldHorizontalVelocity) < 0f
                || target.sqrMagnitude < worldHorizontalVelocity.sqrMagnitude;
            acceleration = braking ? moveDeceleration : moveAcceleration;
        }
        worldHorizontalVelocity = Vector3.MoveTowards(worldHorizontalVelocity, target,
            Mathf.Max(0f, acceleration) * Mathf.Max(0f, deltaTime));
        Vector3 local = Quaternion.Inverse(heading) * worldHorizontalVelocity;
        _currentForwardSpeed = local.z; _currentStrafeSpeed = local.x;
        return worldHorizontalVelocity;
    }

    public void BeginPetalTakeoff()
    {
        if (!useWorldGravity || !enableRhythmFlight || IsFlowerPerched || ExternalMovementLocked) return;
        _strokeLift.Add(petalTakeoffLift, strokeLiftDuration);
        glideReleaseElapsed = 0f;
    }
}
