using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LetItBee.UI
{
    /// <summary>
    /// One menu card (Story / Study / Settings). Reacts to the VR controller ray
    /// through the EventSystem: hover = scale up + glow, click = small press pulse.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LetItBeeMenuCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Image border;
        public Image glow;
        public float hoverScale = 1.06f;
        public float smoothing = 14f;
        [Range(0f, 1f)] public float idleBorderAlpha = 0.55f;

        public UnityEvent onHoverEnter = new UnityEvent();
        public UnityEvent onClick = new UnityEvent();

        readonly System.Collections.Generic.HashSet<int> pointers = new System.Collections.Generic.HashSet<int>();
        float nextClick;
        Vector3 _targetScale = Vector3.one;
        float _glowTarget;
        float _glow;
        float _pulse;

        void OnEnable()
        {
            pointers.Clear(); _pulse = 0f; nextClick = 0f;
            transform.localScale = Vector3.one;
            _targetScale = Vector3.one;
            _glowTarget = _glow = 0f;
            Apply(0f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!GetComponent<Button>().IsInteractable()) return;
            pointers.Add(eventData.pointerId);
            _targetScale = Vector3.one * hoverScale;
            _glowTarget = 1f;
            onHoverEnter.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointers.Remove(eventData.pointerId); if (pointers.Count > 0) return;
            _targetScale = Vector3.one;
            _glowTarget = 0f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!GetComponent<Button>().IsInteractable() || Time.unscaledTime < nextClick) return;
            nextClick = Time.unscaledTime + 0.3f;
            _pulse = 1f;
            onClick.Invoke();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float k = 1f - Mathf.Exp(-smoothing * dt);
            _pulse = Mathf.MoveTowards(_pulse, 0f, dt * 5f);

            Vector3 target = _targetScale * (1f - 0.04f * Mathf.Sin(_pulse * Mathf.PI));
            transform.localScale = Vector3.Lerp(transform.localScale, target, k);

            _glow = Mathf.Lerp(_glow, _glowTarget, k);
            Apply(_glow);
        }

        void Apply(float a)
        {
            if (glow != null)
            {
                Color c = glow.color;
                c.a = a;
                glow.color = c;
            }
            if (border != null)
            {
                Color c = border.color;
                c.a = Mathf.Lerp(idleBorderAlpha, 1f, a);
                border.color = c;
            }
        }
    }
}
