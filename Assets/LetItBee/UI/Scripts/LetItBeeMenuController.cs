using UnityEngine;
using UnityEngine.Events;

namespace LetItBee.UI
{
    /// <summary>
    /// Put on the menu's root Canvas. Forwards card clicks to three UnityEvents
    /// (wire them in the Inspector) and gives the panel a gentle floating motion.
    /// </summary>
    public class LetItBeeMenuController : MonoBehaviour
    {
        public LetItBeeMenuCard storyCard;
        public LetItBeeMenuCard studyCard;
        public LetItBeeMenuCard settingsCard;

        [Header("Events")]
        public UnityEvent onStoryMode = new UnityEvent();
        public UnityEvent onStudyMode = new UnityEvent();
        public UnityEvent onSettings = new UnityEvent();

        [Header("Floating motion (meters)")]
        public float bobHeight = 0.01f;
        public float bobSpeed = 0.8f;

        Vector3 _startLocalPos;

        void Awake()
        {
            _startLocalPos = transform.localPosition;
        }

        void OnEnable()
        {
            if (storyCard != null) storyCard.onClick.AddListener(HandleStory);
            if (studyCard != null) studyCard.onClick.AddListener(HandleStudy);
            if (settingsCard != null) settingsCard.onClick.AddListener(HandleSettings);
        }

        void OnDisable()
        {
            if (storyCard != null) storyCard.onClick.RemoveListener(HandleStory);
            if (studyCard != null) studyCard.onClick.RemoveListener(HandleStudy);
            if (settingsCard != null) settingsCard.onClick.RemoveListener(HandleSettings);
        }

        void HandleStory() { onStoryMode.Invoke(); }
        void HandleStudy() { onStudyMode.Invoke(); }
        void HandleSettings() { onSettings.Invoke(); }

        void Update()
        {
            if (bobHeight <= 0f) return;
            float y = Mathf.Sin(Time.unscaledTime * bobSpeed * Mathf.PI * 2f) * bobHeight;
            transform.localPosition = _startLocalPos + new Vector3(0f, y, 0f);
        }
    }
}
