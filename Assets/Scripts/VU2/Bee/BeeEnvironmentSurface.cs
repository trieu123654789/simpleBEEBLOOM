using UnityEngine;

// Explicit surface classification for authored environments; independent of mesh names.
[DisallowMultipleComponent]
public sealed class BeeEnvironmentSurface : MonoBehaviour
{
    public enum SurfaceKind { Ground, Water }
    public SurfaceKind kind = SurfaceKind.Ground;
}