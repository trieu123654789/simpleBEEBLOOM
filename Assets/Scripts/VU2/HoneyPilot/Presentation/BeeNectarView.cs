using UnityEngine;

namespace LetItBee.HoneyPilot
{
    [DisallowMultipleComponent, AddComponentMenu("Let It Bee/Presentation/Nectar View")]
    public sealed class BeeNectarView : MonoBehaviour
    {
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        [Tooltip("Local anchor representing the hand contact on the nectar. Zero preserves the current carry pose.")]
        public Transform gripAnchor;
    }
}
