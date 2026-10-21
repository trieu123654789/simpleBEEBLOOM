using System.Collections.Generic;
using UnityEngine;

// A stroke contributes a fixed total change in velocity, independent of frame rate.
// The smoothstep integral starts and ends with zero lift acceleration.
public sealed class BeeLiftEnvelope
{
    private struct Pulse
    {
        public float amount, duration, elapsed;
    }

    private readonly List<Pulse> _pulses = new List<Pulse>(8);
    public bool Active => _pulses.Count > 0;

    public bool Add(float amount, float duration)
    {
        if (amount <= 0f || _pulses.Count >= 16) return false;
        _pulses.Add(new Pulse { amount = amount, duration = Mathf.Max(0.01f, duration) });
        return true;
    }

    public float Step(float deltaTime)
    {
        if (deltaTime <= 0f) return 0f;
        float lift = 0f;
        for (int i = _pulses.Count - 1; i >= 0; i--)
        {
            Pulse pulse = _pulses[i];
            float before = Mathf.Clamp01(pulse.elapsed / pulse.duration);
            pulse.elapsed += deltaTime;
            float after = Mathf.Clamp01(pulse.elapsed / pulse.duration);
            lift += pulse.amount * (Mathf.SmoothStep(0f, 1f, after) - Mathf.SmoothStep(0f, 1f, before));
            if (after >= 1f) _pulses.RemoveAt(i);
            else _pulses[i] = pulse;
        }
        return lift;
    }

    public void Clear() => _pulses.Clear();
}

// Match accepted stroke events, not overlapping contact states. Each event is used once.
public sealed class BeeStrokePairWindow
{
    private float _left = float.NegativeInfinity;
    private float _right = float.NegativeInfinity;

    public bool Register(bool isLeft, float time, float window)
    {
        float opposite = isLeft ? _right : _left;
        float elapsed = time - opposite;
        if (elapsed >= 0f && elapsed <= Mathf.Max(0f, window))
        {
            Clear();
            return true;
        }
        if (isLeft) _left = time;
        else _right = time;
        return false;
    }

    public void Clear() => _left = _right = float.NegativeInfinity;
}

public static class BeeFlightMath
{
    // Integral of 1 - smoothstep(t). A full brake travels velocity * duration / 2.
    public static float BrakeIntegral(float t)
    {
        t = Mathf.Clamp01(t);
        return t - t * t * t + 0.5f * t * t * t * t;
    }
}
