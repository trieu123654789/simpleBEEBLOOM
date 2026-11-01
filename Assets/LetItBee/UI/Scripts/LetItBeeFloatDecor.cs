using UnityEngine;

namespace LetItBee.UI
{
    /// <summary>Gentle hover + tilt for decorative bees on the menu.</summary>
    public class LetItBeeFloatDecor : MonoBehaviour
    {
        public float amplitude = 6f;      // canvas units
        public float speed = 1.2f;
        public float tiltDegrees = 4f;
        public float phase;

        Vector2 _start;
        RectTransform _rt;

        void Awake()
        {
            _rt = (RectTransform)transform;
            _start = _rt.anchoredPosition;
        }

        void Update()
        {
            float t = Time.unscaledTime * speed + phase;
            _rt.anchoredPosition = _start + new Vector2(Mathf.Sin(t * 0.7f) * amplitude * 0.5f, Mathf.Sin(t) * amplitude);
            _rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * tiltDegrees);
        }
    }
}
