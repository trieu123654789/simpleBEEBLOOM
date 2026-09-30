using UnityEngine;

public class RotateTargetHub : MonoBehaviour
{
    [Tooltip("Đối tượng để nhìn (mặc định là Main Camera)")]
    public Transform target;

    [Tooltip("Tốc độ quay (0 = quay ngay lập tức)")]
    public float turnSpeed = 12f;

    [Tooltip("Góc bù nếu model của bạn lệch hướng trước")]
    public float yawOffset = 0f;

    private void Awake()
    {
        if (target == null && Camera.main != null)
            target = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Hướng tới camera nhưng bỏ thành phần cao–thấp (Y)
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;                            // CHỈ xoay theo Y
        if (dir.sqrMagnitude < 1e-6f) return;  // quá gần thì bỏ

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
        if (yawOffset != 0f)
            desired = Quaternion.AngleAxis(yawOffset, Vector3.up) * desired;

        // Quay mượt
        if (turnSpeed <= 0f) transform.rotation = desired;
        else transform.rotation = Quaternion.Slerp(
            transform.rotation, desired, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }
}